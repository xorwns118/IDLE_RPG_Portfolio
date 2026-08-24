using System;
using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Data
{
    public sealed class MonsterDefinition
    {
        public MonsterDefinition(string _Id, string _DisplayName, StatBlock _Stats, int _GoldReward, int _ExpReward)
        {
            if (string.IsNullOrWhiteSpace(_Id))
                throw new ArgumentException("Monster id is required.", nameof(_Id));

            Id = _Id;
            DisplayName = string.IsNullOrWhiteSpace(_DisplayName) ? _Id : _DisplayName;
            Stats = _Stats;
            GoldReward = Math.Max(0, _GoldReward);
            ExpReward = Math.Max(0, _ExpReward);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public StatBlock Stats { get; }
        public int GoldReward { get; }
        public int ExpReward { get; }

        public MonsterDefinition WithStats(StatBlock _Stats, int _GoldReward, int _ExpReward)
        {
            return new MonsterDefinition(Id, DisplayName, _Stats, _GoldReward, _ExpReward);
        }
    }
}
