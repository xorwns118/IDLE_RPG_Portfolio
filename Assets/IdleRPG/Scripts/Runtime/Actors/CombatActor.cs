using System;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Combat;
using UnityEngine;

namespace IdleRPG.Runtime.Actors
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CombatActor : MonoBehaviour
    {
        private bool DeathRaised;
        private SpriteRenderer ActorSpriteRenderer;
        private Color DefeatedTint = new Color(0.25f, 0.25f, 0.25f, 0.7f);

        public ActorModel Model { get; private set; }
        public CombatActor CurrentTarget { get; private set; }
        public ActorTeam Team => Model != null ? Model.Team : ActorTeam.Monster;
        public bool IsAlive => Model != null && !Model.IsDead;

        public event Action<CombatActor> Died;
        public event Action<CombatActor, CombatActor, DamageResult> DamageTaken;

        public void Initialize(ActorModel _Model, Sprite _Sprite, Color _Color)
        {
            Initialize(_Model, _Sprite, _Color, DefeatedTint, _Model != null && _Model.Team == ActorTeam.Player ? 10 : 9);
        }

        public void Initialize(ActorModel _Model, Sprite _Sprite, Color _Color, Color _DefeatedTint, int _SortingOrder)
        {
            if (Model != null)
            {
                Model.Died -= HandleModelDied;
            }

            DeathRaised = false;
            DefeatedTint = _DefeatedTint;
            Model = _Model ?? throw new ArgumentNullException(nameof(_Model));
            ActorSpriteRenderer = GetComponent<SpriteRenderer>();
            ActorSpriteRenderer.sprite = _Sprite;
            ActorSpriteRenderer.color = _Color;
            ActorSpriteRenderer.sortingOrder = _SortingOrder;
            name = _Model.DisplayName;

            Model.Died += HandleModelDied;
        }

        public void SetTarget(CombatActor _Target)
        {
            CurrentTarget = _Target;
        }

        public DamageResult TakeBasicAttack(CombatActor _Attacker, float _CriticalRoll)
        {
            if (!IsAlive || _Attacker == null || _Attacker.Model == null)
            {
                return DamageResult.None;
            }

            DamageResult result = Model.ReceiveBasicAttack(_Attacker.Model.Stats, _CriticalRoll);
            DamageTaken?.Invoke(this, _Attacker, result);
            return result;
        }

        public void Face(Vector3 _Point)
        {
            if (ActorSpriteRenderer != null)
            {
                ActorSpriteRenderer.flipX = _Point.x < transform.position.x;
            }
        }

        private void HandleModelDied(ActorModel _Model)
        {
            if (DeathRaised)
            {
                return;
            }

            DeathRaised = true;

            if (ActorSpriteRenderer != null)
            {
                ActorSpriteRenderer.color = DefeatedTint;
            }

            Died?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Model != null)
            {
                Model.Died -= HandleModelDied;
            }
        }
    }
}
