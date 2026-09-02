using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Combat
{
    public interface ISkillExecutor
    {
        bool CanExecute(SkillRuntime _Skill, ActorModel _Caster, ActorModel _Target, float _Distance);
        SkillExecutionResult Execute(SkillRuntime _Skill, ActorModel _Caster, ActorModel _Target, float _Distance, float _CriticalRoll);
    }
}
