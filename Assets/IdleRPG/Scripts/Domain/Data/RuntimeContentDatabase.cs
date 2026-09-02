using System;
using System.Collections.Generic;

namespace IdleRPG.Domain.Data
{
    public sealed class RuntimeContentDatabase
    {
        private readonly Dictionary<string, MonsterDefinition> MonstersById;
        private readonly List<StageDefinition> StageDefinitions;

        public RuntimeContentDatabase(PlayerDefinition _Player, IEnumerable<MonsterDefinition> _Monsters, IEnumerable<StageDefinition> _Stages)
        {
            Player = _Player ?? throw new ArgumentNullException(nameof(_Player));
            MonstersById = new Dictionary<string, MonsterDefinition>(StringComparer.OrdinalIgnoreCase);
            StageDefinitions = new List<StageDefinition>();

            foreach (MonsterDefinition monster in _Monsters)
            {
                if (MonstersById.ContainsKey(monster.Id))
                    throw new InvalidOperationException("Duplicate monster id: " + monster.Id);

                MonstersById.Add(monster.Id, monster);
            }

            foreach (StageDefinition stage in _Stages)
            {
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
