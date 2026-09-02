using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Combat
{
    public readonly struct SkillEffectResult
    {
        private SkillEffectResult(bool _Applied, SkillEffectKind _Kind, DamageResult _Damage)
        {
            Applied = _Applied;
            Kind = _Kind;
            Damage = _Damage;
        }

        public bool Applied { get; }
        public SkillEffectKind Kind { get; }
        public DamageResult Damage { get; }

        public static SkillEffectResult None => new SkillEffectResult(false, SkillEffectKind.Damage, DamageResult.None);

        public static SkillEffectResult AppliedBuff()
        {
            return new SkillEffectResult(true, SkillEffectKind.Buff, DamageResult.None);
        }

        public static SkillEffectResult AppliedDamage(DamageResult _Damage)
        {
            return new SkillEffectResult(true, SkillEffectKind.Damage, _Damage);
        }
    }

    public readonly struct SkillExecutionResult
    {
        private SkillExecutionResult(bool _Succeeded, string _SkillId, string _SkillDisplayName, int _AppliedEffectCount, DamageResult _LastDamage)
        {
            Succeeded = _Succeeded;
            SkillId = _SkillId ?? string.Empty;
            SkillDisplayName = _SkillDisplayName ?? string.Empty;
            AppliedEffectCount = _AppliedEffectCount;
            LastDamage = _LastDamage;
        }

        public bool Succeeded { get; }
        public string SkillId { get; }
        public string SkillDisplayName { get; }
        public int AppliedEffectCount { get; }
        public DamageResult LastDamage { get; }

        public static SkillExecutionResult Failed(string _SkillId)
        {
            return new SkillExecutionResult(false, _SkillId, string.Empty, 0, DamageResult.None);
        }

        public static SkillExecutionResult Success(SkillDefinition _Skill, int _AppliedEffectCount, DamageResult _LastDamage)
        {
            return new SkillExecutionResult(true, _Skill.Id, _Skill.DisplayName, _AppliedEffectCount, _LastDamage);
        }
    }
}
