using System;
using UnityEngine;

namespace IdleRPG.Runtime.Configuration
{
    [Serializable]
    public sealed class MvpSceneDesignerSettings
    {
        [Header("Camera")]
        public MvpCameraSettings Camera = new MvpCameraSettings();

        [Header("World Layout")]
        public MvpWorldLayoutSettings World = new MvpWorldLayoutSettings();

        [Header("Actor View")]
        public MvpActorViewSettings Actors = new MvpActorViewSettings();

        [Header("Monster Spawn")]
        public MvpMonsterSpawnSettings Spawn = new MvpMonsterSpawnSettings();

        [Header("HUD")]
        public MvpHudSettings Hud = new MvpHudSettings();

        [Header("Restart Popup")]
        public MvpRestartPanelSettings RestartPanel = new MvpRestartPanelSettings();

        [Header("Stage Runtime")]
        public MvpStageRuntimeSettings Stage = new MvpStageRuntimeSettings();

        public static MvpSceneDesignerSettings CreateDefault()
        {
            return new MvpSceneDesignerSettings();
        }

        public void EnsureDefaults()
        {
            if (Camera == null) Camera = new MvpCameraSettings();
            if (World == null) World = new MvpWorldLayoutSettings();
            if (Actors == null) Actors = new MvpActorViewSettings();
            if (Spawn == null) Spawn = new MvpMonsterSpawnSettings();
            if (Hud == null) Hud = new MvpHudSettings();
            if (RestartPanel == null) RestartPanel = new MvpRestartPanelSettings();
            if (Stage == null) Stage = new MvpStageRuntimeSettings();

            Actors.EnsureDefaults();
            Hud.EnsureDefaults();
            RestartPanel.EnsureDefaults();
        }
    }

    [Serializable]
    public sealed class MvpCameraSettings
    {
        [Tooltip("Main camera position used by the MVP scene builder.")]
        public Vector3 Position = new Vector3(0f, 0.9f, -10f);

        [Min(0.1f)]
        public float OrthographicSize = 4.1f;

        public Color BackgroundColor = new Color(0.08f, 0.1f, 0.13f);
    }

    [Serializable]
    public sealed class MvpWorldLayoutSettings
    {
        public Vector3 GroundPosition = new Vector3(0f, -0.75f, 0f);
        public Vector3 GroundScale = new Vector3(8.5f, 0.08f, 1f);
        public Color GroundColor = new Color(0.34f, 0.38f, 0.42f);
        public int GroundSortingOrder = 1;

        [Tooltip("Hero start position at the beginning of every stage.")]
        public Vector3 PlayerStartPosition = new Vector3(-3.2f, 0f, 0f);

        [Tooltip("Base position used by Monster Spawn Point.")]
        public Vector3 MonsterSpawnPosition = new Vector3(3.3f, 0f, 0f);
    }

    [Serializable]
    public sealed class MvpActorViewSettings
    {
        public Vector3 PlayerScale = new Vector3(0.85f, 1.25f, 1f);
        public Vector3 MonsterScale = new Vector3(0.8f, 1f, 1f);
        public Color PlayerColor = new Color(0.25f, 0.7f, 1f);
        public Color MonsterFallbackColor = new Color(0.35f, 0.9f, 0.55f);
        public Color DefeatedTint = new Color(0.25f, 0.25f, 0.25f, 0.7f);
        public Color NameLabelColor = Color.white;
        public Vector3 NameLabelOffset = new Vector3(0f, 1.35f, 0f);
        [Min(0.01f)] public float NameLabelCharacterSize = 0.18f;
        [Min(1)] public int NameLabelFontSize = 36;
        public int PlayerSortingOrder = 10;
        public int MonsterSortingOrder = 9;
        public int LabelSortingOrderOffset = 20;
        public MvpHealthBarSettings HealthBar = new MvpHealthBarSettings();
        public MvpAutoCombatSettings AutoCombat = new MvpAutoCombatSettings();

