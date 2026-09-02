using System;
using System.Collections;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Combat;
using IdleRPG.Domain.Data;
using IdleRPG.Domain.Skills;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
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
            public TileMapLayout TileMap;
            public int StartStageNumberOverride;
        }

        private RuntimeContentDatabase Database;
        private BattleContext Context;
        private ActorFactory Factory;
        private MonsterSpawner Spawner;
        private MvpStageRuntimeSettings RuntimeSettings = new MvpStageRuntimeSettings();
        private MvpGameContentSettings ContentSettings = MvpGameContentSettings.CreateDefault();
        private MvpActorViewSettings ActorSettings = MvpActorViewSettings.CreateDefault();
        private MvpMonsterSpawnSettings SpawnSettings = new MvpMonsterSpawnSettings();
        private TileMapLayout TileMap;
        private CombatActor PlayerActor;
        private CombatActor ActiveMonsterActor;
        private MonsterDefinition ActiveMonsterDefinition;
        private StageDefinition CurrentStage;
        private Transform MonsterSpawnPoint;
        private Vector3 PlayerStartPosition = DefaultPlayerStartPosition;
        private int CurrentStageNumberValue;
        private int KillsInStageValue;
        private bool RealtimeCombatActive = true;

        public int CurrentStageNumber => CurrentStageNumberValue;
        public int KillsInStage => KillsInStageValue;
        public int RequiredKills => CurrentStage != null ? CurrentStage.RequiredKills : 0;
        public int TotalGold { get; private set; }
        public int TotalExp { get; private set; }
        public string LastLog { get; private set; } = "Ready";
        public CombatActor Player => PlayerActor;
        public CombatActor ActiveMonster => ActiveMonsterActor;
        public bool HasActiveStage => CurrentStageNumberValue > 0 && CurrentStage != null;
        public bool IsPlayerDefeated { get; private set; }
        public bool IsRealtimeCombatActive => RealtimeCombatActive;

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
            TileMap = _Setup.TileMap;
            PlayerStartPosition = _Setup.PlayerStartPosition;

            ContentSettings.EnsureDefaults();
            ActorSettings.EnsureDefaults();
            SpawnSettings.EnsureDefaults();
            Context.SetTileMap(TileMap);
            Context.ConfigureTargeting(ActorSettings.Targeting);

            Spawner = gameObject.GetComponent<MonsterSpawner>();
            if (Spawner == null)
                Spawner = gameObject.AddComponent<MonsterSpawner>();

            Spawner.Initialize(Context, Factory, SpawnSettings);
            Spawner.SetSpawnPoint(MonsterSpawnPoint);
            Spawner.SetTileMap(TileMap);

            int startStageNumber = _Setup.StartStageNumberOverride > 0 ? _Setup.StartStageNumberOverride : RuntimeSettings.StartStageNumber;
            BeginStage(Mathf.Max(1, startStageNumber));
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

        public void StartStage(int _StageNumber)
        {
            if (Database == null || Context == null || Factory == null)
                return;

            StopAllCoroutines();
            BeginStage(Mathf.Max(1, _StageNumber));
        }

        public void ClearRuntime()
        {
            StopAllCoroutines();
            ClearSpawnedMonsters();
            ClearPlayerActor();
            CurrentStage = null;
            CurrentStageNumberValue = 0;
            KillsInStageValue = 0;
            IsPlayerDefeated = false;
            LastLog = RuntimeSettings.FieldReadyLog;
        }

        public void SetRealtimeCombatActive(bool _Active)
        {
            RealtimeCombatActive = _Active;
            ApplyRealtimeCombatActive(PlayerActor);
            ApplyRealtimeCombatActive(ActiveMonsterActor);
        }

        private void PreparePlayerForStage()
        {
            if (PlayerActor != null)
            {
                PlayerActor.Died -= HandlePlayerDied;
                PlayerActor.DamageTaken -= HandleDamageTaken;
                PlayerActor.SkillUsed -= HandleSkillUsed;
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
            ApplyRealtimeCombatActive(PlayerActor);
            PlayerActor.Died += HandlePlayerDied;
            PlayerActor.DamageTaken += HandleDamageTaken;
            PlayerActor.SkillUsed += HandleSkillUsed;
        }

        private ActorModel CreatePlayerModel()
        {
            PlayerDefinition definition = Database.Player;
            ActorModel model = new ActorModel(definition.Id, definition.DisplayName, ActorTeam.Player, definition.Stats);
            model.SetSkillLoadout(new SkillLoadout(definition.SkillLoadout));
            return model;
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
            ActiveMonsterActor.SkillUsed += HandleSkillUsed;
            ApplyRealtimeCombatActive(ActiveMonsterActor);
        }

        private void ApplyRealtimeCombatActive(CombatActor _Actor)
        {
            if (_Actor == null)
                return;

            AutoCombatController controller = _Actor.GetComponent<AutoCombatController>();
            if (controller != null)
                controller.SetRuntimeActive(RealtimeCombatActive);
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
            _Monster.SkillUsed -= HandleSkillUsed;
            ExitCombat(_Monster);
            ExitCombat(PlayerActor);

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
            {
                ActiveMonsterActor.SetTarget(null);
                ExitCombat(ActiveMonsterActor);
            }

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

        private void HandleSkillUsed(CombatActor _Caster, CombatActor _Target, SkillExecutionResult _Result)
        {
            if (!_Result.Succeeded || _Caster == null || _Caster.Model == null)
                return;

            if (_Result.LastDamage.FinalDamage > 0f && _Target != null && _Target.Model != null)
            {
                LastLog = RuntimeSettings.FormatSkillDamage(
                    _Caster.Model.DisplayName,
                    _Result.SkillDisplayName,
                    _Target.Model.DisplayName,
                    _Result.LastDamage.FinalDamage.ToString("0"),
                    _Result.LastDamage.IsCritical);
                return;
            }

            LastLog = RuntimeSettings.FormatSkillUsed(_Caster.Model.DisplayName, _Result.SkillDisplayName);
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
                actor.SkillUsed -= HandleSkillUsed;
                ExitCombat(actor);
                actor.SetTarget(null);
                Context.Unregister(actor);
                DestroyActorObject(actor.gameObject);
            }
        }

        private void ClearPlayerActor()
        {
            if (PlayerActor == null)
                return;

            PlayerActor.Died -= HandlePlayerDied;
            PlayerActor.DamageTaken -= HandleDamageTaken;
            PlayerActor.SkillUsed -= HandleSkillUsed;
            ExitCombat(PlayerActor);
            PlayerActor.SetTarget(null);

            if (Context != null)
                Context.Unregister(PlayerActor);

            DestroyActorObject(PlayerActor.gameObject);
            PlayerActor = null;
        }

        private static void ExitCombat(CombatActor _Actor)
        {
            if (_Actor != null && _Actor.Model != null)
                _Actor.Model.ExitCombat();
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
