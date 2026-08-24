using System;

namespace IdleRPG.Domain.Data
{
    public sealed class StageDefinition
    {
        public StageDefinition(int _StageNumber, string _MonsterId, int _RequiredKills)
        {
            if (_StageNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(_StageNumber), "Stage number must be positive.");

            if (string.IsNullOrWhiteSpace(_MonsterId))
                throw new ArgumentException("Monster id is required.", nameof(_MonsterId));

            StageNumber = _StageNumber;
            MonsterId = _MonsterId;
            RequiredKills = Math.Max(1, _RequiredKills);
        }

        public int StageNumber { get; }
        public string MonsterId { get; }
        public int RequiredKills { get; }
    }
}
