using System.Collections;
using System.Collections.Generic;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Runtime.Actors;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    public sealed class BattleContext : MonoBehaviour
    {
        private readonly List<CombatActor> RegisteredActors = new List<CombatActor>();

        public IReadOnlyList<CombatActor> Actors => RegisteredActors;
        public CombatActor Player { get; private set; }

        public void Register(CombatActor _Actor)
        {
            if (_Actor == null || RegisteredActors.Contains(_Actor))
            {
                return;
            }

            RegisteredActors.Add(_Actor);
            _Actor.Died += HandleActorDied;

            if (_Actor.Team == ActorTeam.Player)
            {
                Player = _Actor;
            }
        }

        public void Unregister(CombatActor _Actor)
        {
            if (_Actor == null)
            {
                return;
            }

            _Actor.Died -= HandleActorDied;
            RegisteredActors.Remove(_Actor);

            if (Player == _Actor)
            {
                Player = null;
            }
        }

        public CombatActor FindNearestEnemy(CombatActor _Requester)
        {
            if (_Requester == null || !_Requester.IsAlive)
            {
                return null;
            }

            CombatActor nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (CombatActor actor in RegisteredActors)
            {
                if (actor == null || actor.Team == _Requester.Team || !actor.IsAlive)
                {
                    continue;
                }

                float distance = Vector2.Distance(_Requester.transform.position, actor.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = actor;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void HandleActorDied(CombatActor _Actor)
        {
            if (_Actor != null && _Actor.Team != ActorTeam.Player)
            {
                StartCoroutine(RemoveDeadActor(_Actor));
            }
        }

        private IEnumerator RemoveDeadActor(CombatActor _Actor)
        {
            yield return new WaitForSeconds(0.35f);
            Unregister(_Actor);

            if (_Actor != null)
            {
                Destroy(_Actor.gameObject);
            }
        }
    }
}
