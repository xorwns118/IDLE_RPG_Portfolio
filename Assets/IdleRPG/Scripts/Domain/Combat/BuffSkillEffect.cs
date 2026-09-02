using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Combat
{
    public sealed class BuffSkillEffect : ISkillEffect
    {
        public BuffSkillEffect(SkillDefinition _Skill, SkillEffectDefinition _Definition)
        {
            Skill = _Skill;
            Definition = _Definition;
        }

        public SkillDefinition Skill { get; }
        public SkillEffectDefinition Definition { get; }

        public SkillEffectResult Apply(ActorModel _Caster, ActorModel _Target, float _CriticalRoll)
        {
            if (_Caster == null || _Target == null || _Target.IsDead)
                return SkillEffectResult.None;

            _Target.RemoveStatModifiersFromSource(BuildSourceId(_Caster));
            _Target.AddStatModifier(BuildSourceId(_Caster), Definition.StatModifier, Definition.DurationSeconds);
            return SkillEffectResult.AppliedBuff();
        }

        private string BuildSourceId(ActorModel _Caster)
        {
            return Skill.Id + ":" + Definition.Id + ":" + _Caster.Id;
        }
    }
}
