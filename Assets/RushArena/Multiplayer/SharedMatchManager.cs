using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace RushArena.Multiplayer
{
    public struct KillFeedEntry
    {
        public string Killer;
        public string Victim;
        public string Weapon;
        public float Timestamp;
    }

    // Authoritative match cycle: 120-second match, kill feed, and round transitions in Shared Mode.
    public sealed class SharedMatchManager : NetworkBehaviour
    {
        public static SharedMatchManager Instance { get; private set; }

        public const float MatchDurationSeconds = 120f;
        public const float PostMatchDurationSeconds = 10f;

        [Networked] public TickTimer MatchTimer { get; set; }
        [Networked] public TickTimer RestartTimer { get; set; }
        [Networked] public NetworkBool IsMatchActive { get; set; }
        [Networked] public NetworkBool IsMatchOver { get; set; }
        [Networked] public int WinnerPlayerNumber { get; set; }

        public string WinnerName => WinnerPlayerNumber >= 0 ? $"Player {WinnerPlayerNumber + 1}" : "Nobody";

        public readonly List<KillFeedEntry> KillFeed = new List<KillFeedEntry>();

        public override void Spawned()
        {
            Instance = this;
            if (Object.HasStateAuthority && !IsMatchActive && !IsMatchOver)
            {
                StartNewMatch();
            }
        }

        private void StartNewMatch()
        {
            IsMatchActive = true;
            IsMatchOver = false;
            WinnerPlayerNumber = -1;
            MatchTimer = TickTimer.CreateFromSeconds(Runner, MatchDurationSeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            if (IsMatchActive && MatchTimer.Expired(Runner))
            {
                EndMatch();
            }
            else if (IsMatchOver && RestartTimer.Expired(Runner))
            {
                RestartMatch();
            }
        }

        private void EndMatch()
        {
            IsMatchActive = false;
            IsMatchOver = true;
            RestartTimer = TickTimer.CreateFromSeconds(Runner, PostMatchDurationSeconds);

            // Determine winner
            var avatars = FindObjectsByType<SharedPlayerAvatar>();
            SharedPlayerAvatar topPlayer = null;
            int highestScore = -1;
            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i].Score > highestScore)
                {
                    highestScore = avatars[i].Score;
                    topPlayer = avatars[i];
                }
            }

            WinnerPlayerNumber = topPlayer != null ? topPlayer.PlayerNumber : -1;
        }

        private void RestartMatch()
        {
            var avatars = FindObjectsByType<SharedPlayerAvatar>();
            for (int i = 0; i < avatars.Length; i++)
            {
                avatars[i].ResetStats();
            }
            StartNewMatch();
        }

        public void ReportKill(string killerName, string victimName, string weaponName)
        {
            KillFeed.Add(new KillFeedEntry
            {
                Killer = killerName,
                Victim = victimName,
                Weapon = weaponName,
                Timestamp = Time.time
            });

            if (KillFeed.Count > 6) KillFeed.RemoveAt(0);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }
    }
}
