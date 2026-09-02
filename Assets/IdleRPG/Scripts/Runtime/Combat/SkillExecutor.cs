using System.Collections.Generic;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Combat;
using IdleRPG.Domain.Skills;
using IdleRPG.Runtime.Actors;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    public sealed class SkillExecutor : ISkillExecutor
    {
        public bool TryExecuteBestSkill(
            CombatActor _Caster,
            CombatActor _Target,
            float _Distance,
            float _CriticalRoll,
            out SkillExecutionResult _Result)
        {
            return TryExecuteBestSkill(_Caster, _Target, _Distance, _CriticalRoll, null, 0f, out _Result);
        }

        public bool TryExecuteBestSkill(
            CombatActor _Caster,
            CombatActor _Target,
            float _Distance,
            float _CriticalRoll,
            SkillReadinessGate _ReadinessGate,
            float _ReadyDelaySeconds,
            out SkillExecutionResult _Result)
        {
            _Result = SkillExecutionResult.Failed(string.Empty);
            if (_Caster == null || _Caster.Model == null)
                return false;

            SkillLoadout loadout = _Caster.Model.SkillLoadout;
            if (loadout == null || !loadout.HasAnySkill)
                return false;

            SkillRuntime skill = SelectBestReadySkill(loadout, _Caster, _Target, _Distance, _ReadinessGate, _ReadyDelaySeconds);
            if (skill == null)
                return false;

            _Result = Execute(skill, _Caster, _Target, _Distance, _CriticalRoll);
            if (_Result.Succeeded && _ReadinessGate != null)
                _ReadinessGate.MarkSkillUsed(skill);

            return _Result.Succeeded;
        }

        public bool CanExecute(SkillRuntime _Skill, ActorModel _Caster, ActorModel _Target, float _Distance)
        {
            if (_Skill == null || _Skill.Definition == null || !_Skill.IsReady)
                return false;

            if (!_Skill.Definition.HasEffects || _Caster == null || _Caster.IsDead)
                return false;

            if (!_Caster.IsInCombat)
                return false;

            if (_Skill.Definition.TargetType == SkillTargetType.Self)
                return true;

            if (!IsEnemyTargetInCombat(_Caster, _Target))
                return false;

            return _Distance <= _Skill.Definition.Range;
        }

        public SkillExecutionResult Execute(
            SkillRuntime _Skill,
            ActorModel _Caster,
            ActorModel _Target,
            float _Distance,
            float _CriticalRoll)
        {
            if (!CanExecute(_Skill, _Caster, _Target, _Distance))
                return SkillExecutionResult.Failed(_Skill != null && _Skill.Definition != null ? _Skill.Definition.Id : string.Empty);

            _Caster.SetState(ActorState.Skill);
            int appliedEffectCount = 0;
            DamageResult lastDamage = DamageResult.None;

            for (int i = 0; i < _Skill.Definition.Effects.Count; i++)
            {
                SkillEffectDefinition effect = _Skill.Definition.Effects[i];
                ActorModel effectTarget = ResolveEffectTarget(effect.TargetType, _Caster, _Target);
                SkillEffectResult effectResult = ApplyEffect(_Skill.Definition, effect, _Caster, effectTarget, _CriticalRoll);
                if (!effectResult.Applied)
                    continue;

                appliedEffectCount++;
                if (effectResult.Kind == SkillEffectKind.Damage)
                    lastDamage = effectResult.Damage;
            }

            if (appliedEffectCount <= 0)
                return SkillExecutionResult.Failed(_Skill.Definition.Id);

            _Skill.StartCooldown();
            return SkillExecutionResult.Success(_Skill.Definition, appliedEffectCount, lastDamage);
        }

        public SkillExecutionResult Execute(
            SkillRuntime _Skill,
            CombatActor _Caster,
            CombatActor _Target,
            float _Distance,
            float _CriticalRoll)
        {
            if (_Caster == null || _Caster.Model == null)
                return SkillExecutionResult.Failed(_Skill != null && _Skill.Definition != null ? _Skill.Definition.Id : string.Empty);

            ActorModel targetModel = _Target != null ? _Target.Model : null;
            if (!CanExecute(_Skill, _Caster.Model, targetModel, _Distance))
                return SkillExecutionResult.Failed(_Skill != null && _Skill.Definition != null ? _Skill.Definition.Id : string.Empty);

            _Caster.Model.SetState(ActorState.Skill);
            if (_Target != null)
                _Caster.Face(_Target.transform.position);

            _Caster.PlayIdleAnimation();

            int appliedEffectCount = 0;
            DamageResult lastDamage = DamageResult.None;
            for (int i = 0; i < _Skill.Definition.Effects.Count; i++)
            {
                SkillEffectDefinition effect = _Skill.Definition.Effects[i];
                CombatActor effectTarget = ResolveEffectTarget(effect.TargetType, _Caster, _Target);
                SkillEffectResult effectResult = ApplyEffect(_Skill.Definition, effect, _Caster, effectTarget, _CriticalRoll);
                if (!effectResult.Applied)
                    continue;

                appliedEffectCount++;
                if (effectResult.Kind == SkillEffectKind.Damage)
                    lastDamage = effectResult.Damage;
            }

            if (appliedEffectCount <= 0)
                return SkillExecutionResult.Failed(_Skill.Definition.Id);

            _Skill.StartCooldown();
            SkillExecutionResult result = SkillExecutionResult.Success(_Skill.Definition, appliedEffectCount, lastDamage);
            _Caster.NotifySkillUsed(_Target, result);
            return result;
        }

        private static bool IsEnemyTargetInCombat(ActorModel _Caster, ActorModel _Target)
        {
            return _Target != null && !_Target.IsDead && _Target.IsInCombat && _Target.Team != _Caster.Team;
        }

        private SkillRuntime SelectBestReadySkill(
            SkillLoadout _Loadout,
            CombatActor _Caster,
            CombatActor _Target,
            float _Distance,
            SkillReadinessGate _ReadinessGate,
            float _ReadyDelaySeconds)
        {
            SkillRuntime bestSkill = null;
            int bestPriority = int.MinValue;
            ActorModel targetModel = _Target != null ? _Target.Model : null;

            for (int i = 0; i < SkillLoadout.MaxSlots; i++)
            {
                SkillRuntime skill = _Loadout.GetSlot(i);
                if (skill == null || !CanExecute(skill, _Caster.Model, targetModel, _Distance))
                    continue;

                if (_ReadinessGate != null && !_ReadinessGate.CanUseSkill(skill, _ReadyDelaySeconds))
                    continue;

                if (bestSkill == null || skill.Definition.Priority > bestPriority)
                {
                    bestSkill = skill;
                    bestPriority = skill.Definition.Priority;
                }
            }

            return bestSkill;
        }

        private static ActorModel ResolveEffectTarget(SkillTargetType _TargetType, ActorModel _Caster, ActorModel _Target)
        {
            return _TargetType == SkillTargetType.Self ? _Caster : _Target;
        }

        private static CombatActor ResolveEffectTarget(SkillTargetType _TargetType, CombatActor _Caster, CombatActor _Target)
        {
            return _TargetType == SkillTargetType.Self ? _Caster : _Target;
        }

        private static SkillEffectResult ApplyEffect(
            SkillDefinition _Skill,
            SkillEffectDefinition _Effect,
            ActorModel _Caster,
            ActorModel _Target,
            float _CriticalRoll)
        {
            if (_Effect.Kind == SkillEffectKind.Buff)
                return new BuffSkillEffect(_Skill, _Effect).Apply(_Caster, _Target, _CriticalRoll);

            return new DamageSkillEffect(_Effect).Apply(_Caster, _Target, _CriticalRoll);
        }

        private static SkillEffectResult ApplyEffect(
            SkillDefinition _Skill,
            SkillEffectDefinition _Effect,
            CombatActor _Caster,
            CombatActor _Target,
            float _CriticalRoll)
        {
            if (_Caster == null || _Target == null || _Target.Model == null || _Target.Model.IsDead)
                return SkillEffectResult.None;

            if (_Effect.Kind == SkillEffectKind.Buff)
            {
                BuffSkillEffect buff = new BuffSkillEffect(_Skill, _Effect);
                return buff.Apply(_Caster.Model, _Target.Model, _CriticalRoll);
            }

            DamageResult damage = _Target.TakeSkillAttack(_Caster, _Effect.PowerMultiplier, _CriticalRoll);
            return SkillEffectResult.AppliedDamage(damage);
        }
    }

    public sealed class SkillReadinessGate
    {
        private readonly Dictionary<SkillRuntime, float> ReadyDelayTimers = new Dictionary<SkillRuntime, float>();
        private readonly List<SkillRuntime> ReadyDelaySkills = new List<SkillRuntime>();

        public void Clear()
        {
            ReadyDelayTimers.Clear();
        }

        public void Clear(SkillLoadout _Loadout)
        {
            if (_Loadout == null)
                return;

            for (int i = 0; i < SkillLoadout.MaxSlots; i++)
            {
                SkillRuntime skill = _Loadout.GetSlot(i);
                if (skill != null)
                    ReadyDelayTimers.Remove(skill);
            }
        }

        public bool CanUseSkill(SkillRuntime _Skill, float _ReadyDelaySeconds)
        {
            if (_Skill == null)
                return false;

            if (!_Skill.IsReady)
            {
                ReadyDelayTimers.Remove(_Skill);
                return false;
            }

            float readyDelaySeconds = Mathf.Max(0f, _ReadyDelaySeconds);
            if (readyDelaySeconds <= 0f)
                return true;

            if (!ReadyDelayTimers.TryGetValue(_Skill, out float remainingSeconds))
            {
                ReadyDelayTimers[_Skill] = readyDelaySeconds;
                return false;
            }

            return remainingSeconds <= 0f;
        }

        public void MarkSkillUsed(SkillRuntime _Skill)
        {
            if (_Skill != null)
                ReadyDelayTimers.Remove(_Skill);
        }

        public void Tick(float _DeltaSeconds)
        {
            if (ReadyDelayTimers.Count == 0)
                return;

            float deltaSeconds = Mathf.Max(0f, _DeltaSeconds);
            ReadyDelaySkills.Clear();
            foreach (SkillRuntime skill in ReadyDelayTimers.Keys)
            {
                ReadyDelaySkills.Add(skill);
            }

            for (int i = 0; i < ReadyDelaySkills.Count; i++)
            {
                SkillRuntime skill = ReadyDelaySkills[i];
                if (skill == null)
                    continue;

                if (!skill.IsReady)
                {
                    ReadyDelayTimers.Remove(skill);
                    continue;
                }

                float remainingSeconds = ReadyDelayTimers[skill] - deltaSeconds;
                if (remainingSeconds <= 0f)
                    ReadyDelayTimers[skill] = 0f;
                else
                    ReadyDelayTimers[skill] = remainingSeconds;
            }
        }
    }
}
