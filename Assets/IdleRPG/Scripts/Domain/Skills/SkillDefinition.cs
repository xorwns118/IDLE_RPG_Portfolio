using System;
using System.Collections.Generic;

namespace IdleRPG.Domain.Skills
{
    public sealed class SkillDefinition
    {
        private readonly SkillEffectDefinition[] EffectsValue;

        public SkillDefinition(
            string _Id,
            string _DisplayName,
            SkillTargetType _TargetType,
            float _CooldownSeconds,
            float _Range,
            int _Priority,
            IEnumerable<SkillEffectDefinition> _Effects)
        {
            if (string.IsNullOrWhiteSpace(_Id))
                throw new ArgumentException("Skill id is required.", nameof(_Id));

            Id = _Id;
            DisplayName = string.IsNullOrWhiteSpace(_DisplayName) ? _Id : _DisplayName;
            TargetType = _TargetType;
            CooldownSeconds = Math.Max(0.01f, _CooldownSeconds);
            Range = Math.Max(0f, _Range);
            Priority = _Priority;
            EffectsValue = CopyEffects(_Effects);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public SkillTargetType TargetType { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public int Priority { get; }
        public IReadOnlyList<SkillEffectDefinition> Effects => EffectsValue;
        public bool HasEffects => EffectsValue.Length > 0;

        private static SkillEffectDefinition[] CopyEffects(IEnumerable<SkillEffectDefinition> _Effects)
        {
            if (_Effects == null)
                return Array.Empty<SkillEffectDefinition>();

            List<SkillEffectDefinition> effects = new List<SkillEffectDefinition>();
            foreach (SkillEffectDefinition effect in _Effects)
            {
                if (effect != null)
                    effects.Add(effect);
            }

            return effects.ToArray();
        }
    }
}
