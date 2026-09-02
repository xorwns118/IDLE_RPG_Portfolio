using IdleRPG.Domain;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Combat;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
using IdleRPG.Runtime.Stages;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace IdleRPG.Runtime.Bootstrap
{
    public sealed class MvpSceneController : MonoBehaviour
    {
        [Header("Designer Editable Settings")]
        [SerializeField] private MvpGameContentSettings GameContent = MvpGameContentSettings.CreateDefault();
        [SerializeField] private MvpSceneDesignerSettings DesignerSettings = MvpSceneDesignerSettings.CreateDefault();

        [HideInInspector, SerializeField, FormerlySerializedAs("PlayerSlot"), FormerlySerializedAs("playerSlot")] private Transform PlayerStartPoint;
        [HideInInspector, SerializeField, FormerlySerializedAs("monsterSpawnPoint")] private Transform MonsterSpawnPoint;
        [HideInInspector, SerializeField, FormerlySerializedAs("hudCanvas")] private Canvas HudCanvas;
        [HideInInspector, SerializeField, FormerlySerializedAs("stageText")] private Text StageText;
        [HideInInspector, SerializeField, FormerlySerializedAs("resourceText")] private Text ResourceText;
        [HideInInspector, SerializeField, FormerlySerializedAs("playerText")] private Text PlayerText;
        [HideInInspector, SerializeField, FormerlySerializedAs("enemyText")] private Text EnemyText;
        [HideInInspector, SerializeField, FormerlySerializedAs("logText")] private Text LogText;
        [HideInInspector, SerializeField, FormerlySerializedAs("playerHpFill")] private Image PlayerHpFill;
        [HideInInspector, SerializeField, FormerlySerializedAs("enemyHpFill")] private Image EnemyHpFill;
        [HideInInspector, SerializeField] private GameObject RestartPanel;
        [HideInInspector, SerializeField] private Text RestartTitleText;
        [HideInInspector, SerializeField] private Text RestartBodyText;
        [HideInInspector, SerializeField] private Button RestartButton;

        private Sprite PreviewSprite;
        private Sprite TileSprite;
        private TileMapLayout TileMap;
        private StageController RuntimeStageController;
        private BattleContext RuntimeBattleContext;
        private StageSceneFlowController RuntimeSceneFlowController;
        private FieldEncounterController RuntimeFieldEncounterController;
        private TurnBasedAutoBattleController RuntimeTurnBattleController;
        private bool BattleRuntimeInitialized;
        private bool RuntimeStarted;

        public MvpSceneDesignerSettings DesignerEditableSettings => DesignerSettings;
        public TileMapLayout CurrentTileMap => TileMap;

        private void OnEnable()
        {
            EnsureDesignerSettings();

            if (Application.isPlaying)
                EnsureSceneLayout();
        }

        private void OnValidate()
        {
            EnsureDesignerSettings();
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                EnsureDesignerSettings();
                EnsureSceneLayout();
                StartRuntime();
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
                RefreshHud();
        }

        [ContextMenu("Rebuild MVP Scene Layout")]
        public void RebuildSceneLayout()
        {
            EnsureDesignerSettings();
            EnsureSceneLayout();
        }

        private void StartRuntime()
        {
            if (RuntimeStarted)
                return;

            RuntimeStarted = true;
            RuntimeBattleContext = GetOrAdd<BattleContext>(gameObject);
            RuntimeStageController = GetOrAdd<StageController>(gameObject);
            RuntimeSceneFlowController = GetOrAdd<StageSceneFlowController>(gameObject);
            RuntimeSceneFlowController.Initialize(DesignerSettings.SceneFlow);
            RuntimeSceneFlowController.FlowChanged -= HandleStageFlowChanged;
            RuntimeSceneFlowController.FlowChanged += HandleStageFlowChanged;

            RuntimeBattleContext.SetTileMap(TileMap);
            RuntimeBattleContext.ConfigureTargeting(DesignerSettings.Actors.Targeting);

            if (ShouldStartBattleRuntime())
            {
                int requestedStageNumber = RuntimeSceneFlowController.HasBattleStageRequest
                    ? RuntimeSceneFlowController.RequestedStageNumber
                    : 0;
                InitializeBattleRuntime(requestedStageNumber);
            }
            else
            {
                RuntimeStageController.ClearRuntime();
                BattleRuntimeInitialized = false;
                ConfigureCombatLoopRuntime();
            }

            RuntimeSceneFlowController.ClearBattleStageRequest();
            ConfigureFieldEncounterController();


            BindRestartButton();
            RefreshHud();
        }

        private bool ShouldStartBattleRuntime()
        {
            return RuntimeSceneFlowController.CurrentMode == StageFlowMode.Battle || RuntimeSceneFlowController.HasBattleStageRequest;
        }

        private void InitializeBattleRuntime(int _StartStageNumberOverride)
        {
            ActorFactory factory = new ActorFactory(EnsureSprite(), DesignerSettings.Actors, DesignerSettings.CombatLoop.Mode);
            RuntimeStageController.Initialize(new StageController.RuntimeSetup
            {
                Database = GameContent.CreateDatabase(),
                Context = RuntimeBattleContext,
                Factory = factory,
                MonsterSpawnPoint = MonsterSpawnPoint,
                RuntimeSettings = DesignerSettings.Stage,
                ContentSettings = GameContent,
                ActorSettings = DesignerSettings.Actors,
                PlayerStartPosition = GetPlayerStartPosition(),
                SpawnSettings = DesignerSettings.Spawn,
                TileMap = TileMap,
                StartStageNumberOverride = _StartStageNumberOverride
            });

            BattleRuntimeInitialized = true;
            ConfigureCombatLoopRuntime();
        }

        private void ConfigureCombatLoopRuntime()
        {
            bool battleLoopActive = IsBattleRuntimeActive();
            bool realtimeActive = battleLoopActive && DesignerSettings.CombatLoop.Mode == CombatLoopMode.Realtime;
            bool turnBasedActive = battleLoopActive && DesignerSettings.CombatLoop.Mode == CombatLoopMode.TurnBased;

            if (RuntimeStageController != null)
                RuntimeStageController.SetRealtimeCombatActive(realtimeActive);

            RuntimeTurnBattleController = GetOrAdd<TurnBasedAutoBattleController>(gameObject);
            RuntimeTurnBattleController.Initialize(
                RuntimeBattleContext,
                DesignerSettings.TurnCombat,
                turnBasedActive);
        }

        private bool IsBattleRuntimeActive()
        {
            return BattleRuntimeInitialized
                && RuntimeSceneFlowController != null
                && RuntimeSceneFlowController.CurrentMode == StageFlowMode.Battle
                && RuntimeStageController != null
                && RuntimeStageController.HasActiveStage;
        }

        private void ConfigureFieldEncounterController()
        {
            if (!DesignerSettings.FieldEncounter.Enabled)
            {
                if (RuntimeFieldEncounterController != null)
                    RuntimeFieldEncounterController.SetRuntimeActive(false);

                return;
            }

            RuntimeFieldEncounterController = GetOrAdd<FieldEncounterController>(gameObject);
            RuntimeFieldEncounterController.Initialize(
                DesignerSettings.FieldEncounter,
                ResolveFieldEncounterPlayer(),
                MonsterSpawnPoint,
                RuntimeSceneFlowController);
            RuntimeFieldEncounterController.SetRuntimeActive(IsFieldRuntimeActive());
        }

        private bool IsFieldRuntimeActive()
        {
            return RuntimeSceneFlowController != null && RuntimeSceneFlowController.CurrentMode == StageFlowMode.Field;
        }

        private Transform ResolveFieldEncounterPlayer()
        {
            if (RuntimeStageController != null && RuntimeStageController.Player != null)
                return RuntimeStageController.Player.transform;

            return PlayerStartPoint;
        }

        private void HandleStageFlowChanged(StageFlowMode _Mode, int _StageNumber)
        {
            if (!Application.isPlaying || RuntimeStageController == null || DesignerSettings.SceneFlow.LoadConfiguredScenes)
                return;

            if (_Mode == StageFlowMode.Field)
            {
                RuntimeStageController.ClearRuntime();
                BattleRuntimeInitialized = false;
                ConfigureCombatLoopRuntime();
                ConfigureFieldEncounterController();
                RefreshHud();
                return;
            }

            if (_Mode == StageFlowMode.Battle)
            {
                if (!BattleRuntimeInitialized || !RuntimeStageController.HasActiveStage)
                    InitializeBattleRuntime(_StageNumber);
                else
                    RuntimeStageController.StartStage(_StageNumber);

                ConfigureCombatLoopRuntime();
                ConfigureFieldEncounterController();
                RuntimeSceneFlowController.ClearBattleStageRequest();
                RefreshHud();
            }
        }

        private Vector3 GetPlayerStartPosition()
        {
            return PlayerStartPoint != null ? PlayerStartPoint.position : DesignerSettings.World.PlayerStartPosition;
        }

        private void EnsureDesignerSettings()
        {
            if (GameContent == null)
                GameContent = MvpGameContentSettings.CreateDefault();

            if (DesignerSettings == null)
                DesignerSettings = MvpSceneDesignerSettings.CreateDefault();

            GameContent.EnsureDefaults();
            DesignerSettings.EnsureDefaults();
        }

        private void EnsureSceneLayout()
        {
            EnsureWorldLayout();
            EnsureCamera();
            EnsureHudLayout();
            EnsureEventSystem();
            RefreshHud();
        }

        private void EnsureCamera()
        {
            MvpCameraSettings cameraSettings = DesignerSettings.Camera;
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = GetCameraOrthographicSize(camera, cameraSettings);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraSettings.BackgroundColor;
            camera.transform.position = GetCameraPosition(cameraSettings);
        }

        private Vector3 GetCameraPosition(MvpCameraSettings _CameraSettings)
        {
            if (_CameraSettings.AutoFitTileMap && TileMap != null && TileMap.IsEnabled)
            {
                Bounds mapBounds = TileMap.GetWorldBounds();
                return new Vector3(mapBounds.center.x, mapBounds.center.y, _CameraSettings.Position.z);
            }

            return _CameraSettings.Position;
        }

        private float GetCameraOrthographicSize(Camera _Camera, MvpCameraSettings _CameraSettings)
        {
            if (_CameraSettings.AutoFitTileMap && TileMap != null && TileMap.IsEnabled)
                return _CameraSettings.CalculateTileMapOrthographicSize(TileMap.Settings, _Camera.aspect);

            return _CameraSettings.OrthographicSize;
        }

        private void EnsureWorldLayout()
        {
            MvpWorldLayoutSettings worldSettings = DesignerSettings.World;
            Transform world = FindOrCreateChild(transform, "World");

            if (worldSettings.TileMap.Enabled)
            {
                RemoveChildIfExists(world, "Combat Ground");
                Transform tileMapRoot = FindOrCreateChild(world, "Combat Tile Map");
                tileMapRoot.localPosition = Vector3.zero;
                tileMapRoot.localRotation = Quaternion.identity;
                tileMapRoot.localScale = Vector3.one;
                RemoveComponentIfExists<SpriteRenderer>(tileMapRoot.gameObject);

                TileMap = GetOrAdd<TileMapLayout>(tileMapRoot.gameObject);
                TileMap.Configure(worldSettings.TileMap);
                TileMap.RebuildVisuals(EnsureTileSprite());

                Vector3 playerLocalPosition = world.InverseTransformPoint(TileMap.CellToActorWorld(worldSettings.TileMap.PlayerStartCell));
                Vector3 monsterLocalPosition = world.InverseTransformPoint(TileMap.CellToActorWorld(worldSettings.TileMap.GetPrimaryMonsterSpawnCell()));
                PlayerStartPoint = FindOrCreateChild(world, "Player Start Point");
                ConfigureStartPoint(PlayerStartPoint, playerLocalPosition);

                MonsterSpawnPoint = FindOrCreateChild(world, "Monster Spawn Point");
                MonsterSpawnPoint.localPosition = monsterLocalPosition;
                MonsterSpawnPoint.localRotation = Quaternion.identity;
                MonsterSpawnPoint.localScale = Vector3.one;
            }
            else
            {
                RemoveChildIfExists(world, "Combat Tile Map");
                TileMap = null;

                Transform ground = FindOrCreateChild(world, "Combat Ground");
                ground.localPosition = worldSettings.GroundPosition;
                ground.localScale = worldSettings.GroundScale;
                SpriteRenderer groundRenderer = GetOrAdd<SpriteRenderer>(ground.gameObject);
                groundRenderer.sprite = EnsureSprite();
                groundRenderer.color = worldSettings.GroundColor;
                groundRenderer.sortingOrder = worldSettings.GroundSortingOrder;

                PlayerStartPoint = FindOrCreateChild(world, "Player Start Point");
                ConfigureStartPoint(PlayerStartPoint, worldSettings.PlayerStartPosition);

                MonsterSpawnPoint = FindOrCreateChild(world, "Monster Spawn Point");
                MonsterSpawnPoint.localPosition = worldSettings.MonsterSpawnPosition;
                MonsterSpawnPoint.localRotation = Quaternion.identity;
                MonsterSpawnPoint.localScale = Vector3.one;
            }

            RemoveChildIfExists(world, "Player Actor");
            RemoveChildIfExists(world, "Monster Actor");
            RemoveEditModeRuntimeActors();
        }

        private void ConfigureStartPoint(Transform _Marker, Vector3 _Position)
        {
            _Marker.localPosition = _Position;
            _Marker.localScale = Vector3.one;

            RemoveComponentIfExists<SpriteRenderer>(_Marker.gameObject);
            RemoveChildIfExists(_Marker, "HP Background");
            RemoveChildIfExists(_Marker, "HP Fill");
            RemoveChildIfExists(_Marker, "Name Label");
        }

        private void RemoveEditModeRuntimeActors()
        {
            if (Application.isPlaying)
                return;

            CombatActor[] actors = FindObjectsOfType<CombatActor>(true);
            foreach (CombatActor actor in actors)
            {
                if (actor == null || actor.gameObject.scene != gameObject.scene)
                    continue;

                DestroyImmediate(actor.gameObject);
            }
        }

        private void EnsureHudLayout()
        {
            MvpHudSettings hudSettings = DesignerSettings.Hud;
            RectTransform canvasTransform = FindOrCreateRectChild(transform, "MVP HUD Canvas");
            HudCanvas = GetOrAdd<Canvas>(canvasTransform.gameObject);
            HudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasTransform.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = hudSettings.ReferenceResolution;
            scaler.matchWidthOrHeight = hudSettings.MatchWidthOrHeight;

            GetOrAdd<GraphicRaycaster>(canvasTransform.gameObject);

            canvasTransform.localScale = Vector3.one;

            RectTransform panel = FindOrCreateRectChild(canvasTransform, "Status Panel");
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = hudSettings.StatusPanelPosition;
            panel.sizeDelta = hudSettings.StatusPanelSize;

            Image panelImage = GetOrAdd<Image>(panel.gameObject);
            panelImage.color = hudSettings.StatusPanelColor;

            Text titleText = EnsureText(panel, "Title Text", hudSettings.TitleText, TextAnchor.MiddleLeft, hudSettings.TextColor);
            titleText.text = hudSettings.Title;

            StageText = EnsureText(panel, "Stage Text", hudSettings.StageText, TextAnchor.MiddleLeft, hudSettings.TextColor);
            ResourceText = EnsureText(panel, "Resource Text", hudSettings.ResourceText, TextAnchor.MiddleLeft, hudSettings.TextColor);
            PlayerText = EnsureText(panel, "Player Text", hudSettings.PlayerText, TextAnchor.MiddleLeft, hudSettings.TextColor);
            EnemyText = EnsureText(panel, "Enemy Text", hudSettings.EnemyText, TextAnchor.MiddleLeft, hudSettings.TextColor);
            LogText = EnsureText(panel, "Log Text", hudSettings.LogText, TextAnchor.MiddleLeft, hudSettings.TextColor);

            PlayerHpFill = EnsureUiBar(panel, "Player HP Bar", hudSettings.PlayerHpBarPosition, DesignerSettings.Actors.PlayerColor);
            EnemyHpFill = EnsureUiBar(panel, "Enemy HP Bar", hudSettings.EnemyHpBarPosition, DesignerSettings.Actors.MonsterFallbackColor);

            EnsureRestartPanel(canvasTransform);
        }

        private void EnsureRestartPanel(Transform _CanvasTransform)
        {
            MvpRestartPanelSettings restartSettings = DesignerSettings.RestartPanel;
            RectTransform panel = FindOrCreateRectChild(_CanvasTransform, "Restart Panel");
            RestartPanel = panel.gameObject;
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = restartSettings.PanelSize;

            Image panelImage = GetOrAdd<Image>(panel.gameObject);
            panelImage.color = restartSettings.PanelColor;

            RestartTitleText = EnsureText(
                panel,
                "Title Text",
                restartSettings.TitlePosition,
                restartSettings.TitleSize,
                restartSettings.TitleFontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                DesignerSettings.Hud.TextColor);
            RestartTitleText.text = restartSettings.PreviewTitle;

            RestartBodyText = EnsureText(
                panel,
                "Body Text",
                restartSettings.BodyPosition,
                restartSettings.BodySize,
                restartSettings.BodyFontSize,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                DesignerSettings.Hud.TextColor);
            RestartBodyText.text = restartSettings.PreviewBody;

            RectTransform buttonRect = FindOrCreateRectChild(panel, "Restart Button");
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = restartSettings.ButtonPosition;
            buttonRect.sizeDelta = restartSettings.ButtonSize;

            Image buttonImage = GetOrAdd<Image>(buttonRect.gameObject);
            buttonImage.color = restartSettings.ButtonColor;

            RestartButton = GetOrAdd<Button>(buttonRect.gameObject);
            RestartButton.targetGraphic = buttonImage;
            RestartButton.interactable = true;

            Text buttonText = EnsureStretchText(buttonRect, "Text", restartSettings.ButtonFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, DesignerSettings.Hud.TextColor);
            buttonText.text = restartSettings.ButtonText;

            SetRestartPanelVisible(false);
        }

        private Text EnsureText(Transform _Parent, string _Name, MvpTextSlotSettings _Slot, TextAnchor _Alignment, Color _Color)
        {
            return EnsureText(_Parent, _Name, _Slot.Position, _Slot.Size, _Slot.FontSize, _Slot.Style, _Alignment, _Color);
        }

        private Text EnsureText(
            Transform _Parent,
            string _Name,
            Vector2 _AnchoredPosition,
            Vector2 _Size,
            int _FontSize,
            FontStyle _Style,
            TextAnchor _Alignment,
            Color _Color)
        {
            RectTransform rect = FindOrCreateRectChild(_Parent, _Name);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = _AnchoredPosition;
            rect.sizeDelta = _Size;

            Text text = GetOrAdd<Text>(rect.gameObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Max(1, _FontSize);
            text.fontStyle = _Style;
            text.color = _Color;
            text.alignment = _Alignment;
            text.raycastTarget = false;
            return text;
        }

        private Text EnsureStretchText(Transform _Parent, string _Name, int _FontSize, FontStyle _Style, TextAnchor _Alignment, Color _Color)
        {
            RectTransform rect = FindOrCreateRectChild(_Parent, _Name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = GetOrAdd<Text>(rect.gameObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Max(1, _FontSize);
            text.fontStyle = _Style;
            text.color = _Color;
            text.alignment = _Alignment;
            text.raycastTarget = false;
            return text;
        }

        private Image EnsureUiBar(Transform _Parent, string _Name, Vector2 _AnchoredPosition, Color _FillColor)
        {
            MvpHudSettings hudSettings = DesignerSettings.Hud;
            RectTransform group = FindOrCreateRectChild(_Parent, _Name);
            group.anchorMin = new Vector2(0f, 1f);
            group.anchorMax = new Vector2(0f, 1f);
            group.pivot = new Vector2(0f, 1f);
            group.anchoredPosition = _AnchoredPosition;
            group.sizeDelta = hudSettings.UiBarSize;

            RectTransform background = FindOrCreateRectChild(group, "Background");
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            Image backgroundImage = GetOrAdd<Image>(background.gameObject);
            backgroundImage.color = hudSettings.UiBarBackgroundColor;

            RectTransform fill = FindOrCreateRectChild(group, "Fill");
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            Image fillImage = GetOrAdd<Image>(fill.gameObject);
            fillImage.color = _FillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
            return fillImage;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void RefreshHud()
        {
            if (StageText == null || ResourceText == null || PlayerText == null || EnemyText == null || LogText == null)
                return;

            MvpHudSettings hudSettings = DesignerSettings.Hud;
            if (RuntimeStageController == null || !RuntimeStageController.HasActiveStage)
            {
                int startStage = DesignerSettings.Stage.StartStageNumber;
                StageText.text = hudSettings.FormatStage(startStage, 0, GameContent.GetRequiredKillsForStage(startStage));
                ResourceText.text = hudSettings.FormatResources(0, 0);
                PlayerText.text = hudSettings.FormatPlayer(GameContent.PlayerDisplayName);
                EnemyText.text = hudSettings.FormatEnemy(hudSettings.PreviewEnemyText);
                LogText.text = hudSettings.PlayPrompt;
                SetFill(PlayerHpFill, 1f);
                SetFill(EnemyHpFill, 1f);
                SetRestartPanelVisible(false);
                return;
            }

            bool playerDefeated = RuntimeStageController.IsPlayerDefeated;
            StageText.text = hudSettings.FormatStage(
                RuntimeStageController.CurrentStageNumber,
                RuntimeStageController.KillsInStage,
                RuntimeStageController.RequiredKills);
            ResourceText.text = hudSettings.FormatResources(RuntimeStageController.TotalGold, RuntimeStageController.TotalExp);
            PlayerText.text = hudSettings.FormatPlayer(FormatActor(RuntimeStageController.Player));
            EnemyText.text = hudSettings.FormatEnemy(FormatActor(RuntimeStageController.ActiveMonster));
            LogText.text = RuntimeStageController.LastLog;
            SetFill(PlayerHpFill, GetHpPercent(RuntimeStageController.Player));
            SetFill(EnemyHpFill, GetHpPercent(RuntimeStageController.ActiveMonster));
            SetRestartPanelVisible(playerDefeated);

            if (playerDefeated && RestartTitleText != null && RestartBodyText != null)
            {
                RestartTitleText.text = DesignerSettings.RestartPanel.FormatTitle(RuntimeStageController.CurrentStageNumber);
                RestartBodyText.text = DesignerSettings.RestartPanel.BodyText;
            }
        }

        private void BindRestartButton()
        {
            if (RestartButton == null)
                return;

            RestartButton.onClick.RemoveListener(HandleRestartClicked);
            RestartButton.onClick.AddListener(HandleRestartClicked);
        }

        private void HandleRestartClicked()
        {
            if (RuntimeStageController == null)
                return;

            RuntimeStageController.RestartCurrentStage();
            RefreshHud();
        }

        private void SetRestartPanelVisible(bool _Visible)
        {
            if (RestartPanel != null && RestartPanel.activeSelf != _Visible)
                RestartPanel.SetActive(_Visible);
        }

        private string FormatActor(CombatActor _Actor)
        {
            if (_Actor == null || _Actor.Model == null)
                return DesignerSettings.Hud.EmptyActorText;

            return DesignerSettings.Hud.FormatActor(
                _Actor.Model.DisplayName,
                _Actor.Model.CurrentHp.ToString("0"),
                _Actor.Model.Stats.MaxHp.ToString("0"),
                _Actor.Model.State);
        }

        private static float GetHpPercent(CombatActor _Actor)
        {
            if (_Actor == null || _Actor.Model == null)
                return 0f;

            return Mathf.Clamp01(_Actor.Model.CurrentHp / _Actor.Model.Stats.MaxHp);
        }

        private static void SetFill(Image _Image, float _Value)
        {
            if (_Image != null)
                _Image.fillAmount = Mathf.Clamp01(_Value);
        }

        private Sprite EnsureSprite()
        {
            if (PreviewSprite == null)
                PreviewSprite = GeneratedSpriteFactory.CreateUnitSprite();

            return PreviewSprite;
        }

        private Sprite EnsureTileSprite()
        {
            if (TileSprite == null)
                TileSprite = GeneratedSpriteFactory.CreateSquareTileSprite();

            return TileSprite;
        }

        private static Transform FindOrCreateChild(Transform _Parent, string _ChildName)
        {
            Transform child = _Parent.Find(_ChildName);
            if (child != null)
                return child;

            GameObject childObject = new GameObject(_ChildName);
            childObject.transform.SetParent(_Parent, false);
            return childObject.transform;
        }

        private static void RemoveChildIfExists(Transform _Parent, string _ChildName)
        {
            Transform child = _Parent.Find(_ChildName);
            if (child == null)
                return;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        private static void RemoveComponentIfExists<T>(GameObject _GameObject) where T : Component
        {
            T component = _GameObject.GetComponent<T>();
            if (component == null)
                return;

            if (Application.isPlaying)
                Destroy(component);
            else
                DestroyImmediate(component);
        }

        private static RectTransform FindOrCreateRectChild(Transform _Parent, string _ChildName)
        {
            Transform child = _Parent.Find(_ChildName);
            if (child != null)
            {
                RectTransform rect = child.GetComponent<RectTransform>();
                if (rect != null)
                    return rect;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            GameObject childObject = new GameObject(_ChildName, typeof(RectTransform));
            childObject.transform.SetParent(_Parent, false);
            return childObject.GetComponent<RectTransform>();
        }

        private static T GetOrAdd<T>(GameObject _GameObject) where T : Component
        {
            T component = _GameObject.GetComponent<T>();
            return component != null ? component : _GameObject.AddComponent<T>();
        }
    }
}
