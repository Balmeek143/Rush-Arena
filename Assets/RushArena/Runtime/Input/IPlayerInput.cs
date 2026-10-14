using RushArena.Core;

namespace RushArena.Input
{
    public interface IPlayerInput
    {
        bool IsActive { get; }
        PlayerCommand Sample();
        void Clear();
    }
}
