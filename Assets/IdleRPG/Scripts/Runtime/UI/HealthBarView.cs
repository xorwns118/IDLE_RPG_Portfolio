using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.UI
{
    public sealed class HealthBarView : MonoBehaviour
    {
        private CombatActor Actor;
        private MvpHealthBarSettings Settings = new MvpHealthBarSettings();
        private Transform Fill;

        public void Initialize(CombatActor _Actor, Sprite _Sprite)
        {
            Initialize(_Actor, _Sprite, Settings, new Color(0.85f, 0.15f, 0.18f));
        }

        public void Initialize(CombatActor _Actor, Sprite _Sprite, MvpHealthBarSettings _Settings, Color _FillColor)
        {
            Actor = _Actor;
            Settings = _Settings ?? new MvpHealthBarSettings();

            CreateBar("HP Background", _Sprite, Settings.BackgroundColor, Settings.BackgroundSortingOrder, Settings.Offset);
            Fill = CreateBar(
                "HP Fill",
                _Sprite,
                _FillColor,
                Settings.FillSortingOrder,
                Settings.Offset + new Vector3(0f, 0f, Settings.FillDepthOffset));
        }

        private void Update()
        {
            if (Actor == null || Actor.Model == null || Fill == null)
            {
                return;
            }

            float percent = Mathf.Clamp01(Actor.Model.CurrentHp / Actor.Model.Stats.MaxHp);
            Fill.localScale = new Vector3(Settings.Width * percent, Settings.Height, 1f);
            Fill.localPosition = Settings.Offset
                + new Vector3(-(Settings.Width - Settings.Width * percent) * 0.5f, 0f, Settings.FillDepthOffset);
        }

        private Transform CreateBar(string _BarName, Sprite _Sprite, Color _Color, int _SortingOrder, Vector3 _LocalPosition)
        {
            Transform existing = transform.Find(_BarName);
            GameObject barObject = existing != null ? existing.gameObject : new GameObject(_BarName);
            barObject.transform.SetParent(transform, false);
            barObject.transform.localPosition = _LocalPosition;
            barObject.transform.localScale = new Vector3(Settings.Width, Settings.Height, 1f);

            SpriteRenderer renderer = barObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = barObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = _Sprite;
            renderer.color = _Color;
            renderer.sortingOrder = _SortingOrder;

            return barObject.transform;
        }
    }
}
