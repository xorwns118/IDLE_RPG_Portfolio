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

        public StatBlock(float _MaxHp, float _AttackPower, float _Defense,float _AttackRange,
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

        private static float Clamp01(float _Value)
        {
            return Math.Max(0f, Math.Min(1f, _Value));
        }
    }
}
