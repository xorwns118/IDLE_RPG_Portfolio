using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Data;
using IdleRPG.Domain.Skills;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
using UnityEngine;

namespace IdleRPG.Runtime.Stages
{
    public sealed class MonsterSpawner : MonoBehaviour
    {
        private ActorFactory Factory;
        private BattleContext Context;
        private Transform SpawnPoint;
        private MvpMonsterSpawnSettings Settings = new MvpMonsterSpawnSettings();
        private TileMapLayout TileMap;
        private int SpawnCount;

        public void Initialize(BattleContext _Context, ActorFactory _Factory)
        {
            Initialize(_Context, _Factory, Settings);
        }

        public void Initialize(BattleContext _Context, ActorFactory _Factory, MvpMonsterSpawnSettings _Settings)
        {
            Context = _Context;
            Factory = _Factory;
            Settings = _Settings ?? new MvpMonsterSpawnSettings();
            Settings.EnsureDefaults();
        }

        public void SetSpawnPoint(Transform _SpawnPoint)
        {
            SpawnPoint = _SpawnPoint;
        }

        public void SetTileMap(TileMapLayout _TileMap)
        {
            TileMap = _TileMap;
        }

        public void ResetSpawnSequence()
        {
            SpawnCount = 0;
        }

        public CombatActor Spawn(MonsterDefinition _Definition, string _DisplayName, Color _Color)
        {
            ActorModel model = new ActorModel(
                _Definition.Id,
                _DisplayName,
                ActorTeam.Monster,
                _Definition.Stats);
            model.SetSkillLoadout(new SkillLoadout(_Definition.SkillLoadout));

            Vector3 position = ResolveSpawnPosition();
            SpawnCount++;

            return Factory.CreateActor(model, position, _Color, Context);
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (TileMap != null && TileMap.IsEnabled && Settings.UseTileSpawnOffset)
            {
                Vector2Int spawnCell = ResolveSpawnCell();
                return TileMap.CellToActorWorld(spawnCell);
            }

            if (Settings.HasSpawnPositions)
            {
                int spawnIndex = Settings.SelectSpawnIndex(SpawnCount, Settings.SpawnPositions.Length);
                return Settings.SpawnPositions[spawnIndex];
            }

            Vector3 basePosition = SpawnPoint != null ? SpawnPoint.position : Settings.FallbackPosition;
            return basePosition + Settings.RepeatedSpawnOffset * SpawnCount;
        }

        private Vector2Int ResolveSpawnCell()
        {
            if (Settings.HasSpawnCells)
            {
                int spawnIndex = Settings.SelectSpawnIndex(SpawnCount, Settings.SpawnCells.Length);
                Vector2Int configuredCell = TileMap.ClampCell(Settings.SpawnCells[spawnIndex]);
                return TileMap.GetNearestWalkableCell(configuredCell);
            }

            if (TileMap.Settings.HasMultipleMonsterSpawnCells)
            {
                int spawnIndex = Settings.SelectSpawnIndex(SpawnCount, TileMap.Settings.MonsterSpawnCellCount);
                Vector2Int configuredCell = TileMap.Settings.GetMonsterSpawnCell(spawnIndex);
                return TileMap.GetNearestWalkableCell(configuredCell);
            }

            Vector3 basePosition = SpawnPoint != null ? SpawnPoint.position : Settings.FallbackPosition;
            Vector2Int baseCell = TileMap.WorldToCell(basePosition);
            Vector2Int spawnCell = TileMap.ClampCell(baseCell + Settings.RepeatedSpawnCellOffset * SpawnCount);
            return TileMap.GetNearestWalkableCell(spawnCell);
        }
    }
}
