using System;

namespace IdleRPG.Domain.Actors
{
    public readonly struct StatModifier
    {
        public static StatModifier None => new StatModifier();

        public float MaxHpAdd { get; }
        public float AttackPowerAdd { get; }
        public float DefenseAdd { get; }
        public float AttackRangeAdd { get; }
        public float AttackIntervalAdd { get; }
        public float MoveSpeedAdd { get; }
        public float CriticalChanceAdd { get; }
        public float CriticalMultiplierAdd { get; }
        public float MaxHpMultiplier { get; }
        public float AttackPowerMultiplier { get; }
        public float DefenseMultiplier { get; }
        public float AttackRangeMultiplier { get; }
        public float AttackIntervalMultiplier { get; }
        public float MoveSpeedMultiplier { get; }
        public float CriticalChanceMultiplier { get; }
        public float CriticalMultiplierMultiplier { get; }

        public StatModifier(
            float _MaxHpAdd = 0f,
            float _AttackPowerAdd = 0f,
            float _DefenseAdd = 0f,
            float _AttackRangeAdd = 0f,
            float _AttackIntervalAdd = 0f,
            float _MoveSpeedAdd = 0f,
            float _CriticalChanceAdd = 0f,
            float _CriticalMultiplierAdd = 0f,
            float _MaxHpMultiplier = 1f,
            float _AttackPowerMultiplier = 1f,
            float _DefenseMultiplier = 1f,
            float _AttackRangeMultiplier = 1f,
            float _AttackIntervalMultiplier = 1f,
            float _MoveSpeedMultiplier = 1f,
            float _CriticalChanceMultiplier = 1f,
            float _CriticalMultiplierMultiplier = 1f)
        {
            MaxHpAdd = _MaxHpAdd;
            AttackPowerAdd = _AttackPowerAdd;
            DefenseAdd = _DefenseAdd;
            AttackRangeAdd = _AttackRangeAdd;
            AttackIntervalAdd = _AttackIntervalAdd;
            MoveSpeedAdd = _MoveSpeedAdd;
            CriticalChanceAdd = _CriticalChanceAdd;
            CriticalMultiplierAdd = _CriticalMultiplierAdd;
            MaxHpMultiplier = NormalizeMultiplier(_MaxHpMultiplier);
            AttackPowerMultiplier = NormalizeMultiplier(_AttackPowerMultiplier);
            DefenseMultiplier = NormalizeMultiplier(_DefenseMultiplier);
            AttackRangeMultiplier = NormalizeMultiplier(_AttackRangeMultiplier);
            AttackIntervalMultiplier = NormalizeMultiplier(_AttackIntervalMultiplier);
            MoveSpeedMultiplier = NormalizeMultiplier(_MoveSpeedMultiplier);
            CriticalChanceMultiplier = NormalizeMultiplier(_CriticalChanceMultiplier);
            CriticalMultiplierMultiplier = NormalizeMultiplier(_CriticalMultiplierMultiplier);
        }

        public static StatModifier Additive(
            float _MaxHp = 0f,
            float _AttackPower = 0f,
            float _Defense = 0f,
            float _AttackRange = 0f,
            float _AttackInterval = 0f,
            float _MoveSpeed = 0f,
            float _CriticalChance = 0f,
            float _CriticalMultiplier = 0f)
        {
            return new StatModifier(
                _MaxHp,
                _AttackPower,
                _Defense,
                _AttackRange,
                _AttackInterval,
                _MoveSpeed,
                _CriticalChance,
                _CriticalMultiplier);
        }

        public static StatModifier Multiplier(
            float _MaxHp = 1f,
            float _AttackPower = 1f,
            float _Defense = 1f,
            float _AttackRange = 1f,
            float _AttackInterval = 1f,
            float _MoveSpeed = 1f,
            float _CriticalChance = 1f,
            float _CriticalMultiplier = 1f)
        {
            return new StatModifier(
                _MaxHpMultiplier: _MaxHp,
                _AttackPowerMultiplier: _AttackPower,
                _DefenseMultiplier: _Defense,
                _AttackRangeMultiplier: _AttackRange,
                _AttackIntervalMultiplier: _AttackInterval,
                _MoveSpeedMultiplier: _MoveSpeed,
                _CriticalChanceMultiplier: _CriticalChance,
                _CriticalMultiplierMultiplier: _CriticalMultiplier);
        }

        private static float NormalizeMultiplier(float _Value)
        {
            if (float.IsNaN(_Value) || float.IsInfinity(_Value))
                return 1f;

            return Math.Max(0f, _Value);
        }
    }
}
