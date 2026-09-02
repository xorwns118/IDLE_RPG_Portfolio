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
        private ActorAnimationView AnimationView;
        private Color DefeatedTint = new Color(0.25f, 0.25f, 0.25f, 0.7f);

        public ActorModel Model { get; private set; }
        public CombatActor CurrentTarget { get; private set; }
        public ActorTeam Team => Model != null ? Model.Team : ActorTeam.Monster;
        public bool IsAlive => Model != null && !Model.IsDead;
        public bool IsInCombat => Model != null && Model.IsInCombat;
        public int SortingOrder => ActorSpriteRenderer != null ? ActorSpriteRenderer.sortingOrder : 0;

        public event Action<CombatActor> Died;
        public event Action<CombatActor, CombatActor, DamageResult> DamageTaken;
        public event Action<CombatActor, CombatActor, SkillExecutionResult> SkillUsed;

        public void Initialize(ActorModel _Model, Sprite _Sprite, Color _Color)
        {
            Initialize(_Model, _Sprite, _Color, DefeatedTint, _Model != null && _Model.Team == ActorTeam.Player ? 10 : 9);
        }

        public void Initialize(ActorModel _Model, Sprite _Sprite, Color _Color, Color _DefeatedTint, int _SortingOrder)
        {
            if (Model != null)
                Model.Died -= HandleModelDied;

            DeathRaised = false;
            DefeatedTint = _DefeatedTint;
            Model = _Model ?? throw new ArgumentNullException(nameof(_Model));
            ActorSpriteRenderer = GetComponent<SpriteRenderer>();
            AnimationView = GetComponent<ActorAnimationView>();
            ActorSpriteRenderer.sprite = _Sprite;
            ActorSpriteRenderer.color = _Color;
            ActorSpriteRenderer.sortingOrder = _SortingOrder;
            name = _Model.DisplayName;
            PlayIdleAnimation();

            Model.Died += HandleModelDied;
        }

        public void SetSortingOrder(int _SortingOrder)
        {
            if (ActorSpriteRenderer != null)
                ActorSpriteRenderer.sortingOrder = _SortingOrder;
        }

        public void SetTarget(CombatActor _Target)
        {
            CurrentTarget = _Target;
        }

        public DamageResult TakeBasicAttack(CombatActor _Attacker, float _CriticalRoll)
        {
            if (!IsAlive || _Attacker == null || _Attacker.Model == null)
                return DamageResult.None;

            _Attacker.Model.EnterCombat();
            Model.EnterCombat();
            DamageResult result = Model.ReceiveBasicAttack(_Attacker.Model.Stats, _CriticalRoll);
            DamageTaken?.Invoke(this, _Attacker, result);
            return result;
        }

        public DamageResult TakeSkillAttack(CombatActor _Attacker, float _PowerMultiplier, float _CriticalRoll)
        {
            if (!IsAlive || _Attacker == null || _Attacker.Model == null)
                return DamageResult.None;

            _Attacker.Model.EnterCombat();
            Model.EnterCombat();
            DamageResult result = Model.ReceiveSkillAttack(_Attacker.Model.Stats, _PowerMultiplier, _CriticalRoll);
            DamageTaken?.Invoke(this, _Attacker, result);
            return result;
        }

        public void NotifySkillUsed(CombatActor _Target, SkillExecutionResult _Result)
        {
            if (!_Result.Succeeded)
                return;

            SkillUsed?.Invoke(this, _Target, _Result);
        }

        public void Face(Vector3 _Point)
        {
            if (ActorSpriteRenderer != null)
            {
                bool shouldFaceLeft = _Point.x < transform.position.x;
                if (AnimationView == null || !AnimationView.HandlesSpriteFacing)
                    ActorSpriteRenderer.flipX = shouldFaceLeft;
            }

            if (AnimationView != null)
                AnimationView.Face(_Point);
        }

        public void PlayMovementAnimation(Vector3 _MovementDelta, Vector3 _FacingPoint)
        {
            if (AnimationView != null)
                AnimationView.PlayMovement(_MovementDelta, _FacingPoint);
        }

        public void PlayIdleAnimation()
        {
            if (AnimationView != null)
                AnimationView.PlayIdle();
        }

        private void HandleModelDied(ActorModel _Model)
        {
            if (DeathRaised)
                return;

            DeathRaised = true;

            if (ActorSpriteRenderer != null)
                ActorSpriteRenderer.color = DefeatedTint;

            PlayIdleAnimation();
            Died?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Model != null)
                Model.Died -= HandleModelDied;
        }
    }
}
