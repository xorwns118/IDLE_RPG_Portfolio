using System;
using IdleRPG.Domain;
using IdleRPG.Domain.Combat;

namespace IdleRPG.Domain.Actors
{
    public sealed class ActorModel
    {
        private bool DeathRaised;

        public ActorModel(string _Id, string _DisplayName, ActorTeam _Team, StatBlock _Stats)
        {
            if (string.IsNullOrWhiteSpace(_Id))
                throw new ArgumentException("Actor id is required.", nameof(_Id));

            Id = _Id;
            DisplayName = string.IsNullOrWhiteSpace(_DisplayName) ? _Id : _DisplayName;
            Team = _Team;
            Stats = _Stats;
            CurrentHp = _Stats.MaxHp;
            State = ActorState.Idle;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public ActorTeam Team { get; }
        public StatBlock Stats { get; private set; }
        public float CurrentHp { get; private set; }
        public ActorState State { get; private set; }
        public bool IsDead => CurrentHp <= 0f || State == ActorState.Dead;

        public event Action<ActorModel, float, float> HealthChanged;
        public event Action<ActorModel> Died;

        public void SetState(ActorState _State)
        {
            if (IsDead && _State != ActorState.Dead)
                return;

            State = _State;
        }

        public void RestoreFull()
        {
            DeathRaised = false;
            CurrentHp = Stats.MaxHp;
            State = ActorState.Idle;
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
                State = ActorState.Dead;

                if (!DeathRaised)
                {
                    DeathRaised = true;
                    Died?.Invoke(this);
                }
            }
            else
            {
                State = ActorState.Hit;
            }

            return result;
        }
    }
}
