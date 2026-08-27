using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
using IdleRPG.Runtime.UI;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    [RequireComponent(typeof(CombatActor))]
    public sealed class AutoCombatController : MonoBehaviour
    {
        private CombatActor Actor;
        private BattleContext Context;
        private MvpAutoCombatSettings Settings = new MvpAutoCombatSettings();
        private HealthBarView HealthBar;
        private MeshRenderer NameLabelRenderer;
        private float AttackTimer;

        public void Initialize(BattleContext _Context)
        {
            Initialize(_Context, Settings);
        }

        public void Initialize(BattleContext _Context, MvpAutoCombatSettings _Settings)
        {
            Context = _Context;
            Settings = _Settings ?? new MvpAutoCombatSettings();
            Settings.EnsureDefaults();
            Actor = GetComponent<CombatActor>();
            HealthBar = GetComponent<HealthBarView>();
            NameLabelRenderer = ResolveNameLabelRenderer();
            AttackTimer = Random.Range(Settings.InitialAttackDelayMin, Settings.ClampInitialDelayMax());
            ApplyTileSorting(Context != null ? Context.TileMap : null);
        }

        private void Awake()
        {
            Actor = GetComponent<CombatActor>();
            HealthBar = GetComponent<HealthBarView>();
            NameLabelRenderer = ResolveNameLabelRenderer();
        }

        private void Update()
        {
            if (Context == null || Actor == null || !Actor.IsAlive)
                return;

            AttackTimer -= Time.deltaTime;

            CombatActor target = Context.FindNearestEnemy(Actor);
            Actor.SetTarget(target);
            TileMapLayout tileMap = Context.TileMap;
            ApplyTileSorting(tileMap);

            if (target == null)
            {
                Actor.Model.SetState(ActorState.Search);
                return;
            }

            if (tileMap != null && tileMap.IsEnabled && Settings.UseTileMovement)
            {
                UpdateTileCombat(target, tileMap);
                return;
            }

            float distance = Vector2.Distance(transform.position, target.transform.position);
            if (distance > Actor.Model.Stats.AttackRange)
            {
                MoveToward(target.transform.position, target.transform.position);
                return;
            }

            Attack(target);
        }

        private void UpdateTileCombat(CombatActor _Target, TileMapLayout _TileMap)
        {
            Vector2Int actorCell = _TileMap.WorldToCell(transform.position);
            Vector2Int targetCell = _TileMap.WorldToCell(_Target.transform.position);
            int attackRange = _TileMap.GetAttackRangeInCells(Actor.Model.Stats.AttackRange);
            int distance = _TileMap.GetCellDistance(actorCell, targetCell);

            if (distance > attackRange)
            {
                Vector2Int nextCell = _TileMap.GetNextCellToward(actorCell, targetCell, attackRange);
                MoveToward(_TileMap.CellToActorWorld(nextCell), _Target.transform.position);
                ApplyTileSorting(_TileMap);
                return;
            }

            Attack(_Target);
        }

        private void MoveToward(Vector3 _MoveTarget, Vector3 _FacingPoint)
        {
            Actor.Model.SetState(ActorState.Move);
            Actor.Face(_FacingPoint);
            transform.position = Vector3.MoveTowards(
                transform.position,
                _MoveTarget,
                Actor.Model.Stats.MoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _MoveTarget) <= Settings.TileArrivalThreshold)
            {
                transform.position = _MoveTarget;
            }
        }

        private void Attack(CombatActor _Target)
        {
            Actor.Model.SetState(ActorState.Attack);
            Actor.Face(_Target.transform.position);

            if (AttackTimer <= 0f)
            {
                AttackTimer = Actor.Model.Stats.AttackInterval;
                _Target.TakeBasicAttack(Actor, Random.value);
            }
        }

        private void ApplyTileSorting(TileMapLayout _TileMap)
        {
            if (_TileMap == null || !_TileMap.IsEnabled || Actor == null)
            {
                return;
            }

            int actorSortingOrder = _TileMap.GetActorSortingOrder(transform.position, Actor.SortingOrder);
            Actor.SetSortingOrder(actorSortingOrder);

            int overlaySortingOrder = _TileMap.GetOverlaySortingOrder(actorSortingOrder);
            if (HealthBar != null)
            {
                HealthBar.SetSortingBase(overlaySortingOrder);
            }

            if (NameLabelRenderer != null)
            {
                NameLabelRenderer.sortingOrder = overlaySortingOrder + 2;
            }
        }

        private MeshRenderer ResolveNameLabelRenderer()
        {
            Transform label = transform.Find("Name Label");
            return label != null ? label.GetComponent<MeshRenderer>() : null;
        }
    }
}