        public static MvpActorViewSettings CreateDefault()
        {
            return new MvpActorViewSettings();
        }

        public void EnsureDefaults()
        {
            if (HealthBar == null) HealthBar = new MvpHealthBarSettings();
            if (AutoCombat == null) AutoCombat = new MvpAutoCombatSettings();
            NameLabelCharacterSize = Mathf.Max(0.01f, NameLabelCharacterSize);
            NameLabelFontSize = Mathf.Max(1, NameLabelFontSize);
        }
    }

    [Serializable]
    public sealed class MvpHealthBarSettings
    {
        [Min(0.01f)] public float Width = 1.25f;
        [Min(0.01f)] public float Height = 0.1f;
        public Vector3 Offset = new Vector3(0f, 1.05f, 0f);
        public float FillDepthOffset = -0.01f;
        public Color BackgroundColor = new Color(0.08f, 0.08f, 0.08f);
        public int BackgroundSortingOrder = 20;
        public int FillSortingOrder = 21;
    }

    [Serializable]
    public sealed class MvpAutoCombatSettings
    {
        [Min(0f)] public float InitialAttackDelayMin = 0f;
        [Min(0f)] public float InitialAttackDelayMax = 0.15f;

        public float ClampInitialDelayMax()
        {
            return Mathf.Max(InitialAttackDelayMin, InitialAttackDelayMax);
        }
    }

    [Serializable]
    public sealed class MvpMonsterSpawnSettings
    {
        [Tooltip("Position used when a scene spawn point is not assigned.")]
        public Vector3 FallbackPosition = new Vector3(3.3f, 0f, 0f);

        [Tooltip("Offset added for repeated spawns in the same stage.")]
        public Vector3 RepeatedSpawnOffset = new Vector3(0.35f, 0f, 0f);
    }

    [Serializable]
    public sealed class MvpHudSettings
    {
        public string Title = "Idle RPG MVP";
        public string StageFormat = "Stage {0}   Kills {1}/{2}";
        public string ResourceFormat = "Gold {0}   EXP {1}";
        public string PlayerFormat = "Player: {0}";
        public string EnemyFormat = "Enemy: {0}";
        public string ActorFormat = "{0}  {1}/{2} HP  [{3}]";
        public string EmptyActorText = "-";
        public string PreviewEnemyText = "Spawn ready";
        public string PlayPrompt = "Press Play to run the MVP combat loop.";

        public Vector2 ReferenceResolution = new Vector2(1280f, 720f);
        [Range(0f, 1f)] public float MatchWidthOrHeight = 0.5f;
        public Vector2 StatusPanelPosition = new Vector2(18f, -18f);
        public Vector2 StatusPanelSize = new Vector2(470f, 214f);
        public Color StatusPanelColor = new Color(0.05f, 0.06f, 0.075f, 0.86f);
        public Color TextColor = Color.white;
        public Color UiBarBackgroundColor = new Color(0.13f, 0.13f, 0.15f, 1f);
        public Vector2 UiBarSize = new Vector2(300f, 10f);

        public MvpTextSlotSettings TitleText = new MvpTextSlotSettings(new Vector2(18f, -15f), new Vector2(430f, 26f), 18, FontStyle.Bold);
        public MvpTextSlotSettings StageText = new MvpTextSlotSettings(new Vector2(18f, -48f), new Vector2(430f, 24f), 15, FontStyle.Bold);
        public MvpTextSlotSettings ResourceText = new MvpTextSlotSettings(new Vector2(18f, -74f), new Vector2(430f, 24f), 14, FontStyle.Normal);
        public MvpTextSlotSettings PlayerText = new MvpTextSlotSettings(new Vector2(18f, -106f), new Vector2(430f, 22f), 14, FontStyle.Normal);
        public MvpTextSlotSettings EnemyText = new MvpTextSlotSettings(new Vector2(18f, -145f), new Vector2(430f, 22f), 14, FontStyle.Normal);
        public MvpTextSlotSettings LogText = new MvpTextSlotSettings(new Vector2(18f, -181f), new Vector2(430f, 24f), 13, FontStyle.Italic);
        public Vector2 PlayerHpBarPosition = new Vector2(18f, -128f);
        public Vector2 EnemyHpBarPosition = new Vector2(18f, -167f);

