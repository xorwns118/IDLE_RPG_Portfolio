using System;

namespace IdleRPG.Domain.Actors
{
    public readonly struct StatBlock
    {
        public float MaxHp { get; }
        public float AttackPower { get; }
        public float Defense { get; }
        public float AttackRange { get; }
        public float AttackInterval { get; }
        public float MoveSpeed { get; }
        public float CriticalChance { get; }
        public float CriticalMultiplier { get; }

        public StatBlock(float _MaxHp, float _AttackPower, float _Defense, float _AttackRange,
            float _AttackInterval, float _MoveSpeed, float _CriticalChance, float _CriticalMultiplier)
        {
            MaxHp = Math.Max(1f, _MaxHp);
            AttackPower = Math.Max(0f, _AttackPower);
            Defense = Math.Max(0f, _Defense);
            AttackRange = Math.Max(0.1f, _AttackRange);
            AttackInterval = Math.Max(0.1f, _AttackInterval);
            MoveSpeed = Math.Max(0f, _MoveSpeed);
            CriticalChance = Clamp01(_CriticalChance);
            CriticalMultiplier = Math.Max(1f, _CriticalMultiplier);
        }

        public StatBlock Scale(float _HpMultiplier, float _AttackMultiplier, float _DefenseMultiplier)
        {
            return new StatBlock(
                MaxHp * Math.Max(0.1f, _HpMultiplier),
                AttackPower * Math.Max(0.1f, _AttackMultiplier),
                Defense * Math.Max(0f, _DefenseMultiplier),
                AttackRange,
                AttackInterval,
                MoveSpeed,
                CriticalChance,
                CriticalMultiplier);
        }

        public StatBlock Apply(StatModifier _Modifier)
        {
            return new StatBlock(
                ApplyStat(MaxHp, _Modifier.MaxHpAdd, _Modifier.MaxHpMultiplier),
                ApplyStat(AttackPower, _Modifier.AttackPowerAdd, _Modifier.AttackPowerMultiplier),
                ApplyStat(Defense, _Modifier.DefenseAdd, _Modifier.DefenseMultiplier),
                ApplyStat(AttackRange, _Modifier.AttackRangeAdd, _Modifier.AttackRangeMultiplier),
                ApplyStat(AttackInterval, _Modifier.AttackIntervalAdd, _Modifier.AttackIntervalMultiplier),
                ApplyStat(MoveSpeed, _Modifier.MoveSpeedAdd, _Modifier.MoveSpeedMultiplier),
                ApplyStat(CriticalChance, _Modifier.CriticalChanceAdd, _Modifier.CriticalChanceMultiplier),
                ApplyStat(CriticalMultiplier, _Modifier.CriticalMultiplierAdd, _Modifier.CriticalMultiplierMultiplier));
        }

        private static float Clamp01(float _Value)
        {
            return Math.Max(0f, Math.Min(1f, _Value));
        }

        private static float ApplyStat(float _BaseValue, float _AddValue, float _Multiplier)
        {
            float multiplier = _Multiplier <= 0f ? 1f : _Multiplier;
            return (_BaseValue + _AddValue) * multiplier;
        }
    }
}
