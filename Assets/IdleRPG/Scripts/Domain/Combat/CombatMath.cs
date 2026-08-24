using System;
using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Combat
{
    public static class CombatMath
    {
        public static DamageResult CalculateBasicAttack(StatBlock _Attacker, StatBlock _Defender, float _CriticalRoll)
        {
            float mitigatedDamage = Math.Max(1f, _Attacker.AttackPower - _Defender.Defense);
            bool isCritical = _CriticalRoll < _Attacker.CriticalChance;
            float multiplier = isCritical ? _Attacker.CriticalMultiplier : 1f;
            float finalDamage = (float)Math.Round(mitigatedDamage * multiplier, 2);

            return new DamageResult(finalDamage, isCritical);
        }
    }
}
