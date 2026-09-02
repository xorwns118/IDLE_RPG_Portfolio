using System;
using System.Collections.Generic;
using IdleRPG.Domain.Skills;

namespace IdleRPG.Domain.Data
{
    public sealed class RuntimeContentDatabase
    {
        private readonly Dictionary<string, SkillDefinition> SkillsById;
        private readonly Dictionary<string, MonsterDefinition> MonstersById;
        private readonly List<StageDefinition> StageDefinitions;
        private readonly List<SkillDefinition> SkillDefinitions;

        public RuntimeContentDatabase(PlayerDefinition _Player, IEnumerable<MonsterDefinition> _Monsters, IEnumerable<StageDefinition> _Stages)
            : this(_Player, _Monsters, _Stages, Array.Empty<SkillDefinition>())
        {
        }

        public RuntimeContentDatabase(
            PlayerDefinition _Player,
            IEnumerable<MonsterDefinition> _Monsters,
            IEnumerable<StageDefinition> _Stages,
            IEnumerable<SkillDefinition> _Skills)
        {
            Player = _Player ?? throw new ArgumentNullException(nameof(_Player));
            SkillsById = new Dictionary<string, SkillDefinition>(StringComparer.OrdinalIgnoreCase);
            MonstersById = new Dictionary<string, MonsterDefinition>(StringComparer.OrdinalIgnoreCase);
            StageDefinitions = new List<StageDefinition>();
            SkillDefinitions = new List<SkillDefinition>();

            foreach (SkillDefinition skill in _Skills ?? Array.Empty<SkillDefinition>())
            {
                if (skill == null)
                    continue;

                if (SkillsById.ContainsKey(skill.Id))
                    throw new InvalidOperationException("Duplicate skill id: " + skill.Id);

                SkillsById.Add(skill.Id, skill);
                SkillDefinitions.Add(skill);
            }

            foreach (MonsterDefinition monster in _Monsters ?? Array.Empty<MonsterDefinition>())
            {
                if (monster == null)
                    continue;

                if (MonstersById.ContainsKey(monster.Id))
                    throw new InvalidOperationException("Duplicate monster id: " + monster.Id);

                MonstersById.Add(monster.Id, monster);
            }

            foreach (StageDefinition stage in _Stages ?? Array.Empty<StageDefinition>())
            {
                if (stage == null)
                    continue;

                if (!MonstersById.ContainsKey(stage.MonsterId))
                    throw new InvalidOperationException("Stage references missing monster id: " + stage.MonsterId);

                StageDefinitions.Add(stage);
            }

            StageDefinitions.Sort((_Left, _Right) => _Left.StageNumber.CompareTo(_Right.StageNumber));

            if (StageDefinitions.Count == 0)
                throw new InvalidOperationException("At least one stage is required.");
        }

        public PlayerDefinition Player { get; }
        public IReadOnlyList<StageDefinition> Stages => StageDefinitions;
        public IReadOnlyList<SkillDefinition> Skills => SkillDefinitions;

        public SkillDefinition GetSkill(string _Id)
        {
            if (!SkillsById.TryGetValue(_Id, out SkillDefinition skill))
                throw new KeyNotFoundException("Skill not found: " + _Id);

            return skill;
        }

        public MonsterDefinition GetMonster(string _Id)
        {
            if (!MonstersById.TryGetValue(_Id, out MonsterDefinition monster))
                throw new KeyNotFoundException("Monster not found: " + _Id);

            return monster;
        }

        public StageDefinition GetStage(int _StageNumber)
        {
            foreach (StageDefinition stage in StageDefinitions)
            {
                if (stage.StageNumber == _StageNumber)
                    return stage;
            }

            return StageDefinitions[StageDefinitions.Count - 1];
        }
    }
}
