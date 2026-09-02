using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Combat
{
    public interface ISkillEffect
    {
        SkillEffectDefinition Definition { get; }
        SkillEffectResult Apply(ActorModel _Caster, ActorModel _Target, float _CriticalRoll);
    }
}
