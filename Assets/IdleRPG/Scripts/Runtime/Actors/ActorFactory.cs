using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.UI;
using UnityEngine;

namespace IdleRPG.Runtime.Actors
{
    public sealed class ActorFactory
    {
        private readonly Sprite UnitSprite;
        private readonly MvpActorViewSettings VisualSettings;

        public ActorFactory(Sprite _UnitSprite)
            : this(_UnitSprite, MvpActorViewSettings.CreateDefault())
        {
        }

        public ActorFactory(Sprite _UnitSprite, MvpActorViewSettings _VisualSettings)
        {
            UnitSprite = _UnitSprite;
            VisualSettings = _VisualSettings ?? MvpActorViewSettings.CreateDefault();
            VisualSettings.EnsureDefaults();
        }

        public CombatActor CreateActor(ActorModel _Model, Vector3 _Position, Color _Color, BattleContext _Context)
        {
            GameObject actorObject = new GameObject(_Model.DisplayName);
            actorObject.transform.position = _Position;

            return ConfigureActor(actorObject, _Model, _Color, _Context);
        }

        public CombatActor ConfigureActor(GameObject _ActorObject, ActorModel _Model, Color _Color, BattleContext _Context)
        {
            _ActorObject.name = _Model.DisplayName;
            _ActorObject.transform.localScale = _Model.Team == ActorTeam.Player
                ? VisualSettings.PlayerScale
                : VisualSettings.MonsterScale;

            CombatActor actor = GetOrAdd<CombatActor>(_ActorObject);
            int sortingOrder = _Model.Team == ActorTeam.Player
                ? VisualSettings.PlayerSortingOrder
                : VisualSettings.MonsterSortingOrder;
            actor.Initialize(_Model, UnitSprite, _Color, VisualSettings.DefeatedTint, sortingOrder);

            HealthBarView healthBar = GetOrAdd<HealthBarView>(_ActorObject);
            healthBar.Initialize(actor, UnitSprite, VisualSettings.HealthBar, _Color);

            EnsureNameLabel(_ActorObject.transform, _Model.DisplayName, sortingOrder + VisualSettings.LabelSortingOrderOffset);

            AutoCombatController controller = GetOrAdd<AutoCombatController>(_ActorObject);
            controller.Initialize(_Context, VisualSettings.AutoCombat);

            _Context.Register(actor);
            return actor;
        }

        private void EnsureNameLabel(Transform _Actor, string _DisplayName, int _SortingOrder)
        {
            Transform labelTransform = FindOrCreateChild(_Actor, "Name Label");
            labelTransform.localPosition = VisualSettings.NameLabelOffset;
            labelTransform.localRotation = Quaternion.identity;
            labelTransform.localScale = Vector3.one;

            TextMesh textMesh = GetOrAdd<TextMesh>(labelTransform.gameObject);
            textMesh.text = _DisplayName;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = VisualSettings.NameLabelCharacterSize;
            textMesh.fontSize = VisualSettings.NameLabelFontSize;
            textMesh.color = VisualSettings.NameLabelColor;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                textMesh.font = font;
            }

            MeshRenderer renderer = GetOrAdd<MeshRenderer>(labelTransform.gameObject);
            renderer.sortingOrder = _SortingOrder;
            if (textMesh.font != null)
            {
                renderer.sharedMaterial = textMesh.font.material;
            }
        }

        private static Transform FindOrCreateChild(Transform _Parent, string _ChildName)
        {
            Transform child = _Parent.Find(_ChildName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(_ChildName);
            childObject.transform.SetParent(_Parent, false);
            return childObject.transform;
        }

        private static T GetOrAdd<T>(GameObject _GameObject) where T : Component
        {
            T component = _GameObject.GetComponent<T>();
            return component != null ? component : _GameObject.AddComponent<T>();
        }
    }
}