        public void EnsureDefaults()
        {
            if (TitleText == null) TitleText = new MvpTextSlotSettings(new Vector2(18f, -15f), new Vector2(430f, 26f), 18, FontStyle.Bold);
            if (StageText == null) StageText = new MvpTextSlotSettings(new Vector2(18f, -48f), new Vector2(430f, 24f), 15, FontStyle.Bold);
            if (ResourceText == null) ResourceText = new MvpTextSlotSettings(new Vector2(18f, -74f), new Vector2(430f, 24f), 14, FontStyle.Normal);
            if (PlayerText == null) PlayerText = new MvpTextSlotSettings(new Vector2(18f, -106f), new Vector2(430f, 22f), 14, FontStyle.Normal);
            if (EnemyText == null) EnemyText = new MvpTextSlotSettings(new Vector2(18f, -145f), new Vector2(430f, 22f), 14, FontStyle.Normal);
            if (LogText == null) LogText = new MvpTextSlotSettings(new Vector2(18f, -181f), new Vector2(430f, 24f), 13, FontStyle.Italic);
        }

        public string FormatStage(int _StageNumber, int _Kills, int _RequiredKills)
        {
            return MvpTextFormatter.Format(StageFormat, _StageNumber, _Kills, _RequiredKills);
        }

        public string FormatResources(int _Gold, int _Exp)
        {
            return MvpTextFormatter.Format(ResourceFormat, _Gold, _Exp);
        }

        public string FormatPlayer(string _ActorText)
        {
            return MvpTextFormatter.Format(PlayerFormat, _ActorText);
        }

        public string FormatEnemy(string _ActorText)
        {
            return MvpTextFormatter.Format(EnemyFormat, _ActorText);
        }

        public string FormatActor(string _Name, string _CurrentHp, string _MaxHp, object _State)
        {
            return MvpTextFormatter.Format(ActorFormat, _Name, _CurrentHp, _MaxHp, _State);
        }
    }

    [Serializable]
    public sealed class MvpTextSlotSettings
    {
        public Vector2 Position;
        public Vector2 Size;
        [Min(1)] public int FontSize;
        public FontStyle Style;

        public MvpTextSlotSettings()
        {
            Position = Vector2.zero;
            Size = new Vector2(100f, 24f);
            FontSize = 14;
            Style = FontStyle.Normal;
        }

        public MvpTextSlotSettings(Vector2 _Position, Vector2 _Size, int _FontSize, FontStyle _Style)
        {
            Position = _Position;
            Size = _Size;
            FontSize = _FontSize;
            Style = _Style;
        }
    }

    [Serializable]
    public sealed class MvpRestartPanelSettings
    {
        public string TitleFormat = "Stage {0} Failed";
        public string BodyText = "Hero will return to the start point with full HP.";
        public string PreviewTitle = "Stage Failed";
        public string PreviewBody = "Restart this stage from the beginning.";
        public string ButtonText = "Restart Stage";
        public Vector2 PanelSize = new Vector2(380f, 176f);
        public Color PanelColor = new Color(0.04f, 0.045f, 0.055f, 0.95f);
        public Vector2 TitlePosition = new Vector2(24f, -20f);
        public Vector2 TitleSize = new Vector2(332f, 30f);
        [Min(1)] public int TitleFontSize = 20;
        public Vector2 BodyPosition = new Vector2(24f, -58f);
        public Vector2 BodySize = new Vector2(332f, 42f);
        [Min(1)] public int BodyFontSize = 14;
        public Vector2 ButtonPosition = new Vector2(0f, 24f);
        public Vector2 ButtonSize = new Vector2(178f, 40f);
        public Color ButtonColor = new Color(0.25f, 0.7f, 1f, 1f);
        [Min(1)] public int ButtonFontSize = 15;

