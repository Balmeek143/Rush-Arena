using UnityEngine;

namespace RushArena.Runtime
{
    public static class GameSettings
    {
        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat("Rush_Sensitivity", 1.0f);
            set { PlayerPrefs.SetFloat("Rush_Sensitivity", Mathf.Clamp(value, 0.2f, 3.5f)); PlayerPrefs.Save(); }
        }

        public static float FieldOfView
        {
            get => PlayerPrefs.GetFloat("Rush_FOV", 85f);
            set { PlayerPrefs.SetFloat("Rush_FOV", Mathf.Clamp(value, 70f, 110f)); PlayerPrefs.Save(); }
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat("Rush_Volume", 1.0f);
            set { PlayerPrefs.SetFloat("Rush_Volume", Mathf.Clamp01(value)); AudioListener.volume = value; PlayerPrefs.Save(); }
        }

        public static int BotDifficultyLevel
        {
            get => PlayerPrefs.GetInt("Rush_BotDifficulty", 1); // 0=Easy, 1=Medium, 2=Hard
            set { PlayerPrefs.SetInt("Rush_BotDifficulty", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); }
        }

        public static int DesiredBotCount
        {
            get => PlayerPrefs.GetInt("Rush_BotCount", 3);
            set { PlayerPrefs.SetInt("Rush_BotCount", Mathf.Clamp(value, 0, 8)); PlayerPrefs.Save(); }
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt("Rush_InvertY", 0) == 1;
            set { PlayerPrefs.SetInt("Rush_InvertY", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool TouchControlsEnabled
        {
            get
            {
                if (!PlayerPrefs.HasKey("Rush_TouchControls"))
                {
                    bool isMobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
                    PlayerPrefs.SetInt("Rush_TouchControls", isMobile ? 1 : 0);
                    PlayerPrefs.Save();
                }
                return PlayerPrefs.GetInt("Rush_TouchControls", 0) == 1;
            }
            set
            {
                PlayerPrefs.SetInt("Rush_TouchControls", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
