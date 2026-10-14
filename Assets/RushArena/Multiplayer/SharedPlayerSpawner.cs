using System.Collections.Generic;
using Fusion;
using RushArena.Core;
using UnityEngine;

namespace RushArena.Multiplayer
{
    public sealed class SharedPlayerSpawner : SimulationBehaviour, IPlayerJoined
    {
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private NetworkObject matchManagerPrefab;
        private bool botsSpawned;

        public static readonly Vector3[] TacticalSpawns = new Vector3[]
        {
            new Vector3(-12f, 1.0f, -18f),
            new Vector3( 12f, 1.0f, -18f),
            new Vector3(-18f, 1.0f,   0f),
            new Vector3( 18f, 1.0f,   0f),
            new Vector3(-12f, 1.0f,  18f),
            new Vector3( 12f, 1.0f,  18f),
            new Vector3(  0f, 4.5f,  -2f),
            new Vector3(  0f, 4.5f,   2f),
        };

        public void Configure(NetworkObject prefab, NetworkObject managerPrefab = null)
        {
            playerPrefab = prefab;
            matchManagerPrefab = managerPrefab;
        }

        void IPlayerJoined.PlayerJoined(PlayerRef player)
        {
            if (player != Runner.LocalPlayer || playerPrefab == null) return;

            if (matchManagerPrefab != null && Runner.IsSharedModeMasterClient && SharedMatchManager.Instance == null)
            {
                Runner.Spawn(matchManagerPrefab, Vector3.zero, Quaternion.identity);
            }

            int index = Mathf.Abs(player.RawEncoded % TacticalSpawns.Length);
            Vector3 spawn = TacticalSpawns[index];
            NetworkObject avatar = Runner.Spawn(playerPrefab, spawn, Quaternion.identity, player);
            Runner.SetPlayerObject(player, avatar);

            // Auto-spawn 3 AI bots for single-player practice / host testing
            if (Runner.IsSharedModeMasterClient && !botsSpawned)
            {
                botsSpawned = true;
                SpawnPracticeBots(3);
            }
        }

        public void SpawnPracticeBots(int count)
        {
            if (playerPrefab == null) return;
            var avatar = playerPrefab.GetComponent<SharedPlayerAvatar>();
            Material armor = avatar != null ? avatar.armorMaterial : null;
            Material visor = avatar != null ? avatar.visorMaterial : null;
            Material rifle = avatar != null ? avatar.rifleMaterial : null;
            Material tracer = avatar != null ? avatar.laserTracerMaterial : null;

            string[] names = new[] { "Nexus-9", "Viper-4", "Cipher-7", "Apex-1", "Titan-3" };

            for (int i = 0; i < count; i++)
            {
                int spawnIdx = (ArenaBot.AllBots.Count + i + 2) % TacticalSpawns.Length;
                Vector3 pos = TacticalSpawns[spawnIdx];
                string botName = names[ArenaBot.AllBots.Count % names.Length];
                WeaponType w = (i % 2 == 0) ? WeaponType.AssaultRifle : WeaponType.PlasmaCannon;
                ArenaBot.Create(pos, botName, 100 + ArenaBot.AllBots.Count, w, armor, visor, rifle, tracer);
            }
        }
    }
}
