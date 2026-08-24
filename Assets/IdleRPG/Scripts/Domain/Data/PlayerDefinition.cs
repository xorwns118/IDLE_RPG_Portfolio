using System;
using IdleRPG.Domain.Actors;

namespace IdleRPG.Domain.Data
{
    public sealed class PlayerDefinition
    {
        public PlayerDefinition(string _Id, string _DisplayName, StatBlock _Stats)
        {
            if (string.IsNullOrWhiteSpace(_Id))
                throw new ArgumentException("Player id is required.", nameof(_Id));

            Id = _Id;
            DisplayName = string.IsNullOrWhiteSpace(_DisplayName) ? _Id : _DisplayName;
            Stats = _Stats;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public StatBlock Stats { get; }
    }
}
