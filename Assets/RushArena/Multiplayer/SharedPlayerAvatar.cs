using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using RushArena.Core;
using RushArena.Input;
using RushArena.Runtime.Audio;
using UnityEngine;

namespace RushArena.Multiplayer
{
    // Each Shared Mode owner advances its own KCC; proxies render replicated state.
    [RequireComponent(typeof(NetworkObject), typeof(SimpleKCC))]
    public sealed class SharedPlayerAvatar : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float sprintSpeed = 10.5f;
        [SerializeField] private float groundAcceleration = 65f;
        [SerializeField] private float airAcceleration = 18f;
        [SerializeField] private float gravity = 25f;
        [SerializeField] private float jumpHeight = 1.4f;

        [Header("Materials (Serialized to prevent stripping)")]
        public Material armorMaterial;
        public Material visorMaterial;
        public Material rifleMaterial;
        public Material plasmaMaterial;
        public Material laserTracerMaterial;
        public Material plasmaTracerMaterial;
        [SerializeField] private MeshRenderer bodyRenderer;

        // Synchronized Network State
        [Networked] private Vector3 MoveVelocity { get; set; }
        [Networked] private float CoyoteLeft { get; set; }
        [Networked] private float JumpBufferLeft { get; set; }
        [Networked] private NetworkBool JumpConsumed { get; set; }
        [Networked] public int Health { get; private set; }
        [Networked] public int Shield { get; private set; }
        [Networked] public int Kills { get; set; }
        [Networked] public int Deaths { get; set; }
        [Networked] public int Score { get; set; }
        [Networked] public WeaponType CurrentWeapon { get; set; }
        [Networked] public float AimPitch { get; set; }
        [Networked] public int PlayerNumber { get; set; }
        [Networked] private TickTimer RapidFireTimer { get; set; }
        [Networked] private TickTimer SpeedBoostTimer { get; set; }
        [Networked] private TickTimer InvulnerableTimer { get; set; }

        public string PlayerDisplayName => $"Player {PlayerNumber + 1}";

        private readonly TickInputBuffer commands = new TickInputBuffer();
        private SimpleKCC kcc;
        private PlayerInputSource source;
        private Camera ownerCamera;
        private AudioSource audioSource;
        private readonly RaycastHit[] shotHits = new RaycastHit[16];
        private float nextShotAt;
        private Vector3 spawnPosition;
        private CombatHud combatHud;

        // First-person weapon references
        private GameObject fpRifle;
        private GameObject fpPlasma;
        private Transform fpMuzzleRifle;
        private Transform fpMuzzlePlasma;
        private Light fpLightRifle;
        private Light fpLightPlasma;
        private LineRenderer tracer;
        private float tracerUntil;
        private float muzzleFlashUntil;
        private Vector3 weaponRecoilPos;
        private Vector3 weaponRecoilRot;
        private Vector3 defaultWeaponPos = new Vector3(0.24f, -0.22f, 0.48f);

        // Third-person avatar presentation
        private GameObject tpCharacter;
        private Transform tpUpperBody;
        private Transform tpLeftLeg;
        private Transform tpRightLeg;
        private Transform tpWeapon;
        private float strideCycle;
        private float damageFlashUntil;
        private Material originalArmorMat;
        private Material damageFlashMat;

        // Ammo state (local client tracking)
        public int AmmoInClip { get; private set; }
        public int MaxClip => CurrentWeapon == WeaponType.AssaultRifle ? 30 : 10;
        public bool IsReloading { get; private set; }
        private float reloadCompleteAt;

        // Respawn state
        public bool IsDead => Health <= 0;
        public float RespawnTimeRemaining => Mathf.Max(0f, respawnAt - Time.time);
        private float respawnAt;

        public bool HasRapidFire => Runner != null && RapidFireTimer.IsRunning && !RapidFireTimer.Expired(Runner);
        public bool HasSpeedBoost => Runner != null && SpeedBoostTimer.IsRunning && !SpeedBoostTimer.Expired(Runner);
        public bool IsInvulnerable => Runner != null && InvulnerableTimer.IsRunning && !InvulnerableTimer.Expired(Runner);

        public string PowerupLabel
        {
            get
            {
                if (HasRapidFire) return "RAPID FIRE";
                if (HasSpeedBoost) return "SUPER SPEED";
                if (IsInvulnerable) return "SHIELDED";
                return string.Empty;
            }
        }

