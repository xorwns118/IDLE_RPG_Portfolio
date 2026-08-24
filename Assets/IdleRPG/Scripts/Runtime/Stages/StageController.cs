using System;
using System.Collections;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Combat;
using IdleRPG.Domain.Data;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Stages
{
    public sealed class StageController : MonoBehaviour
    {
        private static readonly Vector3 DefaultPlayerStartPosition = new Vector3(-3.2f, 0f, 0f);

        public sealed class RuntimeSetup
        {
            public RuntimeContentDatabase Database;
            public BattleContext Context;
            public ActorFactory Factory;
            public Transform MonsterSpawnPoint;
            public MvpStageRuntimeSettings RuntimeSettings = new MvpStageRuntimeSettings();
            public MvpGameContentSettings ContentSettings = MvpGameContentSettings.CreateDefault();
            public MvpActorViewSettings ActorSettings = MvpActorViewSettings.CreateDefault();
            public Vector3 PlayerStartPosition = DefaultPlayerStartPosition;
            public MvpMonsterSpawnSettings SpawnSettings = new MvpMonsterSpawnSettings();
        }

        private RuntimeContentDatabase Database;
        private BattleContext Context;
        private ActorFactory Factory;
        private MonsterSpawner Spawner;
        private MvpStageRuntimeSettings RuntimeSettings = new MvpStageRuntimeSettings();
        private MvpGameContentSettings ContentSettings = MvpGameContentSettings.CreateDefault();
        private MvpActorViewSettings ActorSettings = MvpActorViewSettings.CreateDefault();
        private MvpMonsterSpawnSettings SpawnSettings = new MvpMonsterSpawnSettings();
        private CombatActor PlayerActor;
        private CombatActor ActiveMonsterActor;
        private MonsterDefinition ActiveMonsterDefinition;
        private StageDefinition CurrentStage;
        private Transform MonsterSpawnPoint;
        private Vector3 PlayerStartPosition = DefaultPlayerStartPosition;
        private int CurrentStageNumberValue;
        private int KillsInStageValue;

        public int CurrentStageNumber => CurrentStageNumberValue;
        public int KillsInStage => KillsInStageValue;
        public int RequiredKills => CurrentStage != null ? CurrentStage.RequiredKills : 0;
        public int TotalGold { get; private set; }
        public int TotalExp { get; private set; }
        public string LastLog { get; private set; } = "Ready";
        public CombatActor Player => PlayerActor;
        public CombatActor ActiveMonster => ActiveMonsterActor;
        public bool IsPlayerDefeated { get; private set; }

        public void Initialize(RuntimeSetup _Setup)
        {
            if (_Setup == null)
                throw new ArgumentNullException(nameof(_Setup));

            Database = Require(_Setup.Database, nameof(_Setup.Database));
            Context = Require(_Setup.Context, nameof(_Setup.Context));
            Factory = Require(_Setup.Factory, nameof(_Setup.Factory));
            MonsterSpawnPoint = _Setup.MonsterSpawnPoint;
            RuntimeSettings = _Setup.RuntimeSettings ?? new MvpStageRuntimeSettings();
            ContentSettings = _Setup.ContentSettings ?? MvpGameContentSettings.CreateDefault();
            ActorSettings = _Setup.ActorSettings ?? MvpActorViewSettings.CreateDefault();
            SpawnSettings = _Setup.SpawnSettings ?? new MvpMonsterSpawnSettings();
            PlayerStartPosition = _Setup.PlayerStartPosition;

            ContentSettings.EnsureDefaults();
            ActorSettings.EnsureDefaults();

            Spawner = gameObject.GetComponent<MonsterSpawner>();
            if (Spawner == null)
                Spawner = gameObject.AddComponent<MonsterSpawner>();

            Spawner.Initialize(Context, Factory, SpawnSettings);
            Spawner.SetSpawnPoint(MonsterSpawnPoint);

            BeginStage(Mathf.Max(1, RuntimeSettings.StartStageNumber));
        }

        public void RestartCurrentStage()
        {
            if (Database == null || Context == null || Factory == null)
                return;

            StopAllCoroutines();
            int restartStage = CurrentStageNumberValue > 0 ? CurrentStageNumberValue : Mathf.Max(1, RuntimeSettings.StartStageNumber);
            BeginStage(restartStage);
            LastLog = RuntimeSettings.FormatStageRestarted(CurrentStageNumberValue);
        }

        private void PreparePlayerForStage()
        {
            if (PlayerActor != null)
            {
                PlayerActor.Died -= HandlePlayerDied;
                PlayerActor.DamageTaken -= HandleDamageTaken;
            }

            ActorModel model = CreatePlayerModel();
            GameObject actorObject = PlayerActor != null ? PlayerActor.gameObject : null;

            if (actorObject != null)
                actorObject.SetActive(true);

            PlayerActor = actorObject != null
                ? Factory.ConfigureActor(actorObject, model, ActorSettings.PlayerColor, Context)
                : Factory.CreateActor(model, PlayerStartPosition, ActorSettings.PlayerColor, Context);

            PlayerActor.transform.position = PlayerStartPosition;
            PlayerActor.transform.rotation = Quaternion.identity;
            PlayerActor.SetTarget(null);
            PlayerActor.Died += HandlePlayerDied;
            PlayerActor.DamageTaken += HandleDamageTaken;
        }

        private ActorModel CreatePlayerModel()
        {
            PlayerDefinition definition = Database.Player;
            return new ActorModel(definition.Id, definition.DisplayName, ActorTeam.Player, definition.Stats);
        }

        private void BeginStage(int _StageNumber)
        {
            CurrentStageNumberValue = Mathf.Max(1, _StageNumber);
            CurrentStage = Database.GetStage(CurrentStageNumberValue);
            KillsInStageValue = 0;
            IsPlayerDefeated = false;
            ClearSpawnedMonsters();
            PreparePlayerForStage();
            Spawner.ResetSpawnSequence();
            LastLog = RuntimeSettings.FormatStageStarted(CurrentStageNumberValue);
            SpawnNextMonster();
        }

        private void SpawnNextMonster()
        {
            if (PlayerActor == null || !PlayerActor.IsAlive)
                return;

            MonsterDefinition baseDefinition = Database.GetMonster(CurrentStage.MonsterId);
            ActiveMonsterDefinition = CreateScaledMonsterDefinition(baseDefinition, CurrentStageNumberValue);
            string displayName = RuntimeSettings.FormatMonsterName(baseDefinition.DisplayName, CurrentStageNumberValue);
            Color monsterColor = ContentSettings.ResolveMonsterColor(baseDefinition.Id, ActorSettings.MonsterFallbackColor);
            ActiveMonsterActor = Spawner.Spawn(ActiveMonsterDefinition, displayName, monsterColor);

            ActiveMonsterActor.Died += HandleMonsterDied;
            ActiveMonsterActor.DamageTaken += HandleDamageTaken;
        }

        private MonsterDefinition CreateScaledMonsterDefinition(MonsterDefinition _Definition, int _StageNumber)
        {
            StatBlock scaledStats = _Definition.Stats.Scale(
                RuntimeSettings.GetHpMultiplier(_StageNumber),
                RuntimeSettings.GetAttackMultiplier(_StageNumber),
                RuntimeSettings.GetDefenseMultiplier(_StageNumber));
            float rewardMultiplier = RuntimeSettings.GetRewardMultiplier(_StageNumber);

            return _Definition.WithStats(
                scaledStats,
                Mathf.RoundToInt(_Definition.GoldReward * rewardMultiplier),
                Mathf.RoundToInt(_Definition.ExpReward * rewardMultiplier));
        }

        private void HandleMonsterDied(CombatActor _Monster)
        {
            if (_Monster != ActiveMonsterActor)
                return;

            _Monster.Died -= HandleMonsterDied;
            _Monster.DamageTaken -= HandleDamageTaken;

            int goldReward = ActiveMonsterDefinition.GoldReward;
            int expReward = ActiveMonsterDefinition.ExpReward;
            TotalGold += goldReward;
            TotalExp += expReward;
            KillsInStageValue++;

            LastLog = RuntimeSettings.FormatMonsterDefeated(_Monster.Model.DisplayName, goldReward, expReward);

            if (KillsInStageValue >= CurrentStage.RequiredKills)
                StartCoroutine(AdvanceStageAfterDelay());
            else
                StartCoroutine(SpawnAfterDelay(RuntimeSettings.SpawnDelayAfterKill));
        }

        private void HandlePlayerDied(CombatActor _Player)
        {
            if (IsPlayerDefeated)
                return;

            IsPlayerDefeated = true;
            StopAllCoroutines();
            if (ActiveMonsterActor != null)
                ActiveMonsterActor.SetTarget(null);

            LastLog = RuntimeSettings.PlayerDefeatedLog;
        }

        private void HandleDamageTaken(CombatActor _Target, CombatActor _Attacker, DamageResult _Result)
        {
            if (_Result.FinalDamage <= 0f || _Attacker == null || _Target == null || _Attacker.Model == null || _Target.Model == null)
                return;

            LastLog = RuntimeSettings.FormatDamage(
                _Attacker.Model.DisplayName,
                _Target.Model.DisplayName,
                _Result.FinalDamage.ToString("0"),
                _Result.IsCritical);
        }

        private IEnumerator SpawnAfterDelay(float _DelaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _DelaySeconds));
            SpawnNextMonster();
        }

        private IEnumerator AdvanceStageAfterDelay()
        {
            LastLog = RuntimeSettings.FormatStageCleared(CurrentStageNumberValue);
            yield return new WaitForSeconds(Mathf.Max(0f, RuntimeSettings.StageAdvanceDelay));
            BeginStage(CurrentStageNumberValue + 1);
        }

        private void ClearSpawnedMonsters()
        {
            ActiveMonsterActor = null;
            ActiveMonsterDefinition = null;

            if (Context == null)
                return;

            for (int i = Context.Actors.Count - 1; i >= 0; i--)
            {
                CombatActor actor = Context.Actors[i];
                if (actor == null || actor.Team == ActorTeam.Player)
                    continue;

                actor.Died -= HandleMonsterDied;
                actor.DamageTaken -= HandleDamageTaken;
                actor.SetTarget(null);
                Context.Unregister(actor);
                DestroyActorObject(actor.gameObject);
            }
        }

        private static void DestroyActorObject(GameObject _ActorObject)
        {
            if (_ActorObject == null)
                return;

            if (Application.isPlaying)
                Destroy(_ActorObject);
            else
                DestroyImmediate(_ActorObject);
        }

        private static T Require<T>(T _Value, string _Name) where T : class
        {
            if (_Value == null)
                throw new ArgumentNullException(_Name);

            return _Value;
        }
    }
}
