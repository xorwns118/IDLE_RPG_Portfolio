using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Data;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Stages
{
    public sealed class MonsterSpawner : MonoBehaviour
    {
        private ActorFactory Factory;
        private BattleContext Context;
        private Transform SpawnPoint;
        private MvpMonsterSpawnSettings Settings = new MvpMonsterSpawnSettings();
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
        }

        public void SetSpawnPoint(Transform _SpawnPoint)
        {
            SpawnPoint = _SpawnPoint;
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

            Vector3 basePosition = SpawnPoint != null ? SpawnPoint.position : Settings.FallbackPosition;
            Vector3 position = basePosition + Settings.RepeatedSpawnOffset * SpawnCount;
            SpawnCount++;

            return Factory.CreateActor(model, position, _Color, Context);
        }
    }
}