        public void EnsureDefaults()
        {
            TitleFontSize = Mathf.Max(1, TitleFontSize);
            BodyFontSize = Mathf.Max(1, BodyFontSize);
            ButtonFontSize = Mathf.Max(1, ButtonFontSize);
        }

        public string FormatTitle(int _StageNumber)
        {
            return MvpTextFormatter.Format(TitleFormat, _StageNumber);
        }
    }

    [Serializable]
    public sealed class MvpStageRuntimeSettings
    {
        [Min(1)] public int StartStageNumber = 1;
        [Min(0f)] public float MonsterHpScalePerStage = 0.12f;
        [Min(0f)] public float MonsterAttackScalePerStage = 0.12f;
        [Min(0f)] public float MonsterDefenseScalePerStage = 0.06f;
        [Min(0f)] public float RewardScalePerStage = 0.12f;
        [Min(0f)] public float SpawnDelayAfterKill = 0.65f;
        [Min(0f)] public float StageAdvanceDelay = 0.85f;
        public string MonsterNameFormat = "{0} S{1}";
        public string StageStartedLogFormat = "Stage {0} started.";
        public string StageRestartedLogFormat = "Stage {0} restarted.";
        public string StageClearedLogFormat = "Stage {0} cleared.";
        public string MonsterDefeatedLogFormat = "{0} defeated. +{1} gold, +{2} exp.";
        public string PlayerDefeatedLog = "Player defeated. Restart the stage when ready.";
        public string DamageLogFormat = "{0} -> {1}: {2}{3}";
        public string CriticalSuffix = " CRIT";

        public string FormatMonsterName(string _BaseName, int _StageNumber)
        {
            return MvpTextFormatter.Format(MonsterNameFormat, _BaseName, _StageNumber);
        }

        public string FormatStageStarted(int _StageNumber)
        {
            return MvpTextFormatter.Format(StageStartedLogFormat, _StageNumber);
        }

        public string FormatStageRestarted(int _StageNumber)
        {
            return MvpTextFormatter.Format(StageRestartedLogFormat, _StageNumber);
        }

        public string FormatStageCleared(int _StageNumber)
        {
            return MvpTextFormatter.Format(StageClearedLogFormat, _StageNumber);
        }

        public string FormatMonsterDefeated(string _MonsterName, int _Gold, int _Exp)
        {
            return MvpTextFormatter.Format(MonsterDefeatedLogFormat, _MonsterName, _Gold, _Exp);
        }

        public string FormatDamage(string _Attacker, string _Target, string _Damage, bool _IsCritical)
        {
            return MvpTextFormatter.Format(DamageLogFormat, _Attacker, _Target, _Damage, _IsCritical ? CriticalSuffix : string.Empty);
        }

        public float GetHpMultiplier(int _StageNumber)
        {
            return 1f + Mathf.Max(0, _StageNumber - 1) * MonsterHpScalePerStage;
        }

        public float GetAttackMultiplier(int _StageNumber)
        {
            return 1f + Mathf.Max(0, _StageNumber - 1) * MonsterAttackScalePerStage;
        }

        public float GetDefenseMultiplier(int _StageNumber)
        {
            return 1f + Mathf.Max(0, _StageNumber - 1) * MonsterDefenseScalePerStage;
        }

        public float GetRewardMultiplier(int _StageNumber)
        {
            return 1f + Mathf.Max(0, _StageNumber - 1) * RewardScalePerStage;
        }
    }

    public static class MvpTextFormatter
    {
        public static string Format(string _Format, params object[] _Args)
        {
            if (string.IsNullOrWhiteSpace(_Format))
            {
                return string.Empty;
            }

            try
            {
                return string.Format(_Format, _Args);
            }
            catch (FormatException)
            {
                return _Format;
            }
        }
    }
}
