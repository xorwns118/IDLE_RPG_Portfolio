using System.Collections;
using IdleRPG.Domain;
using IdleRPG.Domain.Combat;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
using IdleRPG.Runtime.UI;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class TurnBasedAutoBattleController : MonoBehaviour, ICombatLoop
    {
        [SerializeField] private MvpTurnCombatSettings Settings = new MvpTurnCombatSettings();

        private BattleContext Context;
        private float TurnTimer;
        private int TurnCursor = -1;
        private readonly SkillExecutor Skills = new SkillExecutor();
        private bool RuntimeActive;
        private bool IsActing;

        public CombatLoopMode Mode => CombatLoopMode.TurnBased;
        public bool IsRuntimeActive => RuntimeActive;
        public int ExecutedTurnCount { get; private set; }

        public void Initialize(BattleContext _Context, MvpTurnCombatSettings _Settings)
        {
            Initialize(_Context, _Settings, true);
        }

        public void Initialize(BattleContext _Context, MvpTurnCombatSettings _Settings, bool _RuntimeActive)
        {
            Context = _Context;
            Settings = _Settings ?? new MvpTurnCombatSettings();
            Settings.EnsureDefaults();
            TurnTimer = Settings.TurnDelaySeconds;
            TurnCursor = Settings.PlayerActsFirst ? -1 : 0;
            ExecutedTurnCount = 0;
            IsActing = false;
            SetRuntimeActive(_RuntimeActive);
        }

        public void SetRuntimeActive(bool _Active)
        {
            RuntimeActive = _Active;
            enabled = RuntimeActive;
        }

        public bool TryExecuteTurn(float _CriticalRoll)
        {
            if (Context == null || !RuntimeActive || IsActing)
                return false;

            CombatActor actor = ResolveNextActor();
            if (actor == null)
                return false;

            CombatActor target = Context.FindTarget(actor);
            actor.SetTarget(target);
            if (target == null)
            {
                actor.Model.SetState(ActorState.Search);
                actor.PlayIdleAnimation();
                return false;
            }

            if (TryUseSkill(actor, target, _CriticalRoll))
            {
                ExecutedTurnCount++;
                return true;
            }

            if (!IsInsideAttackRange(actor, target))
            {
                if (!TryMoveTowardTarget(actor, target))
                    return false;

                ExecutedTurnCount++;
                return true;
            }

            Attack(actor, target, _CriticalRoll);
            ExecutedTurnCount++;
            return true;
        }

        private void Update()
        {
            if (Context == null || !RuntimeActive || IsActing)
                return;

            Context.TickActors(Time.deltaTime);
            TurnTimer -= Time.deltaTime;
            if (TurnTimer > 0f)
                return;

            TryExecuteTurn(Random.value);
            TurnTimer = Settings.TurnDelaySeconds;
        }

        private bool TryUseSkill(CombatActor _Actor, CombatActor _Target, float _CriticalRoll)
        {
            float distance = Vector2.Distance(_Actor.transform.position, _Target.transform.position);
            return Skills.TryExecuteBestSkill(_Actor, _Target, distance, _CriticalRoll, out SkillExecutionResult result) && result.Succeeded;
        }

        private bool IsInsideAttackRange(CombatActor _Actor, CombatActor _Target)
        {
            TileMapLayout tileMap = Context.TileMap;
            if (tileMap != null && tileMap.IsEnabled && Settings.UseTileMovement)
            {
                Vector2Int actorCell = tileMap.WorldToCell(_Actor.transform.position);
                Vector2Int targetCell = tileMap.WorldToCell(_Target.transform.position);
                int attackRange = tileMap.GetAttackRangeInCells(_Actor.Model.Stats.AttackRange, Context.Targeting.AttackRangePadding);
                return tileMap.GetCellDistance(actorCell, targetCell) <= attackRange;
            }

            return CombatRangePolicy.IsInsideAttackRange(_Actor, _Target, Context.Targeting.AttackRangePadding);
        }

        private bool TryMoveTowardTarget(CombatActor _Actor, CombatActor _Target)
        {
            TileMapLayout tileMap = Context.TileMap;
            if (tileMap != null && tileMap.IsEnabled && Settings.UseTileMovement)
                return TryMoveByTile(_Actor, _Target, tileMap);

            return TryMoveByWorld(_Actor, _Target);
        }

        private bool TryMoveByTile(CombatActor _Actor, CombatActor _Target, TileMapLayout _TileMap)
        {
            Vector2Int actorCell = _TileMap.WorldToCell(_Actor.transform.position);
            Vector2Int targetCell = _TileMap.WorldToCell(_Target.transform.position);
            int attackRange = _TileMap.GetAttackRangeInCells(_Actor.Model.Stats.AttackRange, Context.Targeting.AttackRangePadding);
            Vector2Int nextCell = _TileMap.GetNextCellToward(actorCell, targetCell, attackRange);
            if (nextCell == actorCell)
            {
                _Actor.Model.SetState(ActorState.Search);
                _Actor.PlayIdleAnimation();
                return false;
            }

            MoveActor(_Actor, _TileMap.CellToActorWorld(nextCell), _Target.transform.position, _TileMap);
            return true;
        }

        private bool TryMoveByWorld(CombatActor _Actor, CombatActor _Target)
        {
            Vector3 approachPosition = CombatRangePolicy.GetApproachPosition(
                _Actor.transform.position,
                _Target.transform.position,
                _Actor.Model.Stats.AttackRange,
                Context.Targeting.AttackRangePadding);
            float moveDistance = _Actor.Model.Stats.MoveSpeed * Settings.WorldMoveSecondsPerTurn;
            Vector3 destination = Vector3.MoveTowards(_Actor.transform.position, approachPosition, moveDistance);
            if (Vector3.Distance(_Actor.transform.position, destination) <= Settings.ArrivalThreshold)
            {
                _Actor.Model.SetState(ActorState.Search);
                _Actor.PlayIdleAnimation();
                return false;
            }

            MoveActor(_Actor, destination, _Target.transform.position, null);
            return true;
        }

        private void MoveActor(CombatActor _Actor, Vector3 _Destination, Vector3 _FacingPoint, TileMapLayout _TileMap)
        {
            if (Application.isPlaying && Settings.MoveAnimationDuration > 0f && isActiveAndEnabled)
            {
                StartCoroutine(MoveActorRoutine(_Actor, _Destination, _FacingPoint, _TileMap));
                return;
            }

            MoveActorImmediately(_Actor, _Destination, _FacingPoint, _TileMap);
        }

        private IEnumerator MoveActorRoutine(CombatActor _Actor, Vector3 _Destination, Vector3 _FacingPoint, TileMapLayout _TileMap)
        {
            IsActing = true;
            Vector3 startPosition = _Actor.transform.position;
            float duration = Mathf.Max(0.01f, Settings.MoveAnimationDuration);
            float elapsed = 0f;

            _Actor.Model.SetState(ActorState.Move);
            _Actor.Face(_FacingPoint);

            while (elapsed < duration && _Actor != null && _Actor.IsAlive)
            {
                elapsed += Time.deltaTime;
                Vector3 previousPosition = _Actor.transform.position;
                _Actor.transform.position = Vector3.Lerp(startPosition, _Destination, Mathf.Clamp01(elapsed / duration));
                _Actor.PlayMovementAnimation(_Actor.transform.position - previousPosition, _FacingPoint);
                ApplyTileSorting(_Actor, _TileMap);
                yield return null;
            }

            if (_Actor != null && _Actor.IsAlive)
            {
                _Actor.transform.position = _Destination;
                _Actor.PlayIdleAnimation();
                ApplyTileSorting(_Actor, _TileMap);
            }

            IsActing = false;
        }

        private void MoveActorImmediately(CombatActor _Actor, Vector3 _Destination, Vector3 _FacingPoint, TileMapLayout _TileMap)
        {
            _Actor.Model.SetState(ActorState.Move);
            _Actor.Face(_FacingPoint);
            Vector3 previousPosition = _Actor.transform.position;
            _Actor.transform.position = _Destination;
            _Actor.PlayMovementAnimation(_Actor.transform.position - previousPosition, _FacingPoint);
            _Actor.PlayIdleAnimation();
            ApplyTileSorting(_Actor, _TileMap);
        }

        private static void Attack(CombatActor _Actor, CombatActor _Target, float _CriticalRoll)
        {
            _Actor.Model.SetState(ActorState.Attack);
            _Actor.Face(_Target.transform.position);
            _Actor.PlayIdleAnimation();
            _Target.TakeBasicAttack(_Actor, _CriticalRoll);
        }

        private static void ApplyTileSorting(CombatActor _Actor, TileMapLayout _TileMap)
        {
            if (_TileMap == null || !_TileMap.IsEnabled || _Actor == null)
                return;

            int actorSortingOrder = _TileMap.GetActorSortingOrder(_Actor.transform.position, _Actor.SortingOrder);
            _Actor.SetSortingOrder(actorSortingOrder);

            int overlaySortingOrder = _TileMap.GetOverlaySortingOrder(actorSortingOrder);
            HealthBarView healthBar = _Actor.GetComponent<HealthBarView>();
            if (healthBar != null)
                healthBar.SetSortingBase(overlaySortingOrder);

            Transform label = _Actor.transform.Find("Name Label");
            MeshRenderer labelRenderer = label != null ? label.GetComponent<MeshRenderer>() : null;
            if (labelRenderer != null)
                labelRenderer.sortingOrder = overlaySortingOrder + 2;
        }

        private CombatActor ResolveNextActor()
        {
            if (Context.Actors.Count == 0)
                return null;

            for (int i = 0; i < Context.Actors.Count; i++)
            {
                TurnCursor = (TurnCursor + 1) % Context.Actors.Count;
                CombatActor actor = Context.Actors[TurnCursor];
                if (actor != null && actor.IsAlive)
                    return actor;
            }

            return null;
        }
    }
}
