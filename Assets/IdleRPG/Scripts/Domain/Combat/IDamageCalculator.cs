using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Combat
{
    public interface IDamageCalculator
    {
        DamageResult CalculateBasicAttack(StatBlock _Attacker, StatBlock _Defender, float _CriticalRoll);
    }
}
