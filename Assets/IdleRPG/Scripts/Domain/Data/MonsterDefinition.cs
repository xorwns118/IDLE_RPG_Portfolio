using System;
using System.Collections.Generic;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Data
{
    public sealed class MonsterDefinition
    {
        public MonsterDefinition(string _Id, string _DisplayName, StatBlock _Stats, int _GoldReward, int _ExpReward)
            : this(_Id, _DisplayName, _Stats, _GoldReward, _ExpReward, null)
        {
        }

        public MonsterDefinition(
            string _Id,
            string _DisplayName,
            StatBlock _Stats,
            int _GoldReward,
            int _ExpReward,
            IEnumerable<SkillDefinition> _SkillLoadout)
        {
            if (string.IsNullOrWhiteSpace(_Id))
                throw new ArgumentException("Monster id is required.", nameof(_Id));

            Id = _Id;
            DisplayName = string.IsNullOrWhiteSpace(_DisplayName) ? _Id : _DisplayName;
            Stats = _Stats;
            GoldReward = Math.Max(0, _GoldReward);
            ExpReward = Math.Max(0, _ExpReward);
            SkillLoadout = CopySkills(_SkillLoadout);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public StatBlock Stats { get; }
        public int GoldReward { get; }
        public int ExpReward { get; }
        public IReadOnlyList<SkillDefinition> SkillLoadout { get; }

        public MonsterDefinition WithStats(StatBlock _Stats, int _GoldReward, int _ExpReward)
        {
            return new MonsterDefinition(Id, DisplayName, _Stats, _GoldReward, _ExpReward, SkillLoadout);
        }

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
