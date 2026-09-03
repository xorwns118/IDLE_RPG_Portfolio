#if UNITY_EDITOR
using System;
using IdleRPG.Domain;
using IdleRPG.Runtime.Bootstrap;
using IdleRPG.Runtime.Configuration;
using IdleRPG.Runtime.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleRPG.Editor
{
    public sealed class TileMapEditorWindow : EditorWindow
    {
        private const float CellButtonWidth = 34f;
        private const float CellButtonHeight = 24f;
        private static readonly string[] TabLabels = { "Tile Map", "Tile Navigation" };

        [SerializeField] private MvpSceneController Controller;
        [SerializeField] private Vector2Int SelectedCell;
        [SerializeField] private TileVisualKind SelectedVisualKind = TileVisualKind.Ground;
        [SerializeField] private bool PaintVisualOnClick;
        [SerializeField] private TileMapEditorTab ActiveTab;
        private Vector2 ScrollPosition;

        private enum TileMapEditorTab
        {
            TileMap,
            TileNavigation
        }

        [MenuItem("Idle RPG/Maps/Tile Map Editor", priority = 230)]
        public static void Open()
        {
            TileMapEditorWindow window = GetWindow<TileMapEditorWindow>("Tile Map");
            window.minSize = new Vector2(430f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            if (Controller == null)
                FindActiveController();
        }

        private void OnHierarchyChange()
        {
            Repaint();
        }

        private void OnGUI()
        {
            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);
            DrawControllerPicker();

            if (Controller == null)
            {
                DrawMissingController();
                EditorGUILayout.EndScrollView();
                return;
            }

            MvpSceneDesignerSettings designerSettings = Controller.DesignerEditableSettings;
            if (designerSettings == null || designerSettings.World == null || designerSettings.World.TileMap == null)
            {
                EditorGUILayout.HelpBox("Tile Map settings are missing on the selected controller.", MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            designerSettings.EnsureDefaults();
            MvpTileMapSettings tileMapSettings = designerSettings.World.TileMap;
            SelectedCell = tileMapSettings.ClampCell(SelectedCell);

            DrawEditorTabs();
            if (ActiveTab == TileMapEditorTab.TileNavigation)
            {
                DrawNavigationSettings(designerSettings.TileNavigation);
                DrawSceneActions();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawShapeSettings(tileMapSettings);
            DrawSpritePalette(tileMapSettings);
            DrawCellGrid(tileMapSettings);
            DrawSelectedCellTools(tileMapSettings);
            DrawMonsterSpawnList(tileMapSettings);
            DrawVisualSettings(tileMapSettings);
            DrawOverrideList(tileMapSettings);
            DrawSceneActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawControllerPicker()
        {
            EditorGUILayout.LabelField("Tile Map Editor", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            Controller = (MvpSceneController)EditorGUILayout.ObjectField("Scene Controller", Controller, typeof(MvpSceneController), true);
            if (GUILayout.Button("Find Active", GUILayout.Width(90f)))
                FindActiveController();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawEditorTabs()
        {
            ActiveTab = (TileMapEditorTab)GUILayout.Toolbar((int)ActiveTab, TabLabels);
            EditorGUILayout.Space(8f);
        }

        private void DrawMissingController()
        {
            EditorGUILayout.HelpBox("No MVP Scene Controller was found in the active scene.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Create MVP Scene Controller", GUILayout.Height(30f)))
                    CreateController();
            }
        }

        private void DrawShapeSettings(MvpTileMapSettings _Settings)
        {
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);

            bool enabled = _Settings.Enabled;
            int columns = _Settings.Columns;
            int rows = _Settings.Rows;
            float cellSize = Mathf.Max(_Settings.CellSize.x, _Settings.CellSize.y);
            Vector3 origin = _Settings.Origin;
            Vector3 actorAnchorOffset = _Settings.ActorAnchorOffset;
            Vector2Int playerStartCell = _Settings.PlayerStartCell;
            Vector2Int primaryMonsterSpawnCell = _Settings.GetPrimaryMonsterSpawnCell();

            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.Toggle("Enabled", enabled);
            columns = EditorGUILayout.IntSlider("Columns", columns, 1, 16);
            rows = EditorGUILayout.IntSlider("Rows", rows, 1, 12);
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
            origin = EditorGUILayout.Vector3Field("Origin", origin);
            actorAnchorOffset = EditorGUILayout.Vector3Field("Actor Anchor Offset", actorAnchorOffset);
            playerStartCell = EditorGUILayout.Vector2IntField("Player Start Cell", playerStartCell);
            primaryMonsterSpawnCell = EditorGUILayout.Vector2IntField("Primary Monster Spawn Cell", primaryMonsterSpawnCell);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyChange("Edit Tile Map Shape", _TileMapSettings =>
                {
                    _TileMapSettings.Enabled = enabled;
                    _TileMapSettings.Columns = columns;
                    _TileMapSettings.Rows = rows;
                    _TileMapSettings.CellSize = new Vector2(cellSize, cellSize);
                    _TileMapSettings.Origin = origin;
                    _TileMapSettings.ActorAnchorOffset = actorAnchorOffset;
                    _TileMapSettings.PlayerStartCell = playerStartCell;
                    _TileMapSettings.SetPrimaryMonsterSpawnCell(primaryMonsterSpawnCell);
                });
            }

            EditorGUILayout.Space(10f);
        }

        private void DrawNavigationSettings(MvpTileNavigationSettings _Settings)
        {
            if (_Settings == null)
                return;

            EditorGUILayout.LabelField("Tile Navigation", EditorStyles.boldLabel);

            bool useTileMovement = _Settings.UseTileMovement;
            bool useWaypointCompression = _Settings.UseWaypointCompression;
            bool allowDiagonalMovement = _Settings.AllowDiagonalMovement;

            EditorGUI.BeginChangeCheck();
            useTileMovement = EditorGUILayout.Toggle("Use Tile Movement", useTileMovement);
            using (new EditorGUI.DisabledScope(!useTileMovement))
            {
                useWaypointCompression = EditorGUILayout.Toggle("Use Waypoint Compression", useWaypointCompression);
                using (new EditorGUI.DisabledScope(!useWaypointCompression))
                {
                    allowDiagonalMovement = EditorGUILayout.Toggle("Allow Diagonal Movement", allowDiagonalMovement);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                ApplyNavigationChange("Edit Tile Navigation", _NavigationSettings =>
                {
                    _NavigationSettings.UseTileMovement = useTileMovement;
                    _NavigationSettings.UseWaypointCompression = useWaypointCompression;
                    _NavigationSettings.AllowDiagonalMovement = allowDiagonalMovement;
                });
            }

            EditorGUILayout.HelpBox(
                "A* builds a walkable tile path. Compression and diagonal movement control the smoothed realtime waypoint.",
                MessageType.Info);
            EditorGUILayout.Space(10f);
        }

        private void DrawCellGrid(MvpTileMapSettings _Settings)
        {
            EditorGUILayout.LabelField("Cells", EditorStyles.boldLabel);

            GUIStyle cellStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = CellButtonWidth,
                fixedHeight = CellButtonHeight
            };

            for (int y = _Settings.Rows - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < _Settings.Columns; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    Color previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = GetEditorCellColor(_Settings, cell);

                    if (GUILayout.Button(GetCellLabel(_Settings, cell), cellStyle))
                    {
                        SelectedCell = cell;
                        GUI.FocusControl(null);
                        if (PaintVisualOnClick)
                            ApplyChange("Paint Tile Visual", _TileMapSettings => _TileMapSettings.PaintTileVisual(cell, SelectedVisualKind));
                    }

                    GUI.backgroundColor = previousColor;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Selected Cell", SelectedCell.x + ", " + SelectedCell.y);
            EditorGUILayout.Space(10f);
        }

        private void DrawSpritePalette(MvpTileMapSettings _Settings)
        {
            EditorGUILayout.LabelField("Tile Sprite Palette", EditorStyles.boldLabel);

            TileVisualKind defaultVisualKind = _Settings.DefaultVisualKind;
            SelectedVisualKind = (TileVisualKind)EditorGUILayout.EnumPopup("Brush", SelectedVisualKind);
            PaintVisualOnClick = EditorGUILayout.Toggle("Paint On Click", PaintVisualOnClick);

            EditorGUI.BeginChangeCheck();
            defaultVisualKind = (TileVisualKind)EditorGUILayout.EnumPopup("Default Visual", defaultVisualKind);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyChange("Edit Default Tile Visual", _TileMapSettings =>
                {
                    _TileMapSettings.DefaultVisualKind = defaultVisualKind;
                });
            }

            EditorGUILayout.HelpBox("Assign sliced PNG Sprite assets to each visual type, then paint cells with the selected Brush.", MessageType.Info);

            Array visualKinds = Enum.GetValues(typeof(TileVisualKind));
            foreach (object visualKindValue in visualKinds)
            {
                TileVisualKind visualKind = (TileVisualKind)visualKindValue;
                MvpTileSpriteSettings spriteSettings = _Settings.GetSpriteSettings(visualKind);
                if (spriteSettings == null)
                    continue;

                DrawSpritePaletteEntry(visualKind, spriteSettings);
            }

            EditorGUILayout.Space(10f);
        }

        private void DrawSpritePaletteEntry(TileVisualKind _VisualKind, MvpTileSpriteSettings _SpriteSettings)
        {
            Sprite sprite = _SpriteSettings.Sprite;
            Color tint = _SpriteSettings.Tint;
            Vector2 drawSizeInCells = _SpriteSettings.DrawSizeInCells;
            Vector3 localOffset = _SpriteSettings.LocalOffset;
            int sortingOffset = _SpriteSettings.SortingOffset;
            TileKind defaultKind = _SpriteSettings.DefaultKind;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(_VisualKind.ToString(), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", sprite, typeof(Sprite), false);
            tint = EditorGUILayout.ColorField("Tint", tint);
            drawSizeInCells = EditorGUILayout.Vector2Field("Draw Size In Cells", drawSizeInCells);
            localOffset = EditorGUILayout.Vector3Field("Local Offset", localOffset);
            sortingOffset = EditorGUILayout.IntField("Sorting Offset", sortingOffset);
            defaultKind = (TileKind)EditorGUILayout.EnumPopup("Default Kind", defaultKind);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyChange("Edit Tile Sprite Palette", _TileMapSettings =>
                {
                    MvpTileSpriteSettings targetSettings = _TileMapSettings.GetSpriteSettings(_VisualKind);
                    if (targetSettings == null)
                        return;

                    targetSettings.Sprite = sprite;
                    targetSettings.Tint = tint;
                    targetSettings.DrawSizeInCells = drawSizeInCells;
                    targetSettings.LocalOffset = localOffset;
                    targetSettings.SortingOffset = sortingOffset;
                    targetSettings.DefaultKind = defaultKind;
                });
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedCellTools(MvpTileMapSettings _Settings)
        {
            bool isPlayerStart = SelectedCell == _Settings.PlayerStartCell;
            bool isMonsterSpawn = _Settings.IsMonsterSpawnCell(SelectedCell);
            bool canUseMonsterSpawn = _Settings.CanUseMonsterSpawnCell(SelectedCell);
            bool isReservedCell = isPlayerStart || isMonsterSpawn;

            EditorGUILayout.LabelField("Selected Cell Tools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Kind", _Settings.GetTileKind(SelectedCell).ToString());
            EditorGUILayout.LabelField("Visual", _Settings.GetTileVisualKind(SelectedCell).ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Player Start"))
                ApplyChange("Set Player Start Cell", _TileMapSettings => _TileMapSettings.PlayerStartCell = SelectedCell);

            using (new EditorGUI.DisabledScope(!canUseMonsterSpawn))
            {
                if (GUILayout.Button("Set Primary Monster Spawn"))
                    ApplyChange("Set Primary Monster Spawn Cell", _TileMapSettings => _TileMapSettings.SetPrimaryMonsterSpawnCell(SelectedCell));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(isMonsterSpawn || !canUseMonsterSpawn))
            {
                if (GUILayout.Button("Add Monster Spawn"))
                    ApplyChange("Add Monster Spawn Cell", _TileMapSettings => _TileMapSettings.AddMonsterSpawnCell(SelectedCell));
            }

            using (new EditorGUI.DisabledScope(!isMonsterSpawn || _Settings.MonsterSpawnCellCount <= 1))
            {
                if (GUILayout.Button("Remove Monster Spawn"))
                    ApplyChange("Remove Monster Spawn Cell", _TileMapSettings => _TileMapSettings.RemoveMonsterSpawnCell(SelectedCell));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Paint Brush"))
                ApplyChange("Paint Tile Visual", _TileMapSettings => _TileMapSettings.PaintTileVisual(SelectedCell, SelectedVisualKind));

            using (new EditorGUI.DisabledScope(isReservedCell))
            {
                if (GUILayout.Button("Block Selected"))
                    ApplyChange("Block Tile Cell", _TileMapSettings => _TileMapSettings.SetTileKind(SelectedCell, TileKind.Blocked));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Walkable"))
                ApplyChange("Set Walkable Tile Cell", _TileMapSettings => _TileMapSettings.SetTileKind(SelectedCell, TileKind.Walkable));

            if (GUILayout.Button("Clear Selected"))
                ApplyChange("Clear Tile Cell", _TileMapSettings => _TileMapSettings.SetCell(SelectedCell, TileKind.Walkable, _TileMapSettings.DefaultVisualKind));
            EditorGUILayout.EndHorizontal();

            if (isPlayerStart)
                EditorGUILayout.HelpBox("Player Start cells stay walkable.", MessageType.Info);

            if (!canUseMonsterSpawn)
                EditorGUILayout.HelpBox("Monster Spawn cells can only be placed on walkable, non-player-start cells.", MessageType.Info);

            EditorGUILayout.Space(10f);
        }

        private void DrawMonsterSpawnList(MvpTileMapSettings _Settings)
        {
            EditorGUILayout.LabelField("Monster Spawn Cells", EditorStyles.boldLabel);

            Vector2Int[] spawnCells = _Settings.GetMonsterSpawnCells();
            for (int i = 0; i < spawnCells.Length; i++)
            {
                Vector2Int cell = spawnCells[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(i == 0 ? "Primary" : "Spawn " + (i + 1), GUILayout.Width(72f));
                EditorGUILayout.LabelField(cell.x + ", " + cell.y, GUILayout.Width(62f));
                if (GUILayout.Button("Select"))
                {
                    SelectedCell = cell;
                    GUI.FocusControl(null);
                }

                using (new EditorGUI.DisabledScope(spawnCells.Length <= 1))
                {
                    if (GUILayout.Button("Remove"))
                    {
                        ApplyChange("Remove Monster Spawn Cell", _TileMapSettings => _TileMapSettings.RemoveMonsterSpawnCell(cell));
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10f);
        }

        private void DrawVisualSettings(MvpTileMapSettings _Settings)
        {
            EditorGUILayout.LabelField("Fallback Visuals", EditorStyles.boldLabel);

            Color primaryTileColor = _Settings.PrimaryTileColor;
            Color alternateTileColor = _Settings.AlternateTileColor;
            Color playerStartTileColor = _Settings.PlayerStartTileColor;
            Color monsterSpawnTileColor = _Settings.MonsterSpawnTileColor;
            Color blockedTileColor = _Settings.BlockedTileColor;
            int tileSortingOrderBase = _Settings.TileSortingOrderBase;
            int tileSortingOrderStep = _Settings.TileSortingOrderStep;
            int actorSortingOrderBase = _Settings.ActorSortingOrderBase;
            int actorSortingOrderStep = _Settings.ActorSortingOrderStep;
            int overlaySortingOffset = _Settings.OverlaySortingOffset;

            EditorGUI.BeginChangeCheck();
            primaryTileColor = EditorGUILayout.ColorField("Primary Tile", primaryTileColor);
            alternateTileColor = EditorGUILayout.ColorField("Alternate Tile", alternateTileColor);
            playerStartTileColor = EditorGUILayout.ColorField("Player Start Tile", playerStartTileColor);
            monsterSpawnTileColor = EditorGUILayout.ColorField("Monster Spawn Tile", monsterSpawnTileColor);
            blockedTileColor = EditorGUILayout.ColorField("Blocked Tile", blockedTileColor);
            tileSortingOrderBase = EditorGUILayout.IntField("Tile Sorting Base", tileSortingOrderBase);
            tileSortingOrderStep = EditorGUILayout.IntField("Tile Sorting Step", tileSortingOrderStep);
            actorSortingOrderBase = EditorGUILayout.IntField("Actor Sorting Base", actorSortingOrderBase);
            actorSortingOrderStep = EditorGUILayout.IntField("Actor Sorting Step", actorSortingOrderStep);
            overlaySortingOffset = EditorGUILayout.IntField("Overlay Sorting Offset", overlaySortingOffset);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyChange("Edit Tile Map Visuals", _TileMapSettings =>
                {
                    _TileMapSettings.PrimaryTileColor = primaryTileColor;
                    _TileMapSettings.AlternateTileColor = alternateTileColor;
                    _TileMapSettings.PlayerStartTileColor = playerStartTileColor;
                    _TileMapSettings.MonsterSpawnTileColor = monsterSpawnTileColor;
                    _TileMapSettings.BlockedTileColor = blockedTileColor;
                    _TileMapSettings.TileSortingOrderBase = tileSortingOrderBase;
                    _TileMapSettings.TileSortingOrderStep = tileSortingOrderStep;
                    _TileMapSettings.ActorSortingOrderBase = actorSortingOrderBase;
                    _TileMapSettings.ActorSortingOrderStep = actorSortingOrderStep;
                    _TileMapSettings.OverlaySortingOffset = overlaySortingOffset;
                });
            }

            EditorGUILayout.Space(10f);
        }

        private void DrawOverrideList(MvpTileMapSettings _Settings)
        {
            EditorGUILayout.LabelField("Cell Overrides", EditorStyles.boldLabel);

            if (_Settings.CellOverrides == null || _Settings.CellOverrides.Length == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            for (int i = 0; i < _Settings.CellOverrides.Length; i++)
            {
                MvpTileCellSettings cellSettings = _Settings.CellOverrides[i];
                if (cellSettings == null)
                    continue;

                Vector2Int cell = cellSettings.Cell;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(cell.x + ", " + cell.y, GUILayout.Width(62f));
                EditorGUILayout.LabelField(cellSettings.VisualKind.ToString(), GUILayout.Width(82f));
                EditorGUILayout.LabelField(cellSettings.Kind.ToString(), GUILayout.Width(74f));
                if (GUILayout.Button("Select"))
                {
                    SelectedCell = cell;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Clear"))
                {
                    ApplyChange("Clear Tile Cell Override", _TileMapSettings => _TileMapSettings.RemoveTileOverride(cell));
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Clear All Cell Overrides"))
                ApplyChange("Clear All Tile Cell Overrides", _TileMapSettings => _TileMapSettings.CellOverrides = Array.Empty<MvpTileCellSettings>());
        }

        private void DrawSceneActions()
        {
            EditorGUILayout.Space(14f);
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild Scene Tile Map", GUILayout.Height(30f)))
                RebuildScene();

            if (GUILayout.Button("Select Tile Map", GUILayout.Height(30f)))
                SelectSceneTileMap();

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyChange(string _UndoName, Action<MvpTileMapSettings> _Edit)
        {
            if (Controller == null || _Edit == null)
                return;

            Undo.RecordObject(Controller, _UndoName);
            MvpTileMapSettings settings = Controller.DesignerEditableSettings.World.TileMap;
            _Edit(settings);
            settings.EnsureDefaults();
            SelectedCell = settings.ClampCell(SelectedCell);
            RebuildScene();
        }

        private void ApplyNavigationChange(string _UndoName, Action<MvpTileNavigationSettings> _Edit)
        {
            if (Controller == null || _Edit == null)
                return;

            Undo.RecordObject(Controller, _UndoName);
            MvpSceneDesignerSettings designerSettings = Controller.DesignerEditableSettings;
            designerSettings.EnsureDefaults();
            _Edit(designerSettings.TileNavigation);
            designerSettings.TileNavigation.EnsureDefaults();
            RebuildScene();
        }

        private void RebuildScene()
        {
            if (Controller == null)
                return;

            Controller.RebuildSceneLayout();
            EditorUtility.SetDirty(Controller);
            EditorUtility.SetDirty(Controller.gameObject);

            TileMapLayout tileMap = ResolveSceneTileMap();
            if (tileMap != null)
            {
                EditorUtility.SetDirty(tileMap);
                EditorUtility.SetDirty(tileMap.gameObject);
            }

            Scene scene = Controller.gameObject.scene;
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }

        private void SelectSceneTileMap()
        {
            TileMapLayout tileMap = ResolveSceneTileMap();
            if (tileMap == null)
            {
                RebuildScene();
                tileMap = ResolveSceneTileMap();
            }

            if (tileMap != null)
            {
                Selection.activeGameObject = tileMap.gameObject;
                EditorGUIUtility.PingObject(tileMap.gameObject);
            }
        }

        private TileMapLayout ResolveSceneTileMap()
        {
            if (Controller == null)
                return null;

            if (Controller.CurrentTileMap != null)
                return Controller.CurrentTileMap;

            Transform tileMapTransform = Controller.transform.Find("World/Combat Tile Map");
            return tileMapTransform != null ? tileMapTransform.GetComponent<TileMapLayout>() : null;
        }

        private void FindActiveController()
        {
            Controller = UnityEngine.Object.FindObjectOfType<MvpSceneController>(true);
            Repaint();
        }

        private void CreateController()
        {
            GameObject controllerObject = new GameObject("MVP Scene Controller");
            Controller = controllerObject.AddComponent<MvpSceneController>();
            Undo.RegisterCreatedObjectUndo(controllerObject, "Create MVP Scene Controller");

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(controllerObject, activeScene);
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            RebuildScene();
        }

        private static string GetCellLabel(MvpTileMapSettings _Settings, Vector2Int _Cell)
        {
            bool isPlayerStart = _Cell == _Settings.PlayerStartCell;
            bool isMonsterSpawn = _Settings.IsMonsterSpawnCell(_Cell);
            if (isPlayerStart && isMonsterSpawn)
                return "PM";

            if (isPlayerStart)
                return "P";

            if (isMonsterSpawn)
                return "M";

            TileVisualKind visualKind = _Settings.GetTileVisualKind(_Cell);
            if (_Settings.GetTileKind(_Cell) == TileKind.Blocked && visualKind == _Settings.DefaultVisualKind)
                return "X";

            switch (visualKind)
            {
                case TileVisualKind.Ground:
                    return "G";
                case TileVisualKind.Wall:
                    return "W";
                case TileVisualKind.Tree:
                    return "T";
                case TileVisualKind.Water:
                    return "~";
                case TileVisualKind.Decoration:
                    return "D";
                default:
                    return "?";
            }
        }

        private Color GetEditorCellColor(MvpTileMapSettings _Settings, Vector2Int _Cell)
        {
            Color cellColor = _Settings.GetTileColor(_Cell);
            if (_Cell == SelectedCell)
                return Color.Lerp(cellColor, Color.white, 0.35f);

            return cellColor;
        }
    }
}
#endif
