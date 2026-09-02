using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Combat
{
    public interface ISkillEffect
    {
        void Apply(ActorModel _Caster, ActorModel _Target, float _CriticalRoll);
    }
}
