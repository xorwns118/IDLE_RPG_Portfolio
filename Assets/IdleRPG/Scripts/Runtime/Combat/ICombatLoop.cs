using IdleRPG.Domain;

namespace IdleRPG.Runtime.Combat
{
    public interface ICombatLoop
    {
        CombatLoopMode Mode { get; }
        bool IsRuntimeActive { get; }
        void SetRuntimeActive(bool _Active);
    }
}
