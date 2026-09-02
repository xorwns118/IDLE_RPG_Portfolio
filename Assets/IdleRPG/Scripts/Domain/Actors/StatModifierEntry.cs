using System;

namespace IdleRPG.Domain.Actors
{
    public sealed class StatModifierEntry
    {
        public StatModifierEntry(string _SourceId, StatModifier _Modifier, float _DurationSeconds)
        {
            SourceId = string.IsNullOrWhiteSpace(_SourceId) ? "unknown" : _SourceId;
            Modifier = _Modifier;
            DurationSeconds = _DurationSeconds;
            RemainingSeconds = _DurationSeconds;
        }

        public string SourceId { get; }
        public StatModifier Modifier { get; }
        public float DurationSeconds { get; }
        public float RemainingSeconds { get; private set; }
        public bool IsTimed => DurationSeconds > 0f;
        public bool IsExpired => IsTimed && RemainingSeconds <= 0f;

        public void Tick(float _DeltaSeconds)
        {
            if (!IsTimed)
                return;

            RemainingSeconds -= _DeltaSeconds;
        }
    }
}
