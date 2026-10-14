using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using RushArena.Core;
using RushArena.Input;
using RushArena.Runtime;
using RushArena.Runtime.Audio;
using UnityEngine;

namespace RushArena.Multiplayer
{
    // Each Shared Mode owner advances its own KCC; proxies render replicated state.
    // Handles tactical inventory progression (Pistol default, arena pickups lost on death),
    // fixed ammo economy with automatic weapon discard upon depletion, precision sniper scoping,
    // mouse wheel weapon cycling, mechanical reload animations, and jump pad physics.
    [RequireComponent(typeof(NetworkObject), typeof(SimpleKCC))]
    public sealed class SharedPlayerAvatar : NetworkBehaviour
    {
        public static readonly List<SharedPlayerAvatar> AllAvatars = new List<SharedPlayerAvatar>();

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
        private float nextShotAt;
        private Vector3 spawnPosition;
        private CombatHud combatHud;

        // Buffered JumpPad impulse to execute strictly inside FixedUpdateNetwork
        private Vector3 pendingLaunchImpulse;
        private bool hasPendingLaunchImpulse;

        // Weapon inventory (Everyone starts with Pistol; arena weapon drops lost on death)
        private readonly List<WeaponType> unlockedWeapons = new List<WeaponType> { WeaponType.Pistol };
        private readonly Dictionary<WeaponType, int> clipAmmo = new Dictionary<WeaponType, int>();
        private readonly Dictionary<WeaponType, int> reserveAmmo = new Dictionary<WeaponType, int>();

        // First-person weapon models
        private GameObject fpPistol;
        private GameObject fpRifle;
        private GameObject fpPlasma;
        private GameObject fpScatter;
        private GameObject fpSniper;
        private GameObject fpMachineGun;
        private Transform fpMuzzlePistol;
        private Transform fpMuzzleRifle;
        private Transform fpMuzzlePlasma;
        private Transform fpMuzzleScatter;
        private Transform fpMuzzleSniper;
        private Transform fpMuzzleMachineGun;
        private Light fpLightPistol;
        private Light fpLightRifle;
        private Light fpLightPlasma;
        private Light fpLightScatter;
        private Light fpLightSniper;
        private Light fpLightMachineGun;

        private LineRenderer tracer;
        private float tracerUntil;
        private float muzzleFlashUntil;
        private Vector3 weaponRecoilPos;
        private Vector3 weaponRecoilRot;
        private Vector3 defaultWeaponPos = new Vector3(0.24f, -0.22f, 0.48f);

        // Third-person avatar presentation for proxies
        private GameObject tpCharacter;
        private Transform tpUpperBody;
        private Transform tpLeftLeg;
        private Transform tpRightLeg;
        private Transform tpWeapon;
        private float strideCycle;

        // Ammo state (local tracking)
        public int AmmoInClip { get; private set; }
        public int MaxClip => CurrentWeapon switch
        {
            WeaponType.Pistol => 12,
            WeaponType.AssaultRifle => 30,
            WeaponType.PlasmaCannon => 10,
            WeaponType.Scattergun => 8,
            WeaponType.SniperRifle => 5,
            WeaponType.MachineGun => 50,
            _ => 20
        };

        public string ReserveAmmoDisplay => CurrentWeapon == WeaponType.Pistol
            ? "∞"
            : (reserveAmmo.TryGetValue(CurrentWeapon, out int r) ? r.ToString("00") : "00");

        // Sniper Scoping state
        public bool IsScoped { get; private set; }
        private bool wasScopedLastFrame;
        private float boltActionAt;
        public float SniperCycleProgress
        {
            get
            {
                if (CurrentWeapon != WeaponType.SniperRifle) return 1f;
                if (Time.time >= nextShotAt) return 1f;
                float totalDelay = HasRapidFire ? 0.95f : 1.35f;
                float remaining = nextShotAt - Time.time;
                return Mathf.Clamp01(1f - (remaining / totalDelay));
            }
        }