        private void Awake()
        {
            kcc = GetComponent<SimpleKCC>();
            if (kcc != null)
            {
                kcc.SetCollisionLayerMask(~0);
            }
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 35f;

            EnsureMaterials();

            // Hitbox trigger collider for raycast shooting
            if (transform.Find("Hitbox") == null)
            {
                var hitbox = new GameObject("Hitbox");
                hitbox.transform.SetParent(transform, false);
                var hitCollider = hitbox.AddComponent<CapsuleCollider>();
                hitCollider.center = new Vector3(0f, 0.95f, 0f);
                hitCollider.height = 1.9f;
                hitCollider.radius = 0.42f;
                hitCollider.isTrigger = true;
            }
        }

        private void EnsureMaterials()
        {
            // If materials are not linked in the inspector, fetch from Resources/Assets or fallback to existing bodyRenderer
            if (armorMaterial == null && bodyRenderer != null) armorMaterial = bodyRenderer.sharedMaterial;
            if (visorMaterial == null && armorMaterial != null) visorMaterial = armorMaterial;
            if (rifleMaterial == null && armorMaterial != null) rifleMaterial = armorMaterial;
            if (plasmaMaterial == null && armorMaterial != null) plasmaMaterial = armorMaterial;
            if (laserTracerMaterial == null && armorMaterial != null) laserTracerMaterial = armorMaterial;
            if (plasmaTracerMaterial == null && armorMaterial != null) plasmaTracerMaterial = armorMaterial;
        }

        public override void Spawned()
        {
            spawnPosition = transform.position;
            if (kcc != null)
            {
                kcc.SetCollisionLayerMask(~0);
            }

            if (Object.HasStateAuthority)
            {
                Health = 100;
                Shield = 50;
                AmmoInClip = 30;
                PlayerNumber = Runner.LocalPlayer.PlayerId;

                source = gameObject.AddComponent<PlayerInputSource>();
                if (bodyRenderer != null) bodyRenderer.enabled = false;

                // Move local player's own hitbox to Ignore Raycast so local camera never hits itself
                var hb = transform.Find("Hitbox");
                if (hb != null) hb.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

                // Create first-person eye camera
                var eye = new GameObject("Owner eye camera");
                eye.transform.SetParent(transform, false);
                eye.transform.localPosition = new Vector3(0f, 1.62f, 0f);
                eye.tag = "MainCamera";
                ownerCamera = eye.AddComponent<Camera>();
                ownerCamera.fieldOfView = 85f;
                ownerCamera.nearClipPlane = 0.05f;
                ownerCamera.farClipPlane = 120f;
                ownerCamera.clearFlags = CameraClearFlags.SolidColor;
                ownerCamera.backgroundColor = new Color(0.08f, 0.11f, 0.16f);
                eye.AddComponent<AudioListener>();

                // Build first-person weapons
                fpRifle = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.AssaultRifle, rifleMaterial, visorMaterial, out fpMuzzleRifle, out fpLightRifle);
                fpPlasma = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.PlasmaCannon, plasmaMaterial, visorMaterial, out fpMuzzlePlasma, out fpLightPlasma);
                fpPlasma.SetActive(false);

                // Line renderer for bullet tracers
                tracer = eye.gameObject.AddComponent<LineRenderer>();
                tracer.positionCount = 2;
                tracer.startWidth = 0.024f;
                tracer.endWidth = 0.008f;
                tracer.useWorldSpace = true;
                tracer.enabled = false;
                if (laserTracerMaterial != null) tracer.sharedMaterial = laserTracerMaterial;

