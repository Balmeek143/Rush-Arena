using System;
using System.Collections.Generic;
using RushArena.Core;
using RushArena.Runtime.Audio;
using UnityEngine;

namespace RushArena.Multiplayer
{
    // Autonomous AI Combat Droid for solo practice and arena multiplayer.
    [RequireComponent(typeof(CharacterController))]
    public sealed class ArenaBot : MonoBehaviour
    {
        public static readonly List<ArenaBot> AllBots = new List<ArenaBot>();

        public enum BotState
        {
            Patrol,
            Hunt,
            Attack,
            Dead
        }

        [Header("Bot Identity")]
        public string botName = "BOT Alpha";
        public int botId = 100;

        [Header("Stats")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int maxShield = 50;
        public int currentShield = 50;
        public WeaponType weapon = WeaponType.AssaultRifle;

        [Header("Movement")]
        public float walkSpeed = 6.0f;
        public float sprintSpeed = 9.0f;
        public float gravity = 25.0f;

        // Internal AI components
        private CharacterController controller;
        private AudioSource audioSource;
        private BotState state = BotState.Patrol;

        // Visuals
        private GameObject characterVisual;
        private Transform upperBody;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform weaponObj;
        private LineRenderer tracer;
        private float tracerUntil;
        private float strideCycle;

        // Waypoints and targeting
        private Vector3 targetWaypoint;
        private float nextWaypointTime;
        private Transform currentEnemy;
        private float nextFireTime;
        private float burstEndTime;
        private bool isBursting;
        private float respawnTime;
        private Vector3 spawnPoint;
        private float verticalVelocity;

        // Visual Materials
        public Material armorMaterial;
        public Material visorMaterial;
        public Material weaponMaterial;
        public Material tracerMaterial;

        // Tactical waypoints in the 54m x 54m arena
        private static readonly Vector3[] TacticalWaypoints = new Vector3[]
        {
            new Vector3(0f, 3.8f, 0f),      // Central platform center
            new Vector3(4f, 3.8f, 4f),      // Central platform NE
            new Vector3(-4f, 3.8f, -4f),    // Central platform SW
            new Vector3(0f, 0.5f, 16f),     // North lane
            new Vector3(0f, 0.5f, -16f),    // South lane
            new Vector3(16f, 0.5f, 0f),     // East courtyard
            new Vector3(-16f, 0.5f, 0f),    // West courtyard
            new Vector3(-14f, 0.5f, 14f),   // NW jump pad approach
            new Vector3(14f, 0.5f, 14f),    // NE jump pad approach
            new Vector3(-14f, 0.5f, -14f),  // SW jump pad approach
            new Vector3(14f, 0.5f, -14f),   // SE jump pad approach
            new Vector3(0f, 1.8f, 11f),     // North ramp
            new Vector3(0f, 1.8f, -11f),    // South ramp
        };

        public static ArenaBot Create(Vector3 position, string name, int id, WeaponType weaponType,
            Material armor, Material visor, Material weaponMat, Material tracerMat)
        {
            var botObj = new GameObject("Bot_" + name);
            botObj.transform.position = position;

            var bot = botObj.AddComponent<ArenaBot>();
            bot.botName = name;
            bot.botId = id;
            bot.weapon = weaponType;
            bot.armorMaterial = armor;
            bot.visorMaterial = visor;
            bot.weaponMaterial = weaponMat;
            bot.tracerMaterial = tracerMat;

            return bot;
        }

        private void Awake()
        {
            AllBots.Add(this);
            spawnPoint = transform.position;
            controller = GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.38f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 45f;
            controller.minMoveDistance = 0f;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 35f;

            // Build 3D combatant character model
            characterVisual = ProceduralVisuals.BuildThirdPersonCharacter(transform, armorMaterial, visorMaterial, weaponMaterial,
                out upperBody, out leftLeg, out rightLeg, out weaponObj);

            // Bullet tracer
            tracer = gameObject.AddComponent<LineRenderer>();
            tracer.positionCount = 2;
            tracer.startWidth = 0.024f;
            tracer.endWidth = 0.008f;
            tracer.useWorldSpace = true;
            tracer.enabled = false;
            if (tracerMaterial != null) tracer.sharedMaterial = tracerMaterial;

            // Pick initial waypoint
            PickNewWaypoint();
        }

        private void OnDestroy()
        {
            AllBots.Remove(this);
        }

        private void Update()
        {
            // Fall protection
            if (transform.position.y < -5f)
            {
                controller.enabled = false;
                transform.position = spawnPoint + Vector3.up * 0.5f;
                controller.enabled = true;
                verticalVelocity = 0f;
            }

            if (state == BotState.Dead)
            {
                if (Time.time >= respawnTime)
                {
                    Respawn();
                }
                return;
            }

            // Disable tracer when expired
            if (tracer != null && tracer.enabled && Time.unscaledTime >= tracerUntil)
            {
                tracer.enabled = false;
            }

            // Find closest player enemy
            FindTarget();

            // Run AI State
            switch (state)
            {
                case BotState.Patrol:
                    UpdatePatrol();
                    break;
                case BotState.Hunt:
                case BotState.Attack:
                    UpdateCombat();
                    break;
            }

            // Animate stride legs
            AnimateLegs();
        }

        private void FindTarget()
        {
            // Look for nearest alive human player
            var players = FindObjectsByType<SharedPlayerAvatar>();
            float closestDist = 28f;
            Transform bestTarget = null;

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null || p.IsDead) continue;
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < closestDist)
                {
                    // Check line of sight
                    Vector3 eyePos = transform.position + Vector3.up * 1.6f;
                    Vector3 toTarget = (p.transform.position + Vector3.up * 1.2f) - eyePos;
                    if (!Physics.Raycast(eyePos, toTarget.normalized, d - 0.5f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        closestDist = d;
                        bestTarget = p.transform;
                    }
                }
            }

