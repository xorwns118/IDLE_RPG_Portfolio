using System;
using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Skills
{
    public sealed class SkillEffectDefinition
    {
        public SkillEffectDefinition(
            string _Id,
            SkillEffectKind _Kind,
            SkillTargetType _TargetType,
            float _PowerMultiplier,
            StatModifier _StatModifier,
            float _DurationSeconds)
        {
            Id = string.IsNullOrWhiteSpace(_Id) ? _Kind.ToString() : _Id;
            Kind = _Kind;
            TargetType = _TargetType;
            PowerMultiplier = Math.Max(0f, _PowerMultiplier);
            StatModifier = _StatModifier;
            DurationSeconds = Math.Max(0f, _DurationSeconds);
        }

        public string Id { get; }
        public SkillEffectKind Kind { get; }
        public SkillTargetType TargetType { get; }
        public float PowerMultiplier { get; }
        public StatModifier StatModifier { get; }
        public float DurationSeconds { get; }

        public static SkillEffectDefinition Damage(string _Id, SkillTargetType _TargetType, float _PowerMultiplier)
        {
            return new SkillEffectDefinition(_Id, SkillEffectKind.Damage, _TargetType, _PowerMultiplier, StatModifier.None, 0f);
        }

        public static SkillEffectDefinition Buff(string _Id, SkillTargetType _TargetType, StatModifier _Modifier, float _DurationSeconds)
        {
            return new SkillEffectDefinition(_Id, SkillEffectKind.Buff, _TargetType, 0f, _Modifier, _DurationSeconds);
        }
    }
}
