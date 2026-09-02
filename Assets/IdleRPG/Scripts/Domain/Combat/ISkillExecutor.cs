using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Combat
{
    public interface ISkillExecutor
    {
        bool CanExecute(ActorModel _Caster, ActorModel _Target);
        void Execute(ActorModel _Caster, ActorModel _Target, float _CriticalRoll);
    }
}
