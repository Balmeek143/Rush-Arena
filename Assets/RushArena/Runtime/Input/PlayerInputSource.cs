using RushArena.Core;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace RushArena.Input
{
    public sealed class PlayerInputSource : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private bool forceTouchInEditor;
        private IPlayerInput device;
        private bool touchEnabled;
        public MobileInput Mobile { get; private set; }
        public bool IsActive => device != null && device.IsActive;
        public void ConfigureTouch(bool enabled)
        {
            forceTouchInEditor = enabled;
            if (enabled) RushArena.Runtime.GameSettings.TouchControlsEnabled = true;
        }
        private void OnEnable()
        {
            device = new DesktopInput();
        }
        public PlayerCommand Sample() => device == null ? default : device.Sample();
        public void Clear() => device?.Clear();
        private void OnApplicationFocus(bool focused) { if (!focused) Clear(); }
        private void OnApplicationPause(bool paused) { if (paused) Clear(); }
        private void OnDisable()
        {
            Clear();
            device = null;
        }
    }
}
