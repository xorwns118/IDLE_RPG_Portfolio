using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Stages;
using UnityEngine;

namespace IdleRPG.Runtime.UI
{
    public sealed class CombatHud : MonoBehaviour
    {
        private StageController Stage;
        private BattleContext Context;
        private GUIStyle BoxStyle;
        private GUIStyle LabelStyle;
        private GUIStyle TitleStyle;

        public void Initialize(StageController _Stage, BattleContext _Context)
        {
            Stage = _Stage;
            Context = _Context;
        }

        private void OnGUI()
        {
            if (Stage == null || Context == null)
                return;

            EnsureStyles();

            GUI.Box(new Rect(16f, 16f, 380f, 170f), GUIContent.none, BoxStyle);
            GUILayout.BeginArea(new Rect(30f, 26f, 350f, 150f));
            GUILayout.Label("Idle RPG - Week 1 Vertical Slice", TitleStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Stage: " + Stage.CurrentStageNumber + "  Kills: " + Stage.KillsInStage + "/" + Stage.RequiredKills, LabelStyle);
            GUILayout.Label("Gold: " + Stage.TotalGold + "  EXP: " + Stage.TotalExp, LabelStyle);
            GUILayout.Label("Player: " + FormatActor(Stage.Player), LabelStyle);
            GUILayout.Label("Enemy: " + FormatActor(Stage.ActiveMonster), LabelStyle);
            GUILayout.Space(4f);
            GUILayout.Label(Stage.LastLog, LabelStyle);
            GUILayout.EndArea();
        }

        private static string FormatActor(CombatActor _Actor)
        {
            if (_Actor == null || _Actor.Model == null)
                return "-";

            return _Actor.Model.DisplayName + " "
                + _Actor.Model.CurrentHp.ToString("0") + "/" + _Actor.Model.Stats.MaxHp.ToString("0")
                + " HP [" + _Actor.Model.State + "]";
        }

        private void EnsureStyles()
        {
            if (BoxStyle != null)
                return;

            BoxStyle = new GUIStyle(GUI.skin.box);
            BoxStyle.normal.background = Texture2D.grayTexture;

            LabelStyle = new GUIStyle(GUI.skin.label);
            LabelStyle.normal.textColor = Color.white;
            LabelStyle.fontSize = 14;

            TitleStyle = new GUIStyle(LabelStyle);
            TitleStyle.fontStyle = FontStyle.Bold;
            TitleStyle.fontSize = 15;
        }
    }
}
