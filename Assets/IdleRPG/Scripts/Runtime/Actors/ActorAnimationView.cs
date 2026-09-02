using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Actors
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Idle RPG/Actor Animation View")]
    public sealed class ActorAnimationView : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator AnimatorComponent;
        [SerializeField] private SpriteRenderer SpriteRendererComponent;
        [SerializeField] private RuntimeAnimatorController Controller;

        [Header("Parameters")]
        [SerializeField] private bool Enabled = true;
        [SerializeField] private string WalkParameterName = "IsWalk";
        [SerializeField] private string LeftParameterName = "IsLeft";
        [SerializeField, Min(0f)] private float MovementThreshold = 0.001f;
        [SerializeField] private bool MirrorSpriteRendererByFacing;

        private RuntimeAnimatorController CachedController;
        private int WalkParameterHash;
        private int LeftParameterHash;
        private bool HasWalkParameter;
        private bool HasLeftParameter;
        private bool IsMoving;
        private bool IsFacingLeft;

        public bool HandlesSpriteFacing
        {
            get
            {
                if (!Enabled)
                    return false;

                ResolveComponents();
                if (AnimatorComponent != null && CachedController != AnimatorComponent.runtimeAnimatorController)
                    RefreshAnimatorParameters();

                return MirrorSpriteRendererByFacing || HasLeftParameter;
            }
        }

        public void Configure(RuntimeAnimatorController _Controller, MvpActorAnimationSettings _Settings)
        {
            MvpActorAnimationSettings settings = _Settings ?? new MvpActorAnimationSettings();
            settings.EnsureDefaults();

            Controller = _Controller;
            Enabled = settings.Enabled;
            WalkParameterName = settings.WalkParameterName;
            LeftParameterName = settings.LeftParameterName;
            MovementThreshold = settings.MovementThreshold;
            MirrorSpriteRendererByFacing = settings.MirrorSpriteRendererByFacing;

            ResolveComponents();

            if (AnimatorComponent != null && Controller != null)
                AnimatorComponent.runtimeAnimatorController = Controller;

            RefreshAnimatorParameters();
            PlayIdle();
        }

        public void Face(Vector3 _WorldPoint)
        {
            float deltaX = _WorldPoint.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= MovementThreshold)
                return;

            SetFacingLeft(deltaX < 0f);
        }

        public void PlayMovement(Vector3 _MovementDelta, Vector3 _FacingPoint)
        {
            bool isMoving = _MovementDelta.sqrMagnitude > MovementThreshold * MovementThreshold;
            if (isMoving)
            {
                if (Mathf.Abs(_MovementDelta.x) > MovementThreshold)
                    SetFacingLeft(_MovementDelta.x < 0f);
                else
                    Face(_FacingPoint);

                ApplyAnimatorState(true);
                return;
            }

            Face(_FacingPoint);
            PlayIdle();
        }

        public void PlayIdle()
        {
            ApplyAnimatorState(false);
        }

        private void Awake()
        {
            ResolveComponents();
            RefreshAnimatorParameters();
            PlayIdle();
        }

        private void OnValidate()
        {
            MovementThreshold = Mathf.Max(0f, MovementThreshold);
            if (string.IsNullOrWhiteSpace(WalkParameterName))
                WalkParameterName = "IsWalk";

            if (string.IsNullOrWhiteSpace(LeftParameterName))
                LeftParameterName = "IsLeft";
        }

        private void ResolveComponents()
        {
            if (AnimatorComponent == null)
                AnimatorComponent = GetComponent<Animator>();

            if (SpriteRendererComponent == null)
                SpriteRendererComponent = GetComponent<SpriteRenderer>();
        }

        private void RefreshAnimatorParameters()
        {
            if (AnimatorComponent == null)
            {
                HasWalkParameter = false;
                HasLeftParameter = false;
                return;
            }

            CachedController = AnimatorComponent.runtimeAnimatorController;
            WalkParameterHash = Animator.StringToHash(WalkParameterName);
            LeftParameterHash = Animator.StringToHash(LeftParameterName);
            HasWalkParameter = HasBoolParameter(WalkParameterName);
            HasLeftParameter = HasBoolParameter(LeftParameterName);
        }

        private bool HasBoolParameter(string _ParameterName)
        {
            if (AnimatorComponent == null || AnimatorComponent.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(_ParameterName))
                return false;

            AnimatorControllerParameter[] parameters = AnimatorComponent.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == _ParameterName)
                    return true;
            }

            return false;
        }

        private void SetFacingLeft(bool _FacingLeft)
        {
            IsFacingLeft = _FacingLeft;
            if (SpriteRendererComponent != null && MirrorSpriteRendererByFacing)
                SpriteRendererComponent.flipX = IsFacingLeft;

            ApplyAnimatorState(IsMoving);
        }

        private void ApplyAnimatorState(bool _IsMoving)
        {
            IsMoving = Enabled && _IsMoving;

            if (!Enabled)
                return;

            ResolveComponents();
            if (AnimatorComponent == null || AnimatorComponent.runtimeAnimatorController == null)
                return;

            if (CachedController != AnimatorComponent.runtimeAnimatorController)
                RefreshAnimatorParameters();

            if (HasWalkParameter)
                AnimatorComponent.SetBool(WalkParameterHash, IsMoving);

            if (HasLeftParameter)
                AnimatorComponent.SetBool(LeftParameterHash, IsFacingLeft);
        }
    }
}
