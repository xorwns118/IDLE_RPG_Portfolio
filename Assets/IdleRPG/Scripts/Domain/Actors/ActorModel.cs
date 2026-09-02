using System;
using IdleRPG.Domain;
using IdleRPG.Domain.Combat;

namespace IdleRPG.Domain.Actors
{
    public sealed class ActorModel
    {
        private readonly ActorStateMachine StateMachine = new ActorStateMachine();
        private readonly StatModifierStack StatModifiers = new StatModifierStack();
        private bool DeathRaised;
        private StatBlock BaseStatsValue;
        private StatModifier ActiveStatModifierValue = StatModifier.None;

        public ActorModel(string _Id, string _DisplayName, ActorTeam _Team, StatBlock _Stats)
        {
            if (string.IsNullOrWhiteSpace(_Id))
                throw new ArgumentException("Actor id is required.", nameof(_Id));

            Id = _Id;
            DisplayName = string.IsNullOrWhiteSpace(_DisplayName) ? _Id : _DisplayName;
            Team = _Team;
            BaseStatsValue = _Stats;
            Stats = _Stats;
            CurrentHp = _Stats.MaxHp;
            StateMachine.StateChanged += HandleStateChanged;
            StateMachine.Reset();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public ActorTeam Team { get; }
        public StatBlock BaseStats => BaseStatsValue;
        public StatBlock Stats { get; private set; }
        public StatModifier ActiveStatModifier => ActiveStatModifierValue;
        public int ActiveStatModifierCount => StatModifiers.Count;
        public float CurrentHp { get; private set; }
        public ActorState State => StateMachine.CurrentState;
        public bool IsDead => CurrentHp <= 0f || State == ActorState.Dead;

        public event Action<ActorModel, float, float> HealthChanged;
        public event Action<ActorModel, ActorState, ActorState> StateChanged;
        public event Action<ActorModel> Died;

        public void SetState(ActorState _State)
        {
            if (IsDead && _State != ActorState.Dead)
                return;

            StateMachine.TrySetState(_State);
        }

        public void ApplyStatModifier(StatModifier _Modifier, bool _KeepHealthPercent = true)
        {
            StatModifiers.Clear();
            StatModifiers.Add("manual.override", _Modifier);
            RecalculateStats(_KeepHealthPercent);
        }

        public void ClearStatModifier(bool _KeepHealthPercent = true)
        {
            StatModifiers.Clear();
            RecalculateStats(_KeepHealthPercent);
        }

        public void AddStatModifier(string _SourceId, StatModifier _Modifier, float _DurationSeconds = 0f, bool _KeepHealthPercent = true)
        {
            StatModifiers.Add(_SourceId, _Modifier, _DurationSeconds);
            RecalculateStats(_KeepHealthPercent);
        }

        public bool RemoveStatModifiersFromSource(string _SourceId, bool _KeepHealthPercent = true)
        {
            bool removed = StatModifiers.RemoveBySource(_SourceId);
            if (removed)
                RecalculateStats(_KeepHealthPercent);

            return removed;
        }

        public bool TickStatModifiers(float _DeltaSeconds, bool _KeepHealthPercent = true)
        {
            bool removedExpired = StatModifiers.Tick(_DeltaSeconds);
            if (removedExpired)
                RecalculateStats(_KeepHealthPercent);

            return removedExpired;
        }

        public void RestoreFull()
        {
            DeathRaised = false;
            CurrentHp = Stats.MaxHp;
            StateMachine.Reset();
            HealthChanged?.Invoke(this, CurrentHp, Stats.MaxHp);
        }

        public DamageResult ReceiveBasicAttack(StatBlock _AttackerStats, float _CriticalRoll)
        {
            if (IsDead)
                return DamageResult.None;

            DamageResult result = CombatMath.CalculateBasicAttack(_AttackerStats, Stats, _CriticalRoll);
            CurrentHp = Math.Max(0f, CurrentHp - result.FinalDamage);
            HealthChanged?.Invoke(this, CurrentHp, Stats.MaxHp);

            if (CurrentHp <= 0f)
            {
                StateMachine.ForceState(ActorState.Dead);

                if (!DeathRaised)
                {
                    DeathRaised = true;
                    Died?.Invoke(this);
                }
            }
            else
            {
                StateMachine.TrySetState(ActorState.Hit);
            }

            return result;
        }

        private void RecalculateStats(bool _KeepHealthPercent)
        {
            float previousMaxHp = Stats.MaxHp;
            float hpPercent = previousMaxHp > 0f ? CurrentHp / previousMaxHp : 1f;
            ActiveStatModifierValue = StatModifiers.BuildTotalModifier();
            Stats = BaseStatsValue.Apply(ActiveStatModifierValue);
            CurrentHp = _KeepHealthPercent
                ? Math.Max(0f, Math.Min(Stats.MaxHp, Stats.MaxHp * hpPercent))
                : Math.Max(0f, Math.Min(CurrentHp, Stats.MaxHp));

            HealthChanged?.Invoke(this, CurrentHp, Stats.MaxHp);
        }

        private void HandleStateChanged(ActorState _PreviousState, ActorState _CurrentState)
        {
            StateChanged?.Invoke(this, _PreviousState, _CurrentState);
        }
    }
}