        // Tactical Reload Animation State
        public bool IsReloading { get; private set; }
        public float ReloadProgress => IsReloading && reloadDuration > 0.01f ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadDuration) : 0f;
        private float reloadStartTime;
        private float reloadDuration = 1.4f;
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
                if (HasRapidFire) return "OVERCHARGE";
                if (HasSpeedBoost) return "SUPER SPEED";
                if (IsInvulnerable) return "SHIELDED";
                return string.Empty;
            }
        }

        private void Awake()
        {
            if (!AllAvatars.Contains(this)) AllAvatars.Add(this);
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
                PlayerNumber = Runner.LocalPlayer.PlayerId;

                // Reset inventory to starting Pistol with infinite reserve
                unlockedWeapons.Clear();
                unlockedWeapons.Add(WeaponType.Pistol);
                clipAmmo.Clear();
                reserveAmmo.Clear();
                reserveAmmo[WeaponType.Pistol] = int.MaxValue;
                CurrentWeapon = WeaponType.Pistol;
                AmmoInClip = MaxClip;
                clipAmmo[WeaponType.Pistol] = MaxClip;

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
                ownerCamera.fieldOfView = GameSettings.FieldOfView;
                ownerCamera.nearClipPlane = 0.05f;
                ownerCamera.farClipPlane = 120f;
                ownerCamera.clearFlags = CameraClearFlags.SolidColor;
                ownerCamera.backgroundColor = new Color(0.08f, 0.11f, 0.16f);
                eye.AddComponent<AudioListener>();

                // Build all first-person weapon models
                fpPistol = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.Pistol, rifleMaterial, visorMaterial, out fpMuzzlePistol, out fpLightPistol);
                fpRifle = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.AssaultRifle, rifleMaterial, visorMaterial, out fpMuzzleRifle, out fpLightRifle);
                fpPlasma = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.PlasmaCannon, plasmaMaterial, visorMaterial, out fpMuzzlePlasma, out fpLightPlasma);
                fpScatter = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.Scattergun, rifleMaterial, visorMaterial, out fpMuzzleScatter, out fpLightScatter);
                fpSniper = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.SniperRifle, rifleMaterial, visorMaterial, out fpMuzzleSniper, out fpLightSniper);
                fpMachineGun = ProceduralVisuals.BuildFirstPersonWeapon(eye.transform, WeaponType.MachineGun, rifleMaterial, visorMaterial, out fpMuzzleMachineGun, out fpLightMachineGun);

                UpdateActiveFpWeaponVisual();

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
                // Proxy avatar: stylized 3D combatant gladiator
                if (bodyRenderer != null) bodyRenderer.enabled = false;
                tpCharacter = ProceduralVisuals.BuildThirdPersonCharacter(transform, armorMaterial, visorMaterial, rifleMaterial,
                    out tpUpperBody, out tpLeftLeg, out tpRightLeg, out tpWeapon);
            }
        }

        private void UpdateActiveFpWeaponVisual()
        {
            if (fpPistol != null) fpPistol.SetActive(CurrentWeapon == WeaponType.Pistol);
            if (fpRifle != null) fpRifle.SetActive(CurrentWeapon == WeaponType.AssaultRifle);
            if (fpPlasma != null) fpPlasma.SetActive(CurrentWeapon == WeaponType.PlasmaCannon);
            if (fpScatter != null) fpScatter.SetActive(CurrentWeapon == WeaponType.Scattergun);
            if (fpSniper != null) fpSniper.SetActive(CurrentWeapon == WeaponType.SniperRifle && !IsScoped);
            if (fpMachineGun != null) fpMachineGun.SetActive(CurrentWeapon == WeaponType.MachineGun);

            if (tracer != null)
            {
                tracer.sharedMaterial = (CurrentWeapon == WeaponType.PlasmaCannon) ? plasmaTracerMaterial : laserTracerMaterial;
            }
        }

        private GameObject GetActiveFpWeaponObject()
        {
            return CurrentWeapon switch
            {
                WeaponType.Pistol => fpPistol,
                WeaponType.AssaultRifle => fpRifle,
                WeaponType.PlasmaCannon => fpPlasma,
                WeaponType.Scattergun => fpScatter,
                WeaponType.SniperRifle => fpSniper,
                WeaponType.MachineGun => fpMachineGun,
                _ => fpPistol
            };
        }

        private Transform GetActiveMuzzle()
        {
            return CurrentWeapon switch
            {
                WeaponType.Pistol => fpMuzzlePistol,
                WeaponType.AssaultRifle => fpMuzzleRifle,
                WeaponType.PlasmaCannon => fpMuzzlePlasma,
                WeaponType.Scattergun => fpMuzzleScatter,
                WeaponType.SniperRifle => fpMuzzleSniper,
                WeaponType.MachineGun => fpMuzzleMachineGun,
                _ => fpMuzzlePistol
            };
        }

        private Light GetActiveMuzzleLight()
        {
            return CurrentWeapon switch
            {
                WeaponType.Pistol => fpLightPistol,
                WeaponType.AssaultRifle => fpLightRifle,
                WeaponType.PlasmaCannon => fpLightPlasma,
                WeaponType.Scattergun => fpLightScatter,
                WeaponType.SniperRifle => fpLightSniper,
                WeaponType.MachineGun => fpLightMachineGun,
                _ => fpLightPistol
            };
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

                // Smooth Optical Zoom for Sniper Rifle Scoping
                if (ownerCamera != null)
                {
                    float targetFov = (IsScoped && CurrentWeapon == WeaponType.SniperRifle) ? 22f : GameSettings.FieldOfView;
                    ownerCamera.fieldOfView = Mathf.Lerp(ownerCamera.fieldOfView, targetFov, 22f * Time.deltaTime);
                }

                // Check reload completion
                if (IsReloading && Time.time >= reloadCompleteAt)
                {
                    IsReloading = false;
                    int needed = MaxClip - AmmoInClip;
                    int available = reserveAmmo.TryGetValue(CurrentWeapon, out int res) ? res : 0;
                    int toLoad = Mathf.Min(needed, available);
                    AmmoInClip += toLoad;
                    clipAmmo[CurrentWeapon] = AmmoInClip;
                    if (CurrentWeapon != WeaponType.Pistol)
                    {
                        reserveAmmo[CurrentWeapon] = Mathf.Max(0, available - toLoad);
                    }
                }

                // Check respawn timer
                if (IsDead && Time.time >= respawnAt)
                {
                    Respawn();
                }

                // Exaggerated Mechanical Procedural Reload & View Recoil Animation
                var activeWeapon = GetActiveFpWeaponObject();
                if (activeWeapon != null)
                {
                    weaponRecoilPos = Vector3.Lerp(weaponRecoilPos, Vector3.zero, 18f * Time.deltaTime);
                    weaponRecoilRot = Vector3.Lerp(weaponRecoilRot, Vector3.zero, 18f * Time.deltaTime);

                    Vector3 reloadPosOffset = Vector3.zero;
                    Vector3 reloadRotOffset = Vector3.zero;

                    if (IsReloading)
                    {
                        float p = ReloadProgress;
                        if (p < 0.35f)
                        {
                            // Phase 1: Gun dips down 0.28m into view and tilts 32° to eject magazine
                            float t = p / 0.35f;
                            reloadPosOffset = Vector3.Lerp(Vector3.zero, new Vector3(-0.06f, -0.28f, -0.12f), Mathf.SmoothStep(0f, 1f, t));
                            reloadRotOffset = Vector3.Lerp(Vector3.zero, new Vector3(32f, -18f, 22f), Mathf.SmoothStep(0f, 1f, t));
                        }
                        else if (p < 0.70f)
                        {
                            // Phase 2: Insert fresh magazine/battery (exaggerated mechanical tactical shake)
                            float t = (p - 0.35f) / 0.35f;
                            float shake = Mathf.Sin(t * Mathf.PI * 4f) * 0.024f;
                            reloadPosOffset = new Vector3(-0.06f, -0.28f + shake, -0.12f);
                            reloadRotOffset = new Vector3(32f + Mathf.Sin(t * Mathf.PI * 2f) * 5f, -18f, 22f);
                        }
                        else
                        {
                            // Phase 3: Rack slide/bolt and return smoothly to combat stance
                            float t = (p - 0.70f) / 0.30f;
                            reloadPosOffset = Vector3.Lerp(new Vector3(-0.06f, -0.28f, -0.12f), Vector3.zero, Mathf.SmoothStep(0f, 1f, t));
                            reloadRotOffset = Vector3.Lerp(new Vector3(32f, -18f, 22f), Vector3.zero, Mathf.SmoothStep(0f, 1f, t));
                        }
                    }

                    Vector3 basePos = CurrentWeapon == WeaponType.Pistol ? new Vector3(0.20f, -0.20f, 0.40f) : defaultWeaponPos;
                    activeWeapon.transform.localPosition = basePos + weaponRecoilPos + reloadPosOffset;
                    activeWeapon.transform.localRotation = Quaternion.Euler(weaponRecoilRot + reloadRotOffset);
                }

                // Trigger scheduled bolt-action cycling sound for Sniper Rifle
                if (boltActionAt > 0f && Time.time >= boltActionAt)
                {
                    boltActionAt = 0f;
                    if (audioSource != null) ProceduralAudio.PlayBoltAction(audioSource);
                }

                // Turn off muzzle flash light after duration
                if (Time.unscaledTime >= muzzleFlashUntil)
                {
                    var light = GetActiveMuzzleLight();
                    if (light != null && light.enabled) light.enabled = false;
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

            // Scoping check for Sniper Rifle
            bool shouldScope = command.ZoomPressed && CurrentWeapon == WeaponType.SniperRifle && !IsReloading && !IsDead;
            if (shouldScope != IsScoped)
            {
                IsScoped = shouldScope;
                if (IsScoped) ProceduralAudio.PlaySniperZoom(audioSource);
                UpdateActiveFpWeaponVisual();
            }

            // Look rotation with precision sensitivity scaling when scoped
            float sensMultiplier = IsScoped ? GameSettings.AdsSensitivity : 1.0f;
            kcc.AddLookRotation(new Vector2(-command.LookY * sensMultiplier, command.LookX * sensMultiplier));
            Vector2 look = kcc.GetLookRotation(true, false);
            AimPitch = look.x;

            // Weapon switching via mouse wheel scroll or Q
            if (command.WeaponScroll != 0 && !IsReloading)
            {
                CycleWeapon(command.WeaponScroll);
            }
            else if (command.SwitchWeapon && !IsReloading)
            {
                CycleWeapon(1);
            }

            // Reload key [R]
            if (command.ReloadPressed && !IsReloading && AmmoInClip < MaxClip)
            {
                if (reserveAmmo.TryGetValue(CurrentWeapon, out int res) && res > 0)
                {
                    StartReload();
                }
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

            // Apply buffered launch impulse from JumpPad inside fixed update step
            if (hasPendingLaunchImpulse)
            {
                MoveVelocity = new Vector3(pendingLaunchImpulse.x, 0f, pendingLaunchImpulse.z);
                jumpImpulse = pendingLaunchImpulse.y;
                hasPendingLaunchImpulse = false;
                JumpConsumed = true;
                CoyoteLeft = JumpBufferLeft = 0f;
            }

            // Speed boost power-up & Movement Sensitivity scaling
            float moveSens = GameSettings.MovementSensitivity;
            float currentSprintSpeed = (HasSpeedBoost ? sprintSpeed * 1.35f : sprintSpeed) * moveSens;
            float currentWalkSpeed = (HasSpeedBoost ? walkSpeed * 1.35f : walkSpeed) * moveSens;

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

            // Check Powerup and Weapon Pickups
            if (ArenaPowerup.TryCollect(transform.position, out var collectedPowerup))
            {
                HandlePowerupCollected(collectedPowerup);
            }

            // Shooting
            if (command.Fire && Time.time >= nextShotAt && !IsReloading)
            {
                Fire();
            }
        }

        private void HandlePowerupCollected(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.WeaponAssaultRifle:
                    UnlockAndEquipWeapon(WeaponType.AssaultRifle);
                    break;
                case PowerupType.WeaponPlasmaCannon:
                    UnlockAndEquipWeapon(WeaponType.PlasmaCannon);
                    break;
                case PowerupType.WeaponScattergun:
                    UnlockAndEquipWeapon(WeaponType.Scattergun);
                    break;
                case PowerupType.WeaponSniperRifle:
                    UnlockAndEquipWeapon(WeaponType.SniperRifle);
                    break;
                case PowerupType.WeaponMachineGun:
                    UnlockAndEquipWeapon(WeaponType.MachineGun);
                    break;
                case PowerupType.ShieldBoost:
                    Shield = Mathf.Min(100, Shield + 50);
                    if (combatHud != null) combatHud.ShowPickupNotification("SHIELD +50 BOOST!");
                    break;
                case PowerupType.SpeedBoost:
                    SpeedBoostTimer = TickTimer.CreateFromSeconds(Runner, 12f);
                    if (combatHud != null) combatHud.ShowPickupNotification("SUPER SPEED ACTIVE!");
                    break;
                case PowerupType.RapidFire:
                    RapidFireTimer = TickTimer.CreateFromSeconds(Runner, 12f);
                    if (combatHud != null) combatHud.ShowPickupNotification("OVERCHARGE ACTIVE!");
                    break;
            }
        }

        public void UnlockAndEquipWeapon(WeaponType weapon)
        {
            if (!unlockedWeapons.Contains(weapon))
            {
                unlockedWeapons.Add(weapon);
            }

            // Fixed total ammo allocation
            int initialReserve = weapon switch
            {
                WeaponType.AssaultRifle => 30, // 30 clip + 30 reserve = 60 total
                WeaponType.PlasmaCannon => 10, // 10 clip + 10 reserve = 20 total
                WeaponType.Scattergun => 8,    // 8 clip + 8 reserve = 16 total
                WeaponType.SniperRifle => 5,   // 5 clip + 5 reserve = 10 total
                WeaponType.MachineGun => 100,  // 50 clip + 100 reserve = 150 total
                _ => int.MaxValue
            };
            reserveAmmo[weapon] = initialReserve;
            clipAmmo[weapon] = weapon switch
            {
                WeaponType.Pistol => 12,
                WeaponType.AssaultRifle => 30,
                WeaponType.PlasmaCannon => 10,
                WeaponType.Scattergun => 8,
                WeaponType.SniperRifle => 5,
                WeaponType.MachineGun => 50,
                _ => 20
            };

            SwitchToWeapon(weapon);
            if (combatHud != null)
            {
                combatHud.ShowPickupNotification($"ACQUIRED: {weapon.ToString().ToUpper()}!");
            }
        }

        public void CycleWeapon(int direction)
        {
            if (unlockedWeapons.Count <= 1 || IsReloading) return;
            int curIdx = unlockedWeapons.IndexOf(CurrentWeapon);
            if (curIdx < 0) curIdx = 0;
            int nextIdx = (curIdx + direction + unlockedWeapons.Count) % unlockedWeapons.Count;
            SwitchToWeapon(unlockedWeapons[nextIdx]);
        }

        public void SwitchToWeapon(WeaponType weapon)
        {
            if (CurrentWeapon == weapon && AmmoInClip > 0) return;

            // Save active clip ammo
            clipAmmo[CurrentWeapon] = AmmoInClip;

            CurrentWeapon = weapon;
            AmmoInClip = clipAmmo.TryGetValue(weapon, out int clip) ? clip : MaxClip;
            IsReloading = false;
            IsScoped = false;
            UpdateActiveFpWeaponVisual();
        }

        private void Fire()
        {
            if (AmmoInClip <= 0)
            {
                if (reserveAmmo.TryGetValue(CurrentWeapon, out int res) && res > 0)
                {
                    StartReload();
                }
                else
                {
                    HandleWeaponDepleted();
                }
                return;
            }

            AmmoInClip--;
            clipAmmo[CurrentWeapon] = AmmoInClip;
            bool rapid = HasRapidFire;

            float fireInterval = CurrentWeapon switch
            {
                WeaponType.Pistol => 0.22f,
                WeaponType.AssaultRifle => (rapid ? 0.07f : 0.13f),
                WeaponType.PlasmaCannon => (rapid ? 0.25f : 0.50f),
                WeaponType.Scattergun => 0.70f,
                WeaponType.SniperRifle => (rapid ? 0.95f : 1.35f),
                WeaponType.MachineGun => (rapid ? 0.055f : 0.085f),
                _ => 0.15f
            };
            nextShotAt = Time.time + fireInterval;

            if (CurrentWeapon == WeaponType.SniperRifle)
            {
                boltActionAt = Time.time + 0.42f;
            }

            // Recoil kickback per weapon
            if (CurrentWeapon == WeaponType.SniperRifle)
            {
                weaponRecoilPos = new Vector3(0f, 0.045f, -0.14f);
                weaponRecoilRot = new Vector3(-12f, 0f, 0f);
            }
            else if (CurrentWeapon == WeaponType.Scattergun)
            {
                weaponRecoilPos = new Vector3(Random.Range(-0.02f, 0.02f), 0.035f, -0.11f);
                weaponRecoilRot = new Vector3(-8f, Random.Range(-3f, 3f), 0f);
            }
            else if (CurrentWeapon == WeaponType.MachineGun)
            {
                weaponRecoilPos = new Vector3(Random.Range(-0.02f, 0.02f), Random.Range(0.015f, 0.03f), -0.09f);
                weaponRecoilRot = new Vector3(-6f, Random.Range(-2.5f, 2.5f), 0f);
            }
            else
            {
                weaponRecoilPos = new Vector3(Random.Range(-0.015f, 0.015f), Random.Range(0.01f, 0.02f), -0.065f);
                weaponRecoilRot = new Vector3(-4.5f, Random.Range(-2f, 2f), 0f);
            }

            // Muzzle flash
            muzzleFlashUntil = Time.unscaledTime + 0.045f;
            var light = GetActiveMuzzleLight();
            if (light != null) light.enabled = true;

            // Play firing audio
            switch (CurrentWeapon)
            {
                case WeaponType.Pistol:
                    ProceduralAudio.PlayPistol(audioSource);
                    break;
                case WeaponType.AssaultRifle:
                    ProceduralAudio.PlayRifle(audioSource);
                    break;
                case WeaponType.PlasmaCannon:
                    ProceduralAudio.PlayPlasma(audioSource);
                    break;
                case WeaponType.Scattergun:
                    ProceduralAudio.PlayScattergun(audioSource);
                    break;
                case WeaponType.SniperRifle:
                    ProceduralAudio.PlaySniper(audioSource);
                    break;
                case WeaponType.MachineGun:
                    ProceduralAudio.PlayMachineGun(audioSource);
                    break;
            }

            // Raycast hit detection
            Vector3 origin = ownerCamera.transform.position;
            Vector3 forward = ownerCamera.transform.forward;

            if (CurrentWeapon == WeaponType.Scattergun)
            {
                // Scattergun: fire 6 pellets in a spread cone
                bool anyHit = false;
                for (int p = 0; p < 6; p++)
                {
                    Vector3 spread = Random.insideUnitSphere * 0.06f;
                    Vector3 pelletDir = (forward + spread).normalized;
                    if (CastSingleShot(origin, pelletDir, 12, out Vector3 pelletEnd, out bool combatantHit))
                    {
                        anyHit = true;
                    }
                    if (p == 0 && tracer != null)
                    {
                        Transform muzzle = GetActiveMuzzle();
                        Vector3 startPos = (muzzle != null && muzzle.gameObject.activeInHierarchy) ? muzzle.position : (origin + forward * 0.4f);
                        tracer.startWidth = 0.024f;
                        tracer.endWidth = 0.008f;
                        if (laserTracerMaterial != null) tracer.sharedMaterial = laserTracerMaterial;
                        tracer.SetPosition(0, startPos);
                        tracer.SetPosition(1, pelletEnd);
                        tracer.enabled = true;
                        tracerUntil = Time.unscaledTime + 0.07f;
                    }
                }
                if (anyHit) ProceduralAudio.PlayHitMarker();
                if (combatHud != null) combatHud.PulseShot(anyHit);
            }
            else
            {
                int baseDamage = CurrentWeapon switch
                {
                    WeaponType.Pistol => 22,
                    WeaponType.AssaultRifle => (rapid ? 18 : 24),
                    WeaponType.PlasmaCannon => 52,
                    WeaponType.SniperRifle => 95,
                    WeaponType.MachineGun => (rapid ? 13 : 16),
                    _ => 20
                };

                bool hitCombatant = CastSingleShot(origin, forward, baseDamage, out Vector3 endPoint, out bool isCombatant);
                if (hitCombatant) ProceduralAudio.PlayHitMarker();

                // Tracer line
                if (tracer != null)
                {
                    Vector3 startPos;
                    if (CurrentWeapon == WeaponType.SniperRifle)
                    {
                        if (IsScoped)
                        {
                            startPos = origin + forward * 0.45f - ownerCamera.transform.up * 0.06f + ownerCamera.transform.right * 0.07f;
                        }
                        else
                        {
                            Transform muzzle = GetActiveMuzzle();
                            startPos = (muzzle != null && muzzle.gameObject.activeInHierarchy) ? muzzle.position : (origin + forward * 0.45f - ownerCamera.transform.up * 0.08f + ownerCamera.transform.right * 0.12f);
                        }
                        tracer.startWidth = 0.085f;
                        tracer.endWidth = 0.035f;
                        tracerUntil = Time.unscaledTime + 0.35f;
                        if (laserTracerMaterial != null) tracer.sharedMaterial = laserTracerMaterial;
                    }
                    else
                    {
                        Transform muzzle = GetActiveMuzzle();
                        startPos = (muzzle != null && muzzle.gameObject.activeInHierarchy) ? muzzle.position : (origin + forward * 0.4f);
                        if (CurrentWeapon == WeaponType.PlasmaCannon)
                        {
                            tracer.startWidth = 0.055f;
                            tracer.endWidth = 0.025f;
                            tracerUntil = Time.unscaledTime + 0.12f;
                            if (plasmaTracerMaterial != null) tracer.sharedMaterial = plasmaTracerMaterial;
                        }
                        else
                        {
                            tracer.startWidth = 0.024f;
                            tracer.endWidth = 0.008f;
                            tracerUntil = Time.unscaledTime + 0.07f;
                            if (laserTracerMaterial != null) tracer.sharedMaterial = laserTracerMaterial;
                        }
                    }
                    tracer.SetPosition(0, startPos);
                    tracer.SetPosition(1, endPoint);
                    tracer.enabled = true;
                }

                if (combatHud != null) combatHud.PulseShot(hitCombatant);
            }

            if (AmmoInClip <= 0)
            {
                if (reserveAmmo.TryGetValue(CurrentWeapon, out int res) && res > 0)
                {
                    StartReload();
                }
                else
                {
                    HandleWeaponDepleted();
                }
            }
        }

        private void HandleWeaponDepleted()
        {
            if (CurrentWeapon == WeaponType.Pistol)
            {
                StartReload();
                return;
            }

            WeaponType depleted = CurrentWeapon;
            unlockedWeapons.Remove(depleted);
            ProceduralAudio.PlayWeaponDepleted();
            if (combatHud != null)
            {
                combatHud.ShowPickupNotification($"{depleted.ToString().ToUpper()} AMMO DEPLETED! REVERTING TO PISTOL");
            }
            SwitchToWeapon(WeaponType.Pistol);
        }

        private bool CastSingleShot(Vector3 origin, Vector3 direction, int damage, out Vector3 endPoint, out bool hitCombatant)
        {
            endPoint = origin + direction * 60f;
            hitCombatant = false;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, 80f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.isTrigger && h.collider.GetComponentInParent<JumpPad>() != null) continue;
                if (h.collider.isTrigger && h.collider.GetComponentInParent<ArenaPowerup>() != null) continue;

                endPoint = h.point;
                var target = h.collider.GetComponentInParent<SharedPlayerAvatar>();
                if (target == this) target = null;
                var botTarget = h.collider.GetComponentInParent<ArenaBot>();

                if (target != null && !target.IsDead)
                {
                    hitCombatant = true;
                    target.ApplyDamage(damage, Runner.LocalPlayer.PlayerId, CurrentWeapon);
                    return true;
                }
                else if (botTarget != null)
                {
                    hitCombatant = true;
                    botTarget.ApplyDamage(damage, Runner.LocalPlayer.PlayerId, CurrentWeapon);
                    return true;
                }
                break;
            }
            return false;
        }

        public void StartReload()
        {
            if (IsReloading || AmmoInClip == MaxClip) return;
            if (!reserveAmmo.TryGetValue(CurrentWeapon, out int res) || res <= 0) return;

            IsReloading = true;
            IsScoped = false;
            reloadDuration = CurrentWeapon switch
            {
                WeaponType.Pistol => 1.1f,
                WeaponType.AssaultRifle => 1.4f,
                WeaponType.PlasmaCannon => 1.9f,
                WeaponType.Scattergun => 1.7f,
                WeaponType.SniperRifle => 2.1f,
                _ => 1.4f
            };
            reloadStartTime = Time.time;
            reloadCompleteAt = reloadStartTime + reloadDuration;
            ProceduralAudio.PlayReload(audioSource);
        }

        public void TakeDamage(int damage, int attackerPlayerId, WeaponType weaponUsed)
        {
            if (IsDead || IsInvulnerable) return;

            ProceduralAudio.PlayDamage();

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

        public void ApplyDamage(int damage, int attackerPlayerId, WeaponType weaponUsed)
        {
            TakeDamage(damage, attackerPlayerId, weaponUsed);
        }

        private void OnEliminated(int attackerPlayerId, WeaponType weaponUsed)
        {
            Deaths++;
            respawnAt = Time.time + 3.0f;

            // Reset weapon inventory: weapons found from powerups are lost upon death!
            unlockedWeapons.Clear();
            unlockedWeapons.Add(WeaponType.Pistol);
            clipAmmo.Clear();
            reserveAmmo.Clear();
            reserveAmmo[WeaponType.Pistol] = int.MaxValue;
            CurrentWeapon = WeaponType.Pistol;

            // Award killer
            string killerName = $"Player {attackerPlayerId + 1}";
            if (attackerPlayerId >= 100)
            {
                var killerBot = ArenaBot.AllBots.Find(b => b.botId == attackerPlayerId);
                killerName = killerBot != null ? $"[BOT] {killerBot.botName}" : $"[BOT #{attackerPlayerId}]";
            }
            else
            {
                var avatars = FindObjectsByType<SharedPlayerAvatar>();
                for (int i = 0; i < avatars.Length; i++)
                {
                    if (avatars[i].Object != null && avatars[i].Object.InputAuthority.PlayerId == attackerPlayerId)
                    {
                        avatars[i].AwardElimination();
                        killerName = avatars[i].PlayerDisplayName;
                        break;
                    }
                }
            }

            // Broadcast kill feed event
            if (SharedMatchManager.Instance != null)
            {
                SharedMatchManager.Instance.ReportKill(killerName, PlayerDisplayName, weaponUsed.ToString());
            }
        }

        public void AwardElimination()
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
            unlockedWeapons.Clear();
            unlockedWeapons.Add(WeaponType.Pistol);
            clipAmmo.Clear();
            reserveAmmo.Clear();
            reserveAmmo[WeaponType.Pistol] = int.MaxValue;
            CurrentWeapon = WeaponType.Pistol;
            AmmoInClip = MaxClip;
            clipAmmo[WeaponType.Pistol] = MaxClip;
            IsReloading = false;
            IsScoped = false;
            MoveVelocity = Vector3.zero;
            UpdateActiveFpWeaponVisual();

            int slot = Random.Range(0, 8);
            var spawn = new Vector3(-16f + (slot % 4) * 10f, 0.2f, -16f + (slot / 4) * 32f);
            kcc.SetPosition(spawn);

            // 2 seconds invulnerability upon respawn
            InvulnerableTimer = TickTimer.CreateFromSeconds(Runner, 2.0f);
        }

        // JumpPad launch impulse is buffered and applied safely inside FixedUpdateNetwork
        public void ApplyLaunchImpulse(Vector3 impulse)
        {
            pendingLaunchImpulse = impulse;
            hasPendingLaunchImpulse = true;
        }

        private void LateUpdate()
        {
            if (ownerCamera == null) return;
            Vector2 look = kcc.GetLookRotation(true, false);
            ownerCamera.transform.localRotation = Quaternion.Euler(look.x, 0f, 0f);
        }

        private void OnGUI()
        {
            // For remote proxies: draw occluded overhead health bar
            if (Object == null || Object.HasStateAuthority || IsDead) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.1f;
            Vector3 camPos = cam.transform.position;
            float dist = Vector3.Distance(camPos, worldPos);
            if (dist > 32f) return;

            // Linecast occlusion check
            if (Physics.Linecast(camPos, worldPos, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(transform)) return;
            }

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0.4f)
            {
                float w = 110f;
                float h = 12f;
                float x = screenPos.x - w / 2f;
                float y = Screen.height - screenPos.y;

                var nameStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.3f, 0.9f, 1f) }
                };
                GUI.Label(new Rect(x - 20f, y - 18f, w + 40f, 18f), PlayerDisplayName, nameStyle);

                GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
                GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);

                float healthPct = (float)Health / 100f;
                GUI.color = healthPct > 0.4f ? new Color(0.2f, 0.88f, 0.35f) : new Color(0.95f, 0.2f, 0.2f);
                GUI.DrawTexture(new Rect(x, y, w * healthPct, h * 0.6f), Texture2D.whiteTexture);

                if (Shield > 0)
                {
                    float shieldPct = (float)Shield / 50f;
                    GUI.color = new Color(0.2f, 0.82f, 1f);
                    GUI.DrawTexture(new Rect(x, y + h * 0.6f, w * shieldPct, h * 0.4f), Texture2D.whiteTexture);
                }

                GUI.color = Color.white;
            }
        }

        private void OnApplicationFocus(bool focused) { if (!focused) commands.Clear(); }
        private void OnApplicationPause(bool paused) { if (paused) commands.Clear(); }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            AllAvatars.Remove(this);
            commands.Clear();
            if (source != null) Destroy(source);
            if (ownerCamera != null) Destroy(ownerCamera.gameObject);
            if (tpCharacter != null) Destroy(tpCharacter);
        }

        private void OnDestroy()
        {
            AllAvatars.Remove(this);
        }
    }
}
