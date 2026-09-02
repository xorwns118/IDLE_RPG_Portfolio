using System.Collections;
using System.Collections.Generic;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    public sealed class BattleContext : MonoBehaviour
    {
        private readonly List<CombatActor> RegisteredActors = new List<CombatActor>();
        private readonly ITargetSelector TargetSelector = new DefaultTargetSelector();
        private MvpTargetingSettings TargetingSettings = new MvpTargetingSettings();

        public IReadOnlyList<CombatActor> Actors => RegisteredActors;
        public CombatActor Player { get; private set; }
        public TileMapLayout TileMap { get; private set; }

        public MvpTargetingSettings Targeting => TargetingSettings;

        public void SetTileMap(TileMapLayout _TileMap)
        {
            TileMap = _TileMap;
        }

        public void ConfigureTargeting(MvpTargetingSettings _TargetingSettings)
        {
            TargetingSettings = _TargetingSettings ?? new MvpTargetingSettings();
            TargetingSettings.EnsureDefaults();
        }

        public void Register(CombatActor _Actor)
        {
            if (_Actor == null || RegisteredActors.Contains(_Actor))
                return;

            RegisteredActors.Add(_Actor);
            _Actor.Died += HandleActorDied;

            if (_Actor.Team == ActorTeam.Player)
                Player = _Actor;
        }

        public void Unregister(CombatActor _Actor)
        {
            if (_Actor == null)
                return;

            _Actor.Died -= HandleActorDied;
            RegisteredActors.Remove(_Actor);

            if (Player == _Actor)
                Player = null;
        }

        public CombatActor FindNearestEnemy(CombatActor _Requester)
        {
            return FindTarget(_Requester);
        }

        public CombatActor FindTarget(CombatActor _Requester)
        {
            return TargetSelector.SelectTarget(_Requester, RegisteredActors, TargetingSettings);
        }

        private void HandleActorDied(CombatActor _Actor)
        {
            if (_Actor != null && _Actor.Team != ActorTeam.Player)
                StartCoroutine(RemoveDeadActor(_Actor));
        }

        private IEnumerator RemoveDeadActor(CombatActor _Actor)
        {
            yield return new WaitForSeconds(0.35f);
            Unregister(_Actor);

            if (_Actor != null)
                Destroy(_Actor.gameObject);
        }
    }
}
