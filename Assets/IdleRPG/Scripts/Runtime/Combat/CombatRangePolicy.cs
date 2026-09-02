using IdleRPG.Runtime.Actors;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    public readonly struct CombatRangeStatus
    {
        public CombatRangeStatus(float _Distance, float _AttackRange, float _SearchRange)
        {
            Distance = _Distance;
            AttackRange = _AttackRange;
            SearchRange = _SearchRange;
        }

        public float Distance { get; }
        public float AttackRange { get; }
        public float SearchRange { get; }
        public bool IsInsideAttackRange => Distance <= AttackRange;
        public bool IsInsideSearchRange => Distance <= SearchRange;
    }

    public static class CombatRangePolicy
    {
        public static CombatRangeStatus GetStatus(CombatActor _Requester, CombatActor _Target, float _AttackRangePadding, float _SearchRange)
        {
            if (_Requester == null || _Target == null || _Requester.Model == null)
                return new CombatRangeStatus(float.MaxValue, 0f, Mathf.Max(0f, _SearchRange));

            float distance = Vector2.Distance(_Requester.transform.position, _Target.transform.position);
            float attackRange = Mathf.Max(0.01f, _Requester.Model.Stats.AttackRange + Mathf.Max(0f, _AttackRangePadding));
            return new CombatRangeStatus(distance, attackRange, Mathf.Max(0f, _SearchRange));
        }

        public static bool IsInsideAttackRange(CombatActor _Requester, CombatActor _Target, float _AttackRangePadding)
        {
            return GetStatus(_Requester, _Target, _AttackRangePadding, float.MaxValue).IsInsideAttackRange;
        }

        public static Vector3 GetApproachPosition(Vector3 _From, Vector3 _Target, float _AttackRange, float _AttackRangePadding)
        {
            Vector3 direction = _Target - _From;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
                return _Target;

            float stopDistance = Mathf.Max(0.01f, _AttackRange + Mathf.Max(0f, _AttackRangePadding));
            return _Target - direction.normalized * stopDistance;
        }
    }
}
