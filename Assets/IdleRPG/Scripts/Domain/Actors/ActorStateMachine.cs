using IdleRPG.Domain;
using System;

namespace IdleRPG.Domain.Actors
{
    public sealed class ActorStateMachine
    {
        public ActorState CurrentState { get; private set; } = ActorState.Idle;

        public event Action<ActorState, ActorState> StateChanged;

        public bool CanTransition(ActorState _NextState)
        {
            if (CurrentState == _NextState)
                return true;

            if (CurrentState == ActorState.Dead)
                return _NextState == ActorState.Dead;

            return true;
        }

        public bool TrySetState(ActorState _NextState)
        {
            if (!CanTransition(_NextState))
                return false;

            if (CurrentState == _NextState)
                return false;

            ActorState previousState = CurrentState;
            CurrentState = _NextState;
            StateChanged?.Invoke(previousState, CurrentState);
            return true;
        }

        public void ForceState(ActorState _State)
        {
            if (CurrentState == _State)
                return;

            ActorState previousState = CurrentState;
            CurrentState = _State;
            StateChanged?.Invoke(previousState, CurrentState);
        }

        public void Reset()
        {
            ForceState(ActorState.Idle);
        }
    }
}
