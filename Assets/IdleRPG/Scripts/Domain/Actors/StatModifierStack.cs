using System;
using System.Collections.Generic;

namespace IdleRPG.Domain.Actors
{
    public sealed class StatModifierStack
    {
        private readonly List<StatModifierEntry> Entries = new List<StatModifierEntry>();

        public IReadOnlyList<StatModifierEntry> ActiveEntries => Entries;
        public int Count => Entries.Count;

        public void Add(string _SourceId, StatModifier _Modifier, float _DurationSeconds = 0f)
        {
            Entries.Add(new StatModifierEntry(_SourceId, _Modifier, Math.Max(0f, _DurationSeconds)));
        }

        public bool RemoveBySource(string _SourceId)
        {
            if (string.IsNullOrWhiteSpace(_SourceId))
                return false;

            int removedCount = Entries.RemoveAll(_Entry => _Entry.SourceId == _SourceId);
            return removedCount > 0;
        }

        public bool Tick(float _DeltaSeconds)
        {
            bool removedExpired = false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                StatModifierEntry entry = Entries[i];
                entry.Tick(Math.Max(0f, _DeltaSeconds));
                if (!entry.IsExpired)
                    continue;

                Entries.RemoveAt(i);
                removedExpired = true;
            }

            return removedExpired;
        }

        public void Clear()
        {
            Entries.Clear();
        }

        public StatModifier BuildTotalModifier()
        {
            float maxHpAdd = 0f;
            float attackPowerAdd = 0f;
            float defenseAdd = 0f;
            float attackRangeAdd = 0f;
            float attackIntervalAdd = 0f;
            float moveSpeedAdd = 0f;
            float criticalChanceAdd = 0f;
            float criticalMultiplierAdd = 0f;
            float maxHpMultiplier = 1f;
            float attackPowerMultiplier = 1f;
            float defenseMultiplier = 1f;
            float attackRangeMultiplier = 1f;
            float attackIntervalMultiplier = 1f;
            float moveSpeedMultiplier = 1f;
            float criticalChanceMultiplier = 1f;
            float criticalMultiplierMultiplier = 1f;

            foreach (StatModifierEntry entry in Entries)
            {
                StatModifier modifier = entry.Modifier;
                maxHpAdd += modifier.MaxHpAdd;
                attackPowerAdd += modifier.AttackPowerAdd;
                defenseAdd += modifier.DefenseAdd;
                attackRangeAdd += modifier.AttackRangeAdd;
                attackIntervalAdd += modifier.AttackIntervalAdd;
                moveSpeedAdd += modifier.MoveSpeedAdd;
                criticalChanceAdd += modifier.CriticalChanceAdd;
                criticalMultiplierAdd += modifier.CriticalMultiplierAdd;
                maxHpMultiplier *= ReadMultiplier(modifier.MaxHpMultiplier);
                attackPowerMultiplier *= ReadMultiplier(modifier.AttackPowerMultiplier);
                defenseMultiplier *= ReadMultiplier(modifier.DefenseMultiplier);
                attackRangeMultiplier *= ReadMultiplier(modifier.AttackRangeMultiplier);
                attackIntervalMultiplier *= ReadMultiplier(modifier.AttackIntervalMultiplier);
                moveSpeedMultiplier *= ReadMultiplier(modifier.MoveSpeedMultiplier);
                criticalChanceMultiplier *= ReadMultiplier(modifier.CriticalChanceMultiplier);
                criticalMultiplierMultiplier *= ReadMultiplier(modifier.CriticalMultiplierMultiplier);
            }

            return new StatModifier(
                maxHpAdd,
                attackPowerAdd,
                defenseAdd,
                attackRangeAdd,
                attackIntervalAdd,
                moveSpeedAdd,
                criticalChanceAdd,
                criticalMultiplierAdd,
                maxHpMultiplier,
                attackPowerMultiplier,
                defenseMultiplier,
                attackRangeMultiplier,
                attackIntervalMultiplier,
                moveSpeedMultiplier,
                criticalChanceMultiplier,
                criticalMultiplierMultiplier);
        }

        private static float ReadMultiplier(float _Value)
        {
            return _Value <= 0f ? 1f : _Value;
        }
    }
}
