using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Combat
{
    public sealed class DamageSkillEffect : ISkillEffect
    {
        public DamageSkillEffect(SkillEffectDefinition _Definition)
        {
            Definition = _Definition;
        }

        public SkillEffectDefinition Definition { get; }

        public SkillEffectResult Apply(ActorModel _Caster, ActorModel _Target, float _CriticalRoll)
        {
            if (_Caster == null || _Target == null || _Target.IsDead)
                return SkillEffectResult.None;

            DamageResult damage = _Target.ReceiveSkillAttack(_Caster.Stats, Definition.PowerMultiplier, _CriticalRoll);
            return SkillEffectResult.AppliedDamage(damage);
        }
    }
}