            currentEnemy = bestTarget;
            if (currentEnemy != null)
            {
                state = closestDist <= 18f ? BotState.Attack : BotState.Hunt;
            }
            else if (state != BotState.Patrol)
            {
                state = BotState.Patrol;
                PickNewWaypoint();
            }
        }

        private void UpdatePatrol()
        {
            Vector3 diff = targetWaypoint - transform.position;
            diff.y = 0f;

            if (diff.magnitude < 1.5f || Time.time >= nextWaypointTime)
            {
                PickNewWaypoint();
            }

            MoveTowards(targetWaypoint, walkSpeed);
        }

        private void UpdateCombat()
        {
            if (currentEnemy == null)
            {
                state = BotState.Patrol;
                return;
            }

            Vector3 toEnemy = currentEnemy.position - transform.position;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;

            // Face enemy smoothly
            if (toEnemy.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toEnemy.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
            }

            // Aim upper body pitch at enemy head/torso
            if (upperBody != null)
            {
                Vector3 eyePos = transform.position + Vector3.up * 1.5f;
                Vector3 targetHead = currentEnemy.position + Vector3.up * 1.2f;
                Vector3 aimDir = (targetHead - eyePos).normalized;
                float pitch = -Mathf.Asin(Mathf.Clamp(aimDir.y, -0.99f, 0.99f)) * Mathf.Rad2Deg;
                upperBody.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            // Strafe or advance
            Vector3 moveTarget;
            if (dist > 14f)
            {
                // Advance
                moveTarget = currentEnemy.position;
            }
            else if (dist < 6f)
            {
                // Back away slightly
                moveTarget = transform.position - toEnemy.normalized * 4f;
            }
            else
            {
                // Strafe left/right
                Vector3 side = Vector3.Cross(Vector3.up, toEnemy.normalized);
                float strafeDir = Mathf.Sin(Time.time * 1.8f) > 0f ? 1f : -1f;
                moveTarget = transform.position + side * strafeDir * 5f;
            }

            MoveTowards(moveTarget, sprintSpeed);

            // Fire weapons in bursts
            if (dist < 26f)
            {
                ExecuteBotFiring();
            }
        }

        private void ExecuteBotFiring()
        {
            if (isBursting)
            {
                if (Time.time < burstEndTime)
                {
                    if (Time.time >= nextFireTime)
                    {
                        float cadence = weapon == WeaponType.AssaultRifle ? 0.15f : 0.5f;
                        nextFireTime = Time.time + cadence;
                        FireAtTarget();
                    }
                }
                else
                {
                    isBursting = false;
                    nextFireTime = Time.time + UnityEngine.Random.Range(1.2f, 2.2f);
                }
            }
            else if (Time.time >= nextFireTime)
            {
                isBursting = true;
                burstEndTime = Time.time + (weapon == WeaponType.AssaultRifle ? UnityEngine.Random.Range(0.6f, 1.2f) : 0.6f);
            }
        }

        private void FireAtTarget()
        {
            if (currentEnemy == null) return;

            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 targetCenter = currentEnemy.position + Vector3.up * 1.1f;

            // Humanized bot aim error (slight inaccuracy)
            Vector3 spread = UnityEngine.Random.insideUnitSphere * 0.45f;
            Vector3 shootDir = ((targetCenter + spread) - eyePos).normalized;

            // Play firing sound
            if (weapon == WeaponType.AssaultRifle)
                ProceduralAudio.PlayRifle(audioSource);
            else
                ProceduralAudio.PlayPlasma(audioSource);

            // Raycast shot
            RaycastHit hit;
            Vector3 endPoint = eyePos + shootDir * 40f;
            if (Physics.Raycast(eyePos, shootDir, out hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;
                var player = hit.collider.GetComponentInParent<SharedPlayerAvatar>();
                if (player != null && !player.IsDead)
                {
                    int dmg = weapon == WeaponType.AssaultRifle ? 9 : 18;
                    player.TakeDamage(dmg, botId, weapon);
                }
            }

            // Draw tracer
            if (tracer != null)
            {
                tracer.SetPosition(0, eyePos + transform.forward * 0.4f);
                tracer.SetPosition(1, endPoint);
                tracer.enabled = true;
                tracerUntil = Time.unscaledTime + 0.05f;
            }
        }

        private void MoveTowards(Vector3 destination, float speed)
        {
            if (!controller.enabled) return;

            Vector3 direction = (destination - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.04f)
            {
                direction.Normalize();
                if (state == BotState.Patrol)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 6f * Time.deltaTime);
                }
            }
            else
            {
                direction = Vector3.zero;
            }

            // Gravity & Grounding
            if (controller.isGrounded)
            {
                verticalVelocity = -2.0f; // Firm ground snap
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            Vector3 move = direction * speed + Vector3.up * verticalVelocity;
            controller.Move(move * Time.deltaTime);

            // Check JumpPad overlap
            CheckJumpPad();
        }

        private void CheckJumpPad()
        {
            for (int i = 0; i < JumpPad.All.Count; i++)
            {
                var pad = JumpPad.All[i];
                if (pad == null) continue;
                float d = Vector3.Distance(transform.position, pad.transform.position);
                if (d < 1.6f)
                {
                    verticalVelocity = 14f;
                    controller.Move((pad.transform.forward * 8f + Vector3.up * 14f) * Time.deltaTime);
                    ProceduralAudio.PlayJumpPad();
                    break;
                }
            }
        }

        private void AnimateLegs()
        {
            float speed = controller.velocity.magnitude;
            if (speed > 0.4f && controller.isGrounded)
            {
                strideCycle += speed * Time.deltaTime * 3.5f;
                float angle = Mathf.Sin(strideCycle) * 26f;
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(angle, 0f, 0f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-angle, 0f, 0f);
            }
            else
            {
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(leftLeg.localRotation, Quaternion.identity, 10f * Time.deltaTime);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Lerp(rightLeg.localRotation, Quaternion.identity, 10f * Time.deltaTime);
            }
        }

        public void ApplyDamage(int damage, int attackerId, WeaponType weaponUsed)
        {
            if (state == BotState.Dead) return;

            ProceduralAudio.PlayDamage();

            if (currentShield > 0)
            {
                if (currentShield >= damage)
                {
                    currentShield -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= currentShield;
                    currentShield = 0;
                }
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);

            // Report elimination when health reaches 0
            if (currentHealth <= 0)
            {
                Eliminate(attackerId);
            }
        }

        private void Eliminate(int killerId)
        {
            state = BotState.Dead;
            respawnTime = Time.time + 3.0f;

            if (characterVisual != null) characterVisual.SetActive(false);
            if (controller != null) controller.enabled = false;

            // Report to match manager
            if (SharedMatchManager.Instance != null)
            {
                SharedMatchManager.Instance.RpcReportKill($"Player {killerId + 1}", $"[BOT] {botName}", weapon.ToString());
            }

            // Award kill to attacker
            var players = FindObjectsByType<SharedPlayerAvatar>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].PlayerNumber == killerId)
                {
                    players[i].Kills++;
                    players[i].Score += 100;
                    break;
                }
            }
        }

        private void Respawn()
        {
            currentHealth = maxHealth;
            currentShield = maxShield;
            state = BotState.Patrol;

            // Respawn at a random tactical waypoint
            int idx = UnityEngine.Random.Range(0, TacticalWaypoints.Length);
            transform.position = TacticalWaypoints[idx] + Vector3.up * 0.5f;

            if (controller != null) controller.enabled = true;
            if (characterVisual != null) characterVisual.SetActive(true);

            PickNewWaypoint();
        }

        private void PickNewWaypoint()
        {
            int idx = UnityEngine.Random.Range(0, TacticalWaypoints.Length);
            targetWaypoint = TacticalWaypoints[idx];
            nextWaypointTime = Time.time + UnityEngine.Random.Range(6f, 12f);
        }

        private void OnGUI()
        {
            if (state == BotState.Dead) return;

            // Draw overhead health/shield bar billboard
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.1f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0f && screenPos.z < 35f)
            {
                float w = 110f;
                float h = 12f;
                float x = screenPos.x - w / 2f;
                float y = Screen.height - screenPos.y;

                // Name label
                var nameStyle = new GUIStyle(GUI.skin.label);
                nameStyle.alignment = TextAnchor.MiddleCenter;
                nameStyle.fontSize = 11;
                nameStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
                GUI.Label(new Rect(x - 20f, y - 18f, w + 40f, 18f), $"[BOT] {botName}", nameStyle);

                // Background
                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);

                // Health bar (green/red)
                float healthPct = (float)currentHealth / maxHealth;
                GUI.color = healthPct > 0.4f ? new Color(0.2f, 0.85f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
                GUI.DrawTexture(new Rect(x, y, w * healthPct, h * 0.6f), Texture2D.whiteTexture);

                // Shield bar (cyan)
                if (currentShield > 0)
                {
                    float shieldPct = (float)currentShield / maxShield;
                    GUI.color = new Color(0.2f, 0.8f, 1f);
                    GUI.DrawTexture(new Rect(x, y + h * 0.6f, w * shieldPct, h * 0.4f), Texture2D.whiteTexture);
                }

                GUI.color = Color.white;
            }
        }
    }
}
