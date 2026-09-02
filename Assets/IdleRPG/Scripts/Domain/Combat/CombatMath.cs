using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Combat
{
    public static class CombatMath
    {
        private static readonly IDamageCalculator DefaultCalculator = new BasicDamageCalculator();

        public static DamageResult CalculateBasicAttack(StatBlock _Attacker, StatBlock _Defender, float _CriticalRoll)
        {
            return DefaultCalculator.CalculateBasicAttack(_Attacker, _Defender, _CriticalRoll);
        }
    }
}
