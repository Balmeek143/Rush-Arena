namespace RushArena.Core
{
    // Capture once per rendered input frame; consume once per authoritative simulation tick.
    // Local-only state: this is not a rollback history or a network input transport.
    public sealed class TickInputBuffer
    {
        private PlayerCommand pending;

        public void Capture(PlayerCommand frame, bool active = true)
        {
            if (!active) { Clear(); return; }
            pending.MoveX = frame.MoveX;
            pending.MoveY = frame.MoveY;
            pending.Sprint = frame.Sprint;
            pending.Fire = frame.Fire;
            pending.SwitchWeapon |= frame.SwitchWeapon;
            pending.WeaponScroll += frame.WeaponScroll;
            pending.LookX += frame.LookX;
            pending.LookY += frame.LookY;
            pending.JumpPressed |= frame.JumpPressed;
        }

        public PlayerCommand Consume()
        {
            var command = pending;
            // Held controls survive catch-up ticks; displacement and edges fire once.
            pending.LookX = pending.LookY = 0f;
            pending.JumpPressed = false;
            pending.SwitchWeapon = false;
            pending.WeaponScroll = 0;
            return command;
        }

        // Call immediately on focus loss, pause, despawn, disconnect or authority loss.
        public void Clear() { pending = default; }
    }
}