                combatHud = gameObject.AddComponent<CombatHud>();
                combatHud.Configure(this);
            }
            else
            {
                // Proxy avatar: Disable old capsule, build stylized 3D combatant avatar!
                if (bodyRenderer != null) bodyRenderer.enabled = false;
                tpCharacter = ProceduralVisuals.BuildThirdPersonCharacter(transform, armorMaterial, visorMaterial, rifleMaterial,
                    out tpUpperBody, out tpLeftLeg, out tpRightLeg, out tpWeapon);
            }
        }

        private void Update()
        {
            if (Object == null) return;

            if (Object.HasStateAuthority)
            {
                if (source != null && !IsDead)
                {
                    commands.Capture(source.Sample(), source.IsActive);
                }

                // Check local reload completion
                if (IsReloading && Time.time >= reloadCompleteAt)
                {
                    IsReloading = false;
                    AmmoInClip = MaxClip;
                }

                // Check respawn timer
                if (IsDead && Time.time >= respawnAt)
                {
                    Respawn();
                }

                // Smooth view weapon recoil recovery
                if (ownerCamera != null)
                {
                    weaponRecoilPos = Vector3.Lerp(weaponRecoilPos, Vector3.zero, 18f * Time.deltaTime);
                    weaponRecoilRot = Vector3.Lerp(weaponRecoilRot, Vector3.zero, 18f * Time.deltaTime);
                    var activeFpWeapon = CurrentWeapon == WeaponType.AssaultRifle ? fpRifle : fpPlasma;
                    if (activeFpWeapon != null)
                    {
                        activeFpWeapon.transform.localPosition = defaultWeaponPos + weaponRecoilPos;
                        activeFpWeapon.transform.localRotation = Quaternion.Euler(weaponRecoilRot);
                    }
                }

                // Turn off muzzle flash light after 0.04s
                if (Time.unscaledTime >= muzzleFlashUntil)
                {
                    if (fpLightRifle != null && fpLightRifle.enabled) fpLightRifle.enabled = false;
                    if (fpLightPlasma != null && fpLightPlasma.enabled) fpLightPlasma.enabled = false;
                }

                // Turn off tracer line
                if (tracer != null && tracer.enabled && Time.unscaledTime >= tracerUntil)
                {
                    tracer.enabled = false;
                }
            }
            else
            {
                // Proxy: update visual procedural animations
                AnimateProxyVisuals();
            }
        }

        private void AnimateProxyVisuals()
        {
            if (tpCharacter == null) return;

            // Aim pitch replication
            if (tpUpperBody != null)
            {
                tpUpperBody.localRotation = Quaternion.Euler(AimPitch, 0f, 0f);
            }

            // Leg stride animation when moving
            float speed = kcc.RealVelocity.magnitude;
            if (speed > 0.4f && kcc.IsGrounded)
            {
                strideCycle += speed * Time.deltaTime * 3.5f;
                float legAngle = Mathf.Sin(strideCycle) * 28f;
                if (tpLeftLeg != null) tpLeftLeg.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
                if (tpRightLeg != null) tpRightLeg.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);
            }
            else
            {
                if (tpLeftLeg != null) tpLeftLeg.localRotation = Quaternion.Lerp(tpLeftLeg.localRotation, Quaternion.identity, 10f * Time.deltaTime);
                if (tpRightLeg != null) tpRightLeg.localRotation = Quaternion.Lerp(tpRightLeg.localRotation, Quaternion.identity, 10f * Time.deltaTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || IsDead) return;

            PlayerCommand command = commands.Consume();

            // Look rotation
            kcc.AddLookRotation(new Vector2(-command.LookY, command.LookX));
            Vector2 look = kcc.GetLookRotation(true, false);
            AimPitch = look.x;

            // Switch weapons
            if (command.SwitchWeapon && !IsReloading)
            {
                CurrentWeapon = CurrentWeapon == WeaponType.AssaultRifle ? WeaponType.PlasmaCannon : WeaponType.AssaultRifle;
                AmmoInClip = MaxClip;
                if (fpRifle != null) fpRifle.SetActive(CurrentWeapon == WeaponType.AssaultRifle);
                if (fpPlasma != null) fpPlasma.SetActive(CurrentWeapon == WeaponType.PlasmaCannon);
                if (tracer != null) tracer.sharedMaterial = CurrentWeapon == WeaponType.AssaultRifle ? laserTracerMaterial : plasmaTracerMaterial;
            }

            float dt = Runner.DeltaTime;
            CoyoteLeft = kcc.IsGrounded ? 0.10f : Mathf.Max(0f, CoyoteLeft - dt);
            JumpBufferLeft = command.JumpPressed ? 0.12f : Mathf.Max(0f, JumpBufferLeft - dt);
            if (kcc.IsGrounded && kcc.RealVelocity.y <= 0f) JumpConsumed = false;

            float jumpImpulse = 0f;
            if (!JumpConsumed && CoyoteLeft > 0f && JumpBufferLeft > 0f)
            {
                jumpImpulse = Mathf.Sqrt(2f * gravity * jumpHeight);
                JumpConsumed = true;
                CoyoteLeft = JumpBufferLeft = 0f;
            }

            // Power-up movement speed boost
            float currentSprintSpeed = HasSpeedBoost ? sprintSpeed * 1.35f : sprintSpeed;
            float currentWalkSpeed = HasSpeedBoost ? walkSpeed * 1.35f : walkSpeed;

            Vector2 axes = Vector2.ClampMagnitude(new Vector2(command.MoveX, command.MoveY), 1f);
            Vector3 desired = kcc.TransformRotation * new Vector3(axes.x, 0f, axes.y)
                * (command.Sprint ? currentSprintSpeed : currentWalkSpeed);
            float acceleration = kcc.IsGrounded ? groundAcceleration : airAcceleration;
            MoveVelocity = Vector3.MoveTowards(MoveVelocity, desired, acceleration * dt);
            kcc.SetGravity(-gravity);
            kcc.Move(MoveVelocity, jumpImpulse);

            // Prevent falling out of the arena
            if (transform.position.y < -5f)
            {
                kcc.SetPosition(spawnPosition + Vector3.up * 1.0f, true, false);
                kcc.ResetVelocity();
                MoveVelocity = Vector3.zero;
            }

            // Check Powerup Pickups
            if (ArenaPowerup.TryCollect(transform.position, out var collectedPowerup))
            {
                if (collectedPowerup == PowerupType.RapidFire)
                    RapidFireTimer = TickTimer.CreateFromSeconds(Runner, 10f);
                else if (collectedPowerup == PowerupType.ShieldBoost)
                    Shield = Mathf.Min(100, Shield + 50);
                else if (collectedPowerup == PowerupType.SpeedBoost)
                    SpeedBoostTimer = TickTimer.CreateFromSeconds(Runner, 10f);
            }

            // Shooting
            if (command.Fire && Time.time >= nextShotAt && !IsReloading)
            {
                Fire();
            }
        }

        private void Fire()
        {
            if (AmmoInClip <= 0)
            {
                StartReload();
                return;
            }

            AmmoInClip--;
            bool rapid = HasRapidFire;

            float fireInterval = CurrentWeapon == WeaponType.AssaultRifle
                ? (rapid ? 0.07f : 0.14f)
                : (rapid ? 0.28f : 0.55f);
            nextShotAt = Time.time + fireInterval;

            // Recoil kickback
            weaponRecoilPos = new Vector3(Random.Range(-0.015f, 0.015f), Random.Range(0.01f, 0.02f), -0.065f);
            weaponRecoilRot = new Vector3(-4.5f, Random.Range(-2f, 2f), 0f);

            // Muzzle flash
            muzzleFlashUntil = Time.unscaledTime + 0.045f;
            if (CurrentWeapon == WeaponType.AssaultRifle)
            {
                ProceduralAudio.PlayRifle(audioSource);
                if (fpLightRifle != null) fpLightRifle.enabled = true;
            }
            else
            {
                ProceduralAudio.PlayPlasma(audioSource);
                if (fpLightPlasma != null) fpLightPlasma.enabled = true;
            }

            // Raycast hit detection across geometry, players, and bots
            Vector3 origin = ownerCamera.transform.position;
            Vector3 forward = ownerCamera.transform.forward;
            RaycastHit[] hits = Physics.RaycastAll(origin, forward, 80f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Vector3 end = origin + forward * 60f;
            SharedPlayerAvatar target = null;
            ArenaBot botTarget = null;
            bool hitCombatant = false;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.isTrigger && h.collider.GetComponentInParent<JumpPad>() != null) continue;
                if (h.collider.isTrigger && h.collider.GetComponentInParent<ArenaPowerup>() != null) continue;

                end = h.point;
                target = h.collider.GetComponentInParent<SharedPlayerAvatar>();
                if (target == this) target = null;
                botTarget = h.collider.GetComponentInParent<ArenaBot>();

                if (target != null && !target.IsDead)
                {
                    hitCombatant = true;
                }
                else if (botTarget != null)
                {
                    hitCombatant = true;
                }
                break;
            }

            // Bullet Tracer line
            Transform muzzle = CurrentWeapon == WeaponType.AssaultRifle ? fpMuzzleRifle : fpMuzzlePlasma;
            Vector3 startPos = muzzle != null ? muzzle.position : origin + forward * 0.4f;
            tracer.SetPosition(0, startPos);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerUntil = Time.unscaledTime + 0.05f;

            if (hitCombatant)
            {
                ProceduralAudio.PlayHitMarker();
                int dmg = CurrentWeapon == WeaponType.AssaultRifle ? (rapid ? 18 : 24) : 48;
                if (target != null)
                {
                    target.RpcApplyDamage(dmg, Runner.LocalPlayer.PlayerId, CurrentWeapon);
                }
                else if (botTarget != null)
                {
                    botTarget.ApplyDamage(dmg, Runner.LocalPlayer.PlayerId, CurrentWeapon);
                }
            }

            if (combatHud != null) combatHud.PulseShot(hitCombatant);

            if (AmmoInClip <= 0)
            {
                StartReload();
            }
        }

        private void StartReload()
        {
            if (IsReloading) return;
            IsReloading = true;
            reloadCompleteAt = Time.time + (CurrentWeapon == WeaponType.AssaultRifle ? 1.4f : 1.9f);
        }

        public void TakeDamage(int damage, int attackerPlayerId, WeaponType weaponUsed)
        {
            if (IsDead || IsInvulnerable) return;

            ProceduralAudio.PlayDamage();

            // Shield absorbs damage first
            if (Shield > 0)
            {
                if (Shield >= damage)
                {
                    Shield -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= Shield;
                    Shield = 0;
                }
            }

            if (damage > 0)
            {
                Health = Mathf.Max(0, Health - damage);
            }

            if (Health <= 0)
            {
                OnEliminated(attackerPlayerId, weaponUsed);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcApplyDamage(int damage, int attackerPlayerId, WeaponType weaponUsed)
        {
            TakeDamage(damage, attackerPlayerId, weaponUsed);
        }

        private void OnEliminated(int attackerPlayerId, WeaponType weaponUsed)
        {
            Deaths++;
            respawnAt = Time.time + 3.0f; // 3-second respawn timer

            // Award killer
            var avatars = FindObjectsByType<SharedPlayerAvatar>();
            string killerName = "Player " + (attackerPlayerId + 1);
            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i].Object != null && avatars[i].Object.InputAuthority.PlayerId == attackerPlayerId)
                {
                    avatars[i].RpcAwardElimination();
                    killerName = avatars[i].PlayerDisplayName;
                    break;
                }
            }

            // Broadcast kill feed event
            if (SharedMatchManager.Instance != null)
            {
                string weaponName = weaponUsed == WeaponType.AssaultRifle ? "Assault Rifle" : "Plasma Cannon";
                SharedMatchManager.Instance.RpcReportKill(killerName, PlayerDisplayName, weaponName);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcAwardElimination()
        {
            Kills++;
            Score += 100;
        }

        public void ResetStats()
        {
            Kills = Deaths = Score = 0;
            Respawn();
        }

        private void Respawn()
        {
            Health = 100;
            Shield = 50;
            AmmoInClip = MaxClip;
            IsReloading = false;
            MoveVelocity = Vector3.zero;

            // Pick a random arena spawn point
            int slot = Random.Range(0, 8);
            var spawn = new Vector3(-16f + (slot % 4) * 10f, 0.2f, -16f + (slot / 4) * 32f);
            kcc.SetPosition(spawn);

            // Grant 2 seconds of invulnerability shield upon respawn
            InvulnerableTimer = TickTimer.CreateFromSeconds(Runner, 2.0f);
        }

        public void ApplyLaunchImpulse(Vector3 impulse)
        {
            MoveVelocity = new Vector3(impulse.x, 0f, impulse.z);
            kcc.SetGravity(-gravity);
            kcc.Move(MoveVelocity, impulse.y);
        }

        private void LateUpdate()
        {
            if (ownerCamera == null) return;
            Vector2 look = kcc.GetLookRotation(true, false);
            ownerCamera.transform.localRotation = Quaternion.Euler(look.x, 0f, 0f);
        }

        private void OnApplicationFocus(bool focused) { if (!focused) commands.Clear(); }
        private void OnApplicationPause(bool paused) { if (paused) commands.Clear(); }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            commands.Clear();
            if (source != null) Destroy(source);
            if (ownerCamera != null) Destroy(ownerCamera.gameObject);
            if (tpCharacter != null) Destroy(tpCharacter);
        }
    }
}
