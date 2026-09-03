using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Combat;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
using IdleRPG.Runtime.UI;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    [RequireComponent(typeof(CombatActor))]
    public sealed class AutoCombatController : MonoBehaviour, ICombatLoop
    {
        private CombatActor Actor;
        private BattleContext Context;
        private MvpAutoCombatSettings Settings = new MvpAutoCombatSettings();
        private MvpTileNavigationSettings NavigationSettings = new MvpTileNavigationSettings();
        private HealthBarView HealthBar;
        private MeshRenderer NameLabelRenderer;
        private readonly SkillExecutor Skills = new SkillExecutor();
        private readonly SkillReadinessGate SkillReadiness = new SkillReadinessGate();
        private float AttackTimer;
        private float SkillUseDelayTimer;
        private bool RuntimeActive = true;
        private bool HasTileMoveTarget;
        private Vector2Int TileMoveTargetCell;

        public CombatLoopMode Mode => CombatLoopMode.Realtime;
        public bool IsRuntimeActive => RuntimeActive && Settings.Enabled;
        public float RemainingSkillUseDelaySeconds => SkillUseDelayTimer;

        public void Initialize(BattleContext _Context)
        {
            Initialize(_Context, Settings);
        }

        public void Initialize(BattleContext _Context, MvpAutoCombatSettings _Settings)
        {
            Initialize(_Context, _Settings, new MvpTileNavigationSettings());
        }

        public void Initialize(
            BattleContext _Context,
            MvpAutoCombatSettings _Settings,
            MvpTileNavigationSettings _NavigationSettings)
        {
            Context = _Context;
            Settings = _Settings ?? new MvpAutoCombatSettings();
            NavigationSettings = _NavigationSettings ?? new MvpTileNavigationSettings();
            Settings.EnsureDefaults();
            NavigationSettings.EnsureDefaults();
            Actor = GetComponent<CombatActor>();
            HealthBar = GetComponent<HealthBarView>();
            NameLabelRenderer = ResolveNameLabelRenderer();
            AttackTimer = Random.Range(Settings.InitialAttackDelayMin, Settings.ClampInitialDelayMax());
            SkillUseDelayTimer = 0f;
            SkillReadiness.Clear();
            ClearTileMoveTarget();
            RuntimeActive = Settings.Enabled;
            enabled = IsRuntimeActive;
            ApplyTileSorting(Context != null ? Context.TileMap : null);
        }

        public void SetRuntimeActive(bool _Active)
        {
            RuntimeActive = _Active;
            enabled = IsRuntimeActive;
        }

        private void Awake()
        {
            Actor = GetComponent<CombatActor>();
            HealthBar = GetComponent<HealthBarView>();
            NameLabelRenderer = ResolveNameLabelRenderer();
        }

        private void Update()
        {
            if (Context == null || Actor == null || !Actor.IsAlive || !IsRuntimeActive)
                return;

            Actor.Model.Tick(Time.deltaTime);
            AttackTimer -= Time.deltaTime;
            UpdateSkillTimers(Time.deltaTime);

            CombatActor target = Context.FindTarget(Actor);
            Actor.SetTarget(target);
            TileMapLayout tileMap = Context.TileMap;
            ApplyTileSorting(tileMap);

            if (target == null)
            {
                ClearTileMoveTarget();
                Actor.Model.SetState(ActorState.Search);
                Actor.PlayIdleAnimation();
                return;
            }

            if (TryUseSkill(target))
                return;

            if (tileMap != null && tileMap.IsEnabled && NavigationSettings.UseTileMovement)
            {
                UpdateTileCombat(target, tileMap);
                return;
            }

            ClearTileMoveTarget();
            if (!CombatRangePolicy.IsInsideAttackRange(Actor, target, Context.Targeting.AttackRangePadding))
            {
                Vector3 approachPosition = CombatRangePolicy.GetApproachPosition(
                    transform.position,
                    target.transform.position,
                    Actor.Model.Stats.AttackRange,
                    Context.Targeting.AttackRangePadding);
                MoveToward(approachPosition, target.transform.position);
                return;
            }

            Attack(target);
        }

        private void UpdateTileCombat(CombatActor _Target, TileMapLayout _TileMap)
        {
            Vector2Int actorCell = _TileMap.WorldToCell(transform.position);
            Vector2Int targetCell = _TileMap.WorldToCell(_Target.transform.position);
            int attackRange = _TileMap.GetAttackRangeInCells(Actor.Model.Stats.AttackRange, Context.Targeting.AttackRangePadding);
            int distance = _TileMap.GetNavigationDistance(actorCell, targetCell, NavigationSettings);

            if (HasTileMoveTarget
                && !NavigationSettings.UseWaypointCompression
                && _TileMap.GetNavigationDistance(actorCell, TileMoveTargetCell, NavigationSettings) > 1)
            {
                ClearTileMoveTarget();
            }

            if (distance <= attackRange)
            {
                ClearTileMoveTarget();
                Attack(_Target);
                return;
            }

            if (HasTileMoveTarget)
            {
                if (!_TileMap.IsWalkable(TileMoveTargetCell))
                {
                    ClearTileMoveTarget();
                }
                else
                {
                    if (MoveToward(_TileMap.CellToActorWorld(TileMoveTargetCell), _Target.transform.position))
                        ClearTileMoveTarget();

                    ApplyTileSorting(_TileMap);
                    return;
                }
            }

            Vector2Int nextCell = NavigationSettings.UseWaypointCompression
                ? _TileMap.GetNextWaypointToward(actorCell, targetCell, attackRange, NavigationSettings)
                : _TileMap.GetNextCellToward(actorCell, targetCell, attackRange, NavigationSettings);
            if (nextCell == actorCell)
            {
                Actor.Model.SetState(ActorState.Search);
                Actor.PlayIdleAnimation();
                return;
            }

            TileMoveTargetCell = nextCell;
            HasTileMoveTarget = true;
            if (MoveToward(_TileMap.CellToActorWorld(TileMoveTargetCell), _Target.transform.position))
                ClearTileMoveTarget();

            ApplyTileSorting(_TileMap);
        }

        private bool TryUseSkill(CombatActor _Target)
        {
            if (!Actor.IsInCombat)
            {
                SkillReadiness.Clear(Actor.Model.SkillLoadout);
                return false;
            }

            if (SkillUseDelayTimer > 0f)
                return false;

            float distance = Vector2.Distance(transform.position, _Target.transform.position);
            if (!Skills.TryExecuteBestSkill(
                Actor,
                _Target,
                distance,
                Random.value,
                SkillReadiness,
                Settings.SkillReadyDelaySeconds,
                out SkillExecutionResult result))
                return false;

            AttackTimer = Actor.Model.Stats.AttackInterval;
            SkillUseDelayTimer = Settings.SkillUseDelaySeconds;
            return result.Succeeded;
        }

        private void UpdateSkillTimers(float _DeltaSeconds)
        {
            if (!Actor.IsInCombat)
            {
                SkillUseDelayTimer = 0f;
                SkillReadiness.Clear(Actor.Model.SkillLoadout);
                return;
            }

            SkillUseDelayTimer = Mathf.Max(0f, SkillUseDelayTimer - _DeltaSeconds);
            SkillReadiness.Tick(_DeltaSeconds);
        }

        private bool MoveToward(Vector3 _MoveTarget, Vector3 _FacingPoint)
        {
            Actor.Model.SetState(ActorState.Move);
            Actor.Face(_FacingPoint);
            Vector3 previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                _MoveTarget,
                Actor.Model.Stats.MoveSpeed * Time.deltaTime);

            bool arrived = Vector3.Distance(transform.position, _MoveTarget) <= Settings.TileArrivalThreshold;
            if (arrived)
                transform.position = _MoveTarget;

            Actor.PlayMovementAnimation(transform.position - previousPosition, _FacingPoint);
            return arrived;
        }

        private void Attack(CombatActor _Target)
        {
            ClearTileMoveTarget();
            Actor.Model.SetState(ActorState.Attack);
            Actor.Face(_Target.transform.position);
            Actor.PlayIdleAnimation();

            if (AttackTimer <= 0f)
            {
                AttackTimer = Actor.Model.Stats.AttackInterval;
                _Target.TakeBasicAttack(Actor, Random.value);
            }
        }

        private void ApplyTileSorting(TileMapLayout _TileMap)
        {
            if (_TileMap == null || !_TileMap.IsEnabled || Actor == null)
                return;

            int actorSortingOrder = _TileMap.GetActorSortingOrder(transform.position, Actor.SortingOrder);
            Actor.SetSortingOrder(actorSortingOrder);

            int overlaySortingOrder = _TileMap.GetOverlaySortingOrder(actorSortingOrder);
            if (HealthBar != null)
                HealthBar.SetSortingBase(overlaySortingOrder);

            if (NameLabelRenderer != null)
                NameLabelRenderer.sortingOrder = overlaySortingOrder + 2;
        }

        private MeshRenderer ResolveNameLabelRenderer()
        {
            Transform label = transform.Find("Name Label");
            return label != null ? label.GetComponent<MeshRenderer>() : null;
        }

        private void ClearTileMoveTarget()
        {
            HasTileMoveTarget = false;
        }
    }
}
