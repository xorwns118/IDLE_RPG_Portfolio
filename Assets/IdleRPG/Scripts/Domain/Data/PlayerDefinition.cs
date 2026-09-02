using System;
using System.Collections.Generic;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Data
{
    public sealed class PlayerDefinition
    {
        public PlayerDefinition(string _Id, string _DisplayName, StatBlock _Stats)
            : this(_Id, _DisplayName, _Stats, null)
        {
        }

        public PlayerDefinition(string _Id, string _DisplayName, StatBlock _Stats, IEnumerable<SkillDefinition> _SkillLoadout)
        {
            if (string.IsNullOrWhiteSpace(_Id))
                throw new ArgumentException("Player id is required.", nameof(_Id));

            Id = _Id;
            DisplayName = string.IsNullOrWhiteSpace(_DisplayName) ? _Id : _DisplayName;
            Stats = _Stats;
            SkillLoadout = CopySkills(_SkillLoadout);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public StatBlock Stats { get; }
        public IReadOnlyList<SkillDefinition> SkillLoadout { get; }

        private static SkillDefinition[] CopySkills(IEnumerable<SkillDefinition> _SkillLoadout)
        {
            if (_SkillLoadout == null)
                return Array.Empty<SkillDefinition>();

            List<SkillDefinition> skills = new List<SkillDefinition>();
            foreach (SkillDefinition skill in _SkillLoadout)
            {
                if (skill != null)
                    skills.Add(skill);
            }

            return skills.ToArray();
        }
    }
}
