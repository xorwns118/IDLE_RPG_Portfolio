using System;
using System.Collections.Generic;
using IdleRPG.Domain;
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

        [Header("Tile Navigation")]
        public MvpTileNavigationSettings TileNavigation = new MvpTileNavigationSettings();

        [Header("Actor View")]
        public MvpActorViewSettings Actors = new MvpActorViewSettings();

        [Header("Combat Loop")]
        public MvpCombatLoopSettings CombatLoop = new MvpCombatLoopSettings();

        [Header("Monster Spawn")]
        public MvpMonsterSpawnSettings Spawn = new MvpMonsterSpawnSettings();

        [Header("Scene Flow")]
        public MvpSceneFlowSettings SceneFlow = new MvpSceneFlowSettings();

        [Header("Field Encounter")]
        public MvpFieldEncounterSettings FieldEncounter = new MvpFieldEncounterSettings();

        [Header("Turn Combat")]
        public MvpTurnCombatSettings TurnCombat = new MvpTurnCombatSettings();

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
            if (Camera == null)
                Camera = new MvpCameraSettings();

            if (World == null)
                World = new MvpWorldLayoutSettings();

            if (TileNavigation == null)
                TileNavigation = new MvpTileNavigationSettings();

            if (Actors == null)
                Actors = new MvpActorViewSettings();

            if (CombatLoop == null)
                CombatLoop = new MvpCombatLoopSettings();

            if (Spawn == null)
                Spawn = new MvpMonsterSpawnSettings();

            if (SceneFlow == null)
                SceneFlow = new MvpSceneFlowSettings();

            if (FieldEncounter == null)
                FieldEncounter = new MvpFieldEncounterSettings();

            if (TurnCombat == null)
                TurnCombat = new MvpTurnCombatSettings();

            if (Hud == null)
                Hud = new MvpHudSettings();

            if (RestartPanel == null)
                RestartPanel = new MvpRestartPanelSettings();

            if (Stage == null)
                Stage = new MvpStageRuntimeSettings();

            Camera.EnsureDefaults();
            World.EnsureDefaults();
            TileNavigation.EnsureDefaults();
            Actors.EnsureDefaults();
            CombatLoop.EnsureDefaults();
            Spawn.EnsureDefaults();
            SceneFlow.EnsureDefaults();
            FieldEncounter.EnsureDefaults();
            TurnCombat.EnsureDefaults();
            Hud.EnsureDefaults();
            RestartPanel.EnsureDefaults();
            Stage.EnsureDefaults();
        }
    }

    [Serializable]
    public sealed class MvpCombatLoopSettings
    {
        [Tooltip("Choose exactly one combat loop. Realtime uses per-actor AutoCombat, TurnBased uses the stage-level turn controller.")]
        public CombatLoopMode Mode = CombatLoopMode.Realtime;

        public void EnsureDefaults()
        {
            if (!Enum.IsDefined(typeof(CombatLoopMode), Mode))
                Mode = CombatLoopMode.Realtime;
        }
    }

    [Serializable]
    public sealed class MvpCameraSettings
    {
        [Tooltip("Fallback camera position. When Auto Fit Tile Map is enabled, X/Y are replaced by the tile map center and Z is kept from this value.")]
        public Vector3 Position = new Vector3(0f, 0.05f, -10f);

        [Tooltip("Fallback orthographic size used when no tile map is available or Auto Fit Tile Map is disabled.")]
        [Min(0.1f)]
        public float OrthographicSize = 4.35f;

        [Tooltip("Center the camera on the tile map and derive orthographic size from the configured cell count.")]
        public bool AutoFitTileMap = true;

        [Tooltip("Largest expected map width in cells. Smaller maps keep this frame size and are centered inside it.")]
        [Min(1)] public int ReferenceMaxColumns = 10;

        [Tooltip("Largest expected map height in cells. Smaller maps keep this frame size and are centered inside it.")]
        [Min(1)] public int ReferenceMaxRows = 12;

        [Tooltip("Extra empty space around the fitted map, measured in tile cells.")]
        [Min(0f)] public float TileMapPaddingCells = 0.75f;

        [Tooltip("Smallest allowed auto-fitted camera size.")]
        [Min(0.1f)] public float MinimumOrthographicSize = 3f;

        [Tooltip("Largest allowed auto-fitted camera size.")]
        [Min(0.1f)] public float MaximumOrthographicSize = 8f;

        public Color BackgroundColor = new Color(0.08f, 0.1f, 0.13f);

        public void EnsureDefaults()
        {
            OrthographicSize = Mathf.Max(0.1f, OrthographicSize);
            ReferenceMaxColumns = Mathf.Max(1, ReferenceMaxColumns);
            ReferenceMaxRows = Mathf.Max(1, ReferenceMaxRows);
            TileMapPaddingCells = Mathf.Max(0f, TileMapPaddingCells);
            MinimumOrthographicSize = Mathf.Max(0.1f, MinimumOrthographicSize);
            MaximumOrthographicSize = Mathf.Max(MinimumOrthographicSize, MaximumOrthographicSize);
        }

        public float CalculateTileMapOrthographicSize(MvpTileMapSettings _TileMapSettings, float _Aspect)
        {
            if (_TileMapSettings == null || !_TileMapSettings.Enabled)
                return OrthographicSize;

            float aspect = Mathf.Max(0.01f, _Aspect);
            Vector2 cellSize = _TileMapSettings.GetSafeCellSize();
            int fitColumns = Mathf.Max(ReferenceMaxColumns, _TileMapSettings.Columns);
            int fitRows = Mathf.Max(ReferenceMaxRows, _TileMapSettings.Rows);
            float paddingWidth = TileMapPaddingCells * cellSize.x * 2f;
            float paddingHeight = TileMapPaddingCells * cellSize.y * 2f;
            float requiredWidth = fitColumns * cellSize.x + paddingWidth;
            float requiredHeight = fitRows * cellSize.y + paddingHeight;
            float widthDrivenSize = requiredWidth * 0.5f / aspect;
            float heightDrivenSize = requiredHeight * 0.5f;

            return Mathf.Clamp(Mathf.Max(widthDrivenSize, heightDrivenSize), MinimumOrthographicSize, MaximumOrthographicSize);
        }
    }

    [Serializable]
    public sealed class MvpWorldLayoutSettings
    {
        [Header("Tile Map")]
        public MvpTileMapSettings TileMap = new MvpTileMapSettings();

        [Header("Legacy Ground Fallback")]
        public Vector3 GroundPosition = new Vector3(0f, -0.75f, 0f);
        public Vector3 GroundScale = new Vector3(8.5f, 0.08f, 1f);
        public Color GroundColor = new Color(0.34f, 0.38f, 0.42f);
        public int GroundSortingOrder = 1;

        [Tooltip("Hero start position at the beginning of every stage.")]
        public Vector3 PlayerStartPosition = new Vector3(-3.2f, 0f, 0f);

        [Tooltip("Base position used by Monster Spawn Point.")]
        public Vector3 MonsterSpawnPosition = new Vector3(3.3f, 0f, 0f);

        public void EnsureDefaults()
        {
            if (TileMap == null)
                TileMap = new MvpTileMapSettings();

            TileMap.EnsureDefaults();
        }
    }

    [Serializable]
    public sealed class MvpTileMapSettings
    {
        [Tooltip("Builds the MVP arena from square tiles instead of a horizontal ground bar.")]
        public bool Enabled = true;

        [Min(1)] public int Columns = 8;
        [Min(1)] public int Rows = 5;
        public Vector2 CellSize = new Vector2(0.8f, 0.8f);
        public Vector3 Origin = new Vector3(-2.8f, -1.6f, 0f);
        public Vector2Int PlayerStartCell = new Vector2Int(1, 2);
        [HideInInspector]
        public Vector2Int MonsterSpawnCell = new Vector2Int(6, 2);
        [Tooltip("Tile cells that can be used as monster spawn locations. The first cell is also used by the scene marker.")]
        public Vector2Int[] MonsterSpawnCells = Array.Empty<Vector2Int>();
        public Vector3 ActorAnchorOffset = new Vector3(0f, 0.22f, 0f);

        [Header("Tile Colors")]
        public Color PrimaryTileColor = new Color(0.16f, 0.25f, 0.25f, 1f);
        public Color AlternateTileColor = new Color(0.12f, 0.2f, 0.23f, 1f);
        public Color PlayerStartTileColor = new Color(0.18f, 0.42f, 0.55f, 1f);
        public Color MonsterSpawnTileColor = new Color(0.32f, 0.34f, 0.2f, 1f);
        public Color BlockedTileColor = new Color(0.08f, 0.08f, 0.09f, 1f);

        [Header("Tile Sprite Palette")]
        [Tooltip("Visual type used by cells without an override.")]
        public TileVisualKind DefaultVisualKind = TileVisualKind.Ground;

        [Tooltip("Assign sliced PNG sprites here. Each cell can then paint one visual type from this palette.")]
        public MvpTileSpriteSettings[] SpritePalette = MvpTileSpriteSettings.CreateDefaultPalette();

        [Header("Cell Overrides")]
        public MvpTileCellSettings[] CellOverrides = Array.Empty<MvpTileCellSettings>();

        [Header("Sorting")]
        public int TileSortingOrderBase = -20;
        [Min(1)] public int TileSortingOrderStep = 1;
        public int ActorSortingOrderBase = 30;
        [Min(1)] public int ActorSortingOrderStep = 1;
        [Min(0)] public int OverlaySortingOffset = 20;

        public void EnsureDefaults()
        {
            Columns = Mathf.Max(1, Columns);
            Rows = Mathf.Max(1, Rows);
            CellSize = GetSafeCellSize();
            PlayerStartCell = ClampCell(PlayerStartCell);
            MonsterSpawnCell = ClampCell(MonsterSpawnCell);
            TileSortingOrderStep = Mathf.Max(1, TileSortingOrderStep);
            ActorSortingOrderStep = Mathf.Max(1, ActorSortingOrderStep);
            OverlaySortingOffset = Mathf.Max(0, OverlaySortingOffset);
            NormalizeSpritePalette();
            NormalizeCellOverrides();
            NormalizeMonsterSpawnCells();
        }

        public Vector2 GetSafeCellSize()
        {
            float squareCellSize = Mathf.Max(0.1f, Mathf.Max(CellSize.x, CellSize.y));
            return new Vector2(squareCellSize, squareCellSize);
        }

        public Vector2 GetMapSize()
        {
            Vector2 cellSize = GetSafeCellSize();
            return new Vector2(
                Mathf.Max(1, Columns) * cellSize.x,
                Mathf.Max(1, Rows) * cellSize.y);
        }

        public Vector3 GetMapCenterLocal()
        {
            Vector2 cellSize = GetSafeCellSize();
            return new Vector3(
                Origin.x + (Mathf.Max(1, Columns) - 1) * cellSize.x * 0.5f,
                Origin.y + (Mathf.Max(1, Rows) - 1) * cellSize.y * 0.5f,
                Origin.z);
        }

        public Bounds GetLocalBounds()
        {
            Vector2 mapSize = GetMapSize();
            return new Bounds(GetMapCenterLocal(), new Vector3(mapSize.x, mapSize.y, 0f));
        }

        public Vector3 CellToLocal(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);

            float x = Origin.x + cell.x * CellSize.x;
            float y = Origin.y + cell.y * CellSize.y;

            return new Vector3(x, y, Origin.z);
        }

        public Vector2Int LocalToCell(Vector3 _LocalPosition)
        {
            int cellX = Mathf.RoundToInt((_LocalPosition.x - Origin.x) / CellSize.x);
            int cellY = Mathf.RoundToInt((_LocalPosition.y - Origin.y) / CellSize.y);

            return ClampCell(new Vector2Int(cellX, cellY));
        }

        public Vector2Int ClampCell(Vector2Int _Cell)
        {
            return new Vector2Int(
                Mathf.Clamp(_Cell.x, 0, Mathf.Max(0, Columns - 1)),
                Mathf.Clamp(_Cell.y, 0, Mathf.Max(0, Rows - 1)));
        }

        public int GetCellDistance(Vector2Int _From, Vector2Int _To)
        {
            Vector2Int from = ClampCell(_From);
            Vector2Int to = ClampCell(_To);
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        public int GetCellDepth(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            return cell.y;
        }

        public Color GetTileColor(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            MvpTileSpriteSettings spriteSettings = GetSpriteSettings(GetTileVisualKind(cell));
            if (spriteSettings != null && spriteSettings.Sprite != null)
                return spriteSettings.Tint;

            if (cell == PlayerStartCell)
                return PlayerStartTileColor;

            if (IsMonsterSpawnCell(cell))
                return MonsterSpawnTileColor;

            if (GetTileKind(cell) == TileKind.Blocked)
                return BlockedTileColor;

            return (cell.x + cell.y) % 2 == 0 ? PrimaryTileColor : AlternateTileColor;
        }

        public TileVisualKind GetTileVisualKind(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            MvpTileCellSettings overrideCell = FindCellOverride(cell);
            return overrideCell != null ? overrideCell.VisualKind : DefaultVisualKind;
        }

        public Sprite GetTileSprite(Vector2Int _Cell)
        {
            MvpTileSpriteSettings spriteSettings = GetSpriteSettings(GetTileVisualKind(_Cell));
            return spriteSettings != null ? spriteSettings.Sprite : null;
        }

        public TileKind GetDefaultTileKind(TileVisualKind _VisualKind)
        {
            MvpTileSpriteSettings spriteSettings = GetSpriteSettings(_VisualKind);
            return spriteSettings != null ? spriteSettings.DefaultKind : TileKind.Walkable;
        }

        public MvpTileSpriteSettings GetSpriteSettings(TileVisualKind _VisualKind)
        {
            if (SpritePalette == null)
                return null;

            MvpTileSpriteSettings fallback = null;
            foreach (MvpTileSpriteSettings spriteSettings in SpritePalette)
            {
                if (spriteSettings == null || spriteSettings.VisualKind != _VisualKind)
                    continue;

                if (fallback == null)
                    fallback = spriteSettings;

                if (spriteSettings.Sprite != null)
                    return spriteSettings;
            }

            return fallback;
        }

        public TileKind GetTileKind(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            MvpTileCellSettings overrideCell = FindCellOverride(cell);
            return overrideCell != null ? overrideCell.Kind : TileKind.Walkable;
        }

        public bool IsWalkable(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            if (cell == PlayerStartCell)
                return true;

            return GetTileKind(cell) != TileKind.Blocked && GetDefaultTileKind(GetTileVisualKind(cell)) != TileKind.Blocked;
        }

        public void SetTileKind(Vector2Int _Cell, TileKind _Kind)
        {
            Vector2Int cell = ClampCell(_Cell);
            SetCell(cell, _Kind, GetTileVisualKind(cell));
        }

        public void SetTileVisualKind(Vector2Int _Cell, TileVisualKind _VisualKind)
        {
            Vector2Int cell = ClampCell(_Cell);
            SetCell(cell, GetTileKind(cell), _VisualKind);
        }

        public void PaintTileVisual(Vector2Int _Cell, TileVisualKind _VisualKind)
        {
            SetCell(_Cell, GetDefaultTileKind(_VisualKind), _VisualKind);
        }

        public void SetCell(Vector2Int _Cell, TileKind _Kind, TileVisualKind _VisualKind)
        {
            Vector2Int cell = ClampCell(_Cell);
            bool shouldNormalizeSpawnCells = IsMonsterSpawnCell(cell) || _Kind == TileKind.Blocked;
            TileKind kind = cell == PlayerStartCell ? TileKind.Walkable : _Kind;
            if (kind == TileKind.Walkable && _VisualKind == DefaultVisualKind)
            {
                RemoveTileOverride(cell);
                if (shouldNormalizeSpawnCells)
                    NormalizeMonsterSpawnCells();

                return;
            }

            if (CellOverrides == null)
                CellOverrides = Array.Empty<MvpTileCellSettings>();

            for (int i = 0; i < CellOverrides.Length; i++)
            {
                MvpTileCellSettings overrideCell = CellOverrides[i];
                if (overrideCell == null || overrideCell.Cell != cell)
                    continue;

                overrideCell.Kind = kind;
                overrideCell.VisualKind = _VisualKind;
                if (shouldNormalizeSpawnCells)
                    NormalizeMonsterSpawnCells();

                return;
            }

            Array.Resize(ref CellOverrides, CellOverrides.Length + 1);
            CellOverrides[CellOverrides.Length - 1] = new MvpTileCellSettings(cell, kind, _VisualKind);
            if (shouldNormalizeSpawnCells)
                NormalizeMonsterSpawnCells();
        }

        public void RemoveTileOverride(Vector2Int _Cell)
        {
            if (CellOverrides == null || CellOverrides.Length == 0)
            {
                CellOverrides = Array.Empty<MvpTileCellSettings>();
                return;
            }

            Vector2Int cell = ClampCell(_Cell);
            List<MvpTileCellSettings> overrides = new List<MvpTileCellSettings>(CellOverrides.Length);
            foreach (MvpTileCellSettings overrideCell in CellOverrides)
            {
                if (overrideCell != null && overrideCell.Cell != cell)
                    overrides.Add(overrideCell);
            }

            CellOverrides = overrides.ToArray();
        }

        public int MonsterSpawnCellCount => MonsterSpawnCells != null && MonsterSpawnCells.Length > 0 ? MonsterSpawnCells.Length : 1;
        public bool HasMultipleMonsterSpawnCells => MonsterSpawnCellCount > 1;

        public Vector2Int GetPrimaryMonsterSpawnCell()
        {
            if (MonsterSpawnCells == null || MonsterSpawnCells.Length == 0)
                return ClampCell(MonsterSpawnCell);

            return ClampCell(MonsterSpawnCells[0]);
        }

        public Vector2Int GetMonsterSpawnCell(int _Index)
        {
            if (MonsterSpawnCells == null || MonsterSpawnCells.Length == 0)
                return ClampCell(MonsterSpawnCell);

            int safeIndex = Mathf.Abs(_Index) % MonsterSpawnCells.Length;
            return ClampCell(MonsterSpawnCells[safeIndex]);
        }

        public Vector2Int[] GetMonsterSpawnCells()
        {
            int count = MonsterSpawnCellCount;
            Vector2Int[] cells = new Vector2Int[count];
            for (int i = 0; i < count; i++)
            {
                cells[i] = GetMonsterSpawnCell(i);
            }

            return cells;
        }

        public bool IsMonsterSpawnCell(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            if (MonsterSpawnCells == null || MonsterSpawnCells.Length == 0)
                return cell == ClampCell(MonsterSpawnCell);

            for (int i = 0; i < MonsterSpawnCells.Length; i++)
            {
                if (ClampCell(MonsterSpawnCells[i]) == cell)
                    return true;
            }

            return false;
        }

        public void SetPrimaryMonsterSpawnCell(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            if (!CanUseMonsterSpawnCell(cell))
                return;

            List<Vector2Int> cells = BuildUniqueMonsterSpawnCells();
            cells.Remove(cell);
            cells.Insert(0, cell);
            ApplyMonsterSpawnCells(cells);
        }

        public void AddMonsterSpawnCell(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            if (!CanUseMonsterSpawnCell(cell))
                return;

            List<Vector2Int> cells = BuildUniqueMonsterSpawnCells();
            if (!cells.Contains(cell))
                cells.Add(cell);

            ApplyMonsterSpawnCells(cells);
        }

        public bool CanUseMonsterSpawnCell(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            return cell != PlayerStartCell
                && GetTileKind(cell) != TileKind.Blocked
                && GetDefaultTileKind(GetTileVisualKind(cell)) != TileKind.Blocked;
        }

        public void RemoveMonsterSpawnCell(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            List<Vector2Int> cells = BuildUniqueMonsterSpawnCells();
            if (cells.Count <= 1)
            {
                ApplyMonsterSpawnCells(cells);
                return;
            }

            cells.Remove(cell);
            if (cells.Count == 0)
                cells.Add(cell);

            ApplyMonsterSpawnCells(cells);
        }

        private void NormalizeSpritePalette()
        {
            if (SpritePalette == null)
                SpritePalette = Array.Empty<MvpTileSpriteSettings>();

            Array visualKinds = Enum.GetValues(typeof(TileVisualKind));
            List<MvpTileSpriteSettings> normalized = new List<MvpTileSpriteSettings>(visualKinds.Length);
            bool shouldAssign = SpritePalette.Length != visualKinds.Length;
            foreach (object visualKindValue in visualKinds)
            {
                TileVisualKind visualKind = (TileVisualKind)visualKindValue;
                MvpTileSpriteSettings spriteSettings = GetSpriteSettings(visualKind) ?? MvpTileSpriteSettings.CreateDefault(visualKind);
                spriteSettings.VisualKind = visualKind;
                spriteSettings.EnsureDefaults();
                if (!shouldAssign && SpritePalette[normalized.Count] != spriteSettings)
                    shouldAssign = true;

                normalized.Add(spriteSettings);
            }

            if (shouldAssign)
                SpritePalette = normalized.ToArray();
        }

        private void NormalizeCellOverrides()
        {
            if (CellOverrides == null)
            {
                CellOverrides = Array.Empty<MvpTileCellSettings>();
                return;
            }

            if (CellOverrides.Length == 0)
                return;

            List<MvpTileCellSettings> normalized = new List<MvpTileCellSettings>(CellOverrides.Length);
            HashSet<Vector2Int> seenCells = new HashSet<Vector2Int>();
            bool shouldAssign = false;

            for (int i = CellOverrides.Length - 1; i >= 0; i--)
            {
                MvpTileCellSettings overrideCell = CellOverrides[i];
                if (overrideCell == null)
                {
                    shouldAssign = true;
                    continue;
                }

                Vector2Int clampedCell = ClampCell(overrideCell.Cell);
                if (overrideCell.Cell != clampedCell)
                {
                    overrideCell.Cell = clampedCell;
                    shouldAssign = true;
                }

                if (!Enum.IsDefined(typeof(TileVisualKind), overrideCell.VisualKind))
                {
                    overrideCell.VisualKind = DefaultVisualKind;
                    shouldAssign = true;
                }

                if (overrideCell.Cell != PlayerStartCell && overrideCell.Kind == TileKind.Walkable && GetDefaultTileKind(overrideCell.VisualKind) == TileKind.Blocked)
                {
                    overrideCell.Kind = TileKind.Blocked;
                    shouldAssign = true;
                }

                if ((overrideCell.Kind == TileKind.Walkable && overrideCell.VisualKind == DefaultVisualKind) || seenCells.Contains(overrideCell.Cell))
                {
                    shouldAssign = true;
                    continue;
                }

                seenCells.Add(overrideCell.Cell);
                normalized.Insert(0, overrideCell);
            }

            if (shouldAssign || normalized.Count != CellOverrides.Length)
                CellOverrides = normalized.ToArray();
        }

        private void NormalizeMonsterSpawnCells()
        {
            List<Vector2Int> sourceCells = BuildUniqueMonsterSpawnCells();
            List<Vector2Int> normalizedCells = new List<Vector2Int>(sourceCells.Count);
            for (int i = 0; i < sourceCells.Count; i++)
            {
                Vector2Int cell = ResolveUsableMonsterSpawnCell(sourceCells[i]);
                if (!normalizedCells.Contains(cell))
                    normalizedCells.Add(cell);
            }

            if (normalizedCells.Count == 0)
                normalizedCells.Add(ResolveUsableMonsterSpawnCell(MonsterSpawnCell));

            ApplyMonsterSpawnCells(normalizedCells);
        }

        private List<Vector2Int> BuildUniqueMonsterSpawnCells()
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            if (MonsterSpawnCells != null)
            {
                for (int i = 0; i < MonsterSpawnCells.Length; i++)
                {
                    AddUniqueMonsterSpawnCell(cells, MonsterSpawnCells[i]);
                }
            }

            if (cells.Count == 0)
                AddUniqueMonsterSpawnCell(cells, MonsterSpawnCell);

            return cells;
        }

        private void AddUniqueMonsterSpawnCell(List<Vector2Int> _Cells, Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            if (!_Cells.Contains(cell))
                _Cells.Add(cell);
        }

        private void ApplyMonsterSpawnCells(List<Vector2Int> _Cells)
        {
            if (_Cells == null || _Cells.Count == 0)
                _Cells = new List<Vector2Int> { ResolveUsableMonsterSpawnCell(MonsterSpawnCell) };

            MonsterSpawnCell = ClampCell(_Cells[0]);
            bool shouldAssign = MonsterSpawnCells == null || MonsterSpawnCells.Length != _Cells.Count;
            if (!shouldAssign)
            {
                for (int i = 0; i < _Cells.Count; i++)
                {
                    if (MonsterSpawnCells[i] == _Cells[i])
                        continue;

                    shouldAssign = true;
                    break;
                }
            }

            if (shouldAssign)
                MonsterSpawnCells = _Cells.ToArray();
        }

        private Vector2Int ResolveUsableMonsterSpawnCell(Vector2Int _Cell)
        {
            Vector2Int cell = ClampCell(_Cell);
            if (CanUseMonsterSpawnCell(cell))
                return cell;

            int maxDistance = Columns + Rows;
            for (int distance = 0; distance <= maxDistance; distance++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Columns; x++)
                    {
                        Vector2Int candidate = new Vector2Int(x, y);
                        if (GetCellDistance(cell, candidate) == distance && CanUseMonsterSpawnCell(candidate))
                            return candidate;
                    }
                }
            }

            return cell;
        }

        private MvpTileCellSettings FindCellOverride(Vector2Int _Cell)
        {
            if (CellOverrides == null)
                return null;

            foreach (MvpTileCellSettings overrideCell in CellOverrides)
            {
                if (overrideCell != null && overrideCell.Cell == _Cell)
                    return overrideCell;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class MvpTileSpriteSettings
    {
        public TileVisualKind VisualKind = TileVisualKind.Ground;
        public Sprite Sprite;
        public Color Tint = Color.white;
        public Vector2 DrawSizeInCells = Vector2.one;
        public Vector3 LocalOffset = Vector3.zero;
        public int SortingOffset;
        public TileKind DefaultKind = TileKind.Walkable;

        public MvpTileSpriteSettings()
        {
        }

        public MvpTileSpriteSettings(TileVisualKind _VisualKind, TileKind _DefaultKind)
        {
            VisualKind = _VisualKind;
            DefaultKind = _DefaultKind;
            Tint = Color.white;
        }

        public void EnsureDefaults()
        {
            if (!Enum.IsDefined(typeof(TileVisualKind), VisualKind))
                VisualKind = TileVisualKind.Ground;

            DrawSizeInCells = new Vector2(
                Mathf.Max(0.01f, DrawSizeInCells.x),
                Mathf.Max(0.01f, DrawSizeInCells.y));
        }

        public static MvpTileSpriteSettings[] CreateDefaultPalette()
        {
            Array visualKinds = Enum.GetValues(typeof(TileVisualKind));
            MvpTileSpriteSettings[] palette = new MvpTileSpriteSettings[visualKinds.Length];
            for (int i = 0; i < visualKinds.Length; i++)
            {
                object visualKindValue = visualKinds.GetValue(i);
                palette[i] = CreateDefault((TileVisualKind)visualKindValue);
            }

            return palette;
        }

        public static MvpTileSpriteSettings CreateDefault(TileVisualKind _VisualKind)
        {
            return new MvpTileSpriteSettings(_VisualKind, GetDefaultKind(_VisualKind));
        }

        private static TileKind GetDefaultKind(TileVisualKind _VisualKind)
        {
            switch (_VisualKind)
            {
                case TileVisualKind.Wall:
                case TileVisualKind.Tree:
                case TileVisualKind.Water:
                    return TileKind.Blocked;
                default:
                    return TileKind.Walkable;
            }
        }
    }

    [Serializable]
    public sealed class MvpTileCellSettings
    {
        public Vector2Int Cell;
        public TileKind Kind = TileKind.Walkable;
        public TileVisualKind VisualKind = TileVisualKind.Ground;

        public MvpTileCellSettings()
        {
        }

        public MvpTileCellSettings(Vector2Int _Cell, TileKind _Kind)
        {
            Cell = _Cell;
            Kind = _Kind;
            VisualKind = TileVisualKind.Ground;
        }

        public MvpTileCellSettings(Vector2Int _Cell, TileKind _Kind, TileVisualKind _VisualKind)
        {
            Cell = _Cell;
            Kind = _Kind;
            VisualKind = _VisualKind;
        }
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
        [Header("Animation")]
        [Tooltip("Animator Controller assigned to runtime-spawned actors. Use an Animator Override Controller when character clips differ by prefab.")]
        public RuntimeAnimatorController AnimatorController;
        public MvpActorAnimationSettings Animation = new MvpActorAnimationSettings();
        [Header("Runtime Helpers")]
        public MvpHealthBarSettings HealthBar = new MvpHealthBarSettings();
        public MvpAutoCombatSettings AutoCombat = new MvpAutoCombatSettings();
        public MvpTargetingSettings Targeting = new MvpTargetingSettings();

        public static MvpActorViewSettings CreateDefault()
        {
            return new MvpActorViewSettings();
        }

        public void EnsureDefaults()
        {
            if (HealthBar == null) HealthBar = new MvpHealthBarSettings();
            if (AutoCombat == null) AutoCombat = new MvpAutoCombatSettings();
            if (Targeting == null) Targeting = new MvpTargetingSettings();
            if (Animation == null) Animation = new MvpActorAnimationSettings();
            NameLabelCharacterSize = Mathf.Max(0.01f, NameLabelCharacterSize);
            NameLabelFontSize = Mathf.Max(1, NameLabelFontSize);
            Animation.EnsureDefaults();
            AutoCombat.EnsureDefaults();
            Targeting.EnsureDefaults();
        }
    }

    [Serializable]
    public sealed class MvpActorAnimationSettings
    {
        [Tooltip("Enable Animator parameter updates for runtime-spawned actors.")]
        public bool Enabled = true;

        [Tooltip("Bool parameter set to true only while the actor position changes.")]
        public string WalkParameterName = "IsWalk";

        [Tooltip("Bool parameter set to true when the actor faces or moves left.")]
        public string LeftParameterName = "IsLeft";

        [Tooltip("Minimum frame movement treated as actual walking.")]
        [Min(0f)] public float MovementThreshold = 0.001f;

        [Tooltip("Keep SpriteRenderer.flipX synced with facing direction. Keep this off when left/right animation clips already include their final facing direction.")]
        public bool MirrorSpriteRendererByFacing;

        public void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(WalkParameterName))
                WalkParameterName = "IsWalk";

            if (string.IsNullOrWhiteSpace(LeftParameterName))
                LeftParameterName = "IsLeft";

            MovementThreshold = Mathf.Max(0f, MovementThreshold);
        }
    }

    [Serializable]
    public sealed class MvpTargetingSettings
    {
        [Tooltip("Rule used when several enemies are valid.")]
        public TargetSelectionMode SelectionMode = TargetSelectionMode.Nearest;

        [Tooltip("If enabled, actors ignore enemies outside Search Range. Keep this off for battle scenes that should auto-start from any spawn distance.")]
        public bool LimitSearchRange = false;

        [Min(0.1f)] public float SearchRange = 8f;

        [Tooltip("Extra tolerance added to Attack Range checks.")]
        [Min(0f)] public float AttackRangePadding = 0.05f;

        public void EnsureDefaults()
        {
            if (!Enum.IsDefined(typeof(TargetSelectionMode), SelectionMode))
                SelectionMode = TargetSelectionMode.Nearest;

            SearchRange = Mathf.Max(0.1f, SearchRange);
            AttackRangePadding = Mathf.Max(0f, AttackRangePadding);
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
    public sealed class MvpTileNavigationSettings
    {
        [Tooltip("When a Tile Map Layout exists, actors use tile navigation so blocked cells are avoided.")]
        public bool UseTileMovement = true;

        [Tooltip("Compress the A* tile path into farther waypoints for more natural realtime movement.")]
        public bool UseWaypointCompression = true;

        [Tooltip("Allow compressed realtime waypoints to move across X and Y at the same time.")]
        public bool AllowDiagonalMovement = true;

        public void EnsureDefaults()
        {
        }
    }

    [Serializable]
    public sealed class MvpAutoCombatSettings
    {
        [Tooltip("Enable real-time per-actor auto combat updates.")]
        public bool Enabled = true;

        [Min(0f)] public float InitialAttackDelayMin = 0f;
        [Min(0f)] public float InitialAttackDelayMax = 0.15f;
        [Tooltip("Shared delay after any skill succeeds. Prevents several ready skills from firing in the same moment.")]
        [Min(0f)] public float SkillUseDelaySeconds = 1f;
        [Tooltip("Delay after a skill becomes ready before auto combat may cast it. Keeps Ready visible before the skill fires.")]
        [Min(0f)] public float SkillReadyDelaySeconds = 1f;
        [Tooltip("Distance used to snap movement to its destination.")]
        [Min(0.001f)] public float TileArrivalThreshold = 0.03f;

        public float ClampInitialDelayMax()
        {
            return Mathf.Max(InitialAttackDelayMin, InitialAttackDelayMax);
        }

        public void EnsureDefaults()
        {
            InitialAttackDelayMin = Mathf.Max(0f, InitialAttackDelayMin);
            InitialAttackDelayMax = Mathf.Max(InitialAttackDelayMin, InitialAttackDelayMax);
            SkillUseDelaySeconds = Mathf.Max(0f, SkillUseDelaySeconds);
            SkillReadyDelaySeconds = Mathf.Max(0f, SkillReadyDelaySeconds);
            TileArrivalThreshold = Mathf.Max(0.001f, TileArrivalThreshold);
        }
    }

    [Serializable]
    public sealed class MvpSceneFlowSettings
    {
        [Tooltip("If enabled, Stage Scene Flow will load the configured scenes when flow changes.")]
        public bool LoadConfiguredScenes = false;

        public StageFlowMode InitialMode = StageFlowMode.Battle;
        public string FieldSceneName = "FieldScene";
        public string BattleSceneName = "BattleScene";

        public void EnsureDefaults()
        {
            if (!Enum.IsDefined(typeof(StageFlowMode), InitialMode))
                InitialMode = StageFlowMode.Battle;

            if (string.IsNullOrWhiteSpace(FieldSceneName))
                FieldSceneName = "FieldScene";

            if (string.IsNullOrWhiteSpace(BattleSceneName))
                BattleSceneName = "BattleScene";
        }
    }

    [Serializable]
    public sealed class MvpFieldEncounterSettings
    {
        [Tooltip("Enable field encounter checks.")]
        public bool Enabled = false;

        public EncounterTriggerMode TriggerMode = EncounterTriggerMode.Distance;

        [Tooltip("Distance between the player and encounter point that starts battle.")]
        [Min(0.01f)] public float TriggerDistance = 0.75f;

        [Min(1)] public int BattleStageNumber = 1;
        public bool TriggerOnce = true;

        public void EnsureDefaults()
        {
            if (!Enum.IsDefined(typeof(EncounterTriggerMode), TriggerMode))
                TriggerMode = EncounterTriggerMode.Distance;

            TriggerDistance = Mathf.Max(0.01f, TriggerDistance);
            BattleStageNumber = Mathf.Max(1, BattleStageNumber);
        }
    }

    [Serializable]
    public sealed class MvpTurnCombatSettings
    {
        [Min(0.01f)] public float TurnDelaySeconds = 0.45f;
        public bool PlayerActsFirst = true;
        [Tooltip("Shared delay after any skill succeeds. Turn-based mode checks this before selecting another skill for the same actor.")]
        [Min(0f)] public float SkillUseDelaySeconds = 1f;
        [Tooltip("Delay after a skill becomes ready before turn-based auto combat may cast it.")]
        [Min(0f)] public float SkillReadyDelaySeconds = 1f;
        [Tooltip("Seconds used as the movement budget in non-tile turn combat.")]
        [Min(0.01f)] public float WorldMoveSecondsPerTurn = 0.45f;
        [Tooltip("Visual duration for one turn movement action.")]
        [Min(0f)] public float MoveAnimationDuration = 0.18f;
        [Tooltip("Distance treated as no movement for turn actions.")]
        [Min(0.001f)] public float ArrivalThreshold = 0.03f;

        public void EnsureDefaults()
        {
            TurnDelaySeconds = Mathf.Max(0.01f, TurnDelaySeconds);
            SkillUseDelaySeconds = Mathf.Max(0f, SkillUseDelaySeconds);
            SkillReadyDelaySeconds = Mathf.Max(0f, SkillReadyDelaySeconds);
            WorldMoveSecondsPerTurn = Mathf.Max(0.01f, WorldMoveSecondsPerTurn);
            MoveAnimationDuration = Mathf.Max(0f, MoveAnimationDuration);
            ArrivalThreshold = Mathf.Max(0.001f, ArrivalThreshold);
        }
    }

    [Serializable]
    public sealed class MvpMonsterSpawnSettings
    {
        [Tooltip("Position used when a scene spawn point is not assigned.")]
        public Vector3 FallbackPosition = new Vector3(3.3f, 0f, 0f);

        [Tooltip("How to choose one spawn location when several positions or cells are configured.")]
        public MonsterSpawnSelectionMode SelectionMode = MonsterSpawnSelectionMode.Sequential;

        [Tooltip("Optional world positions used when Tile Map is disabled. If empty, the scene Monster Spawn Point and repeated offset are used.")]
        public Vector3[] SpawnPositions = new Vector3[0];

        [Tooltip("Offset added for repeated spawns in the same stage.")]
        public Vector3 RepeatedSpawnOffset = new Vector3(0.35f, 0f, 0f);

        [Tooltip("Uses tile coordinates when a Tile Map Layout exists in the scene.")]
        public bool UseTileSpawnOffset = true;

        [Tooltip("Optional tile cells used when Tile Map is enabled. If empty, the scene Monster Spawn Point and repeated cell offset are used.")]
        public Vector2Int[] SpawnCells = new Vector2Int[0];

        [Tooltip("Cell offset added for repeated spawns in the same stage.")]
        public Vector2Int RepeatedSpawnCellOffset = new Vector2Int(0, 1);

        public bool HasSpawnPositions => SpawnPositions != null && SpawnPositions.Length > 0;
        public bool HasSpawnCells => SpawnCells != null && SpawnCells.Length > 0;

        public void EnsureDefaults()
        {
            if (!Enum.IsDefined(typeof(MonsterSpawnSelectionMode), SelectionMode))
                SelectionMode = MonsterSpawnSelectionMode.Sequential;

            if (SpawnPositions == null)
                SpawnPositions = new Vector3[0];

            if (SpawnCells == null)
                SpawnCells = new Vector2Int[0];

            if (RepeatedSpawnCellOffset == Vector2Int.zero)
                RepeatedSpawnCellOffset = new Vector2Int(0, 1);
        }

        public int SelectSpawnIndex(int _SpawnCount, int _SpawnCountLimit)
        {
            if (_SpawnCountLimit <= 0)
                return -1;

            if (SelectionMode == MonsterSpawnSelectionMode.Random)
                return UnityEngine.Random.Range(0, _SpawnCountLimit);

            return Mathf.Abs(_SpawnCount) % _SpawnCountLimit;
        }
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

        [Header("Skill UI")]
        public MvpSkillHudSettings SkillUi = new MvpSkillHudSettings();

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
            if (SkillUi == null) SkillUi = new MvpSkillHudSettings();
            if (TitleText == null) TitleText = new MvpTextSlotSettings(new Vector2(18f, -15f), new Vector2(430f, 26f), 18, FontStyle.Bold);
            if (StageText == null) StageText = new MvpTextSlotSettings(new Vector2(18f, -48f), new Vector2(430f, 24f), 15, FontStyle.Bold);
            if (ResourceText == null) ResourceText = new MvpTextSlotSettings(new Vector2(18f, -74f), new Vector2(430f, 24f), 14, FontStyle.Normal);
            if (PlayerText == null) PlayerText = new MvpTextSlotSettings(new Vector2(18f, -106f), new Vector2(430f, 22f), 14, FontStyle.Normal);
            if (EnemyText == null) EnemyText = new MvpTextSlotSettings(new Vector2(18f, -145f), new Vector2(430f, 22f), 14, FontStyle.Normal);
            if (LogText == null) LogText = new MvpTextSlotSettings(new Vector2(18f, -181f), new Vector2(430f, 24f), 13, FontStyle.Italic);
            SkillUi.EnsureDefaults();
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
    public sealed class MvpSkillHudSettings
    {
        [Tooltip("Create the visible skill panel in the generated MVP HUD.")]
        public bool Enabled = true;

        [Tooltip("Title text shown above the skill slots.")]
        public string Title = "Skills";

        [Tooltip("Text used for a skill slot with no assigned skill.")]
        public string EmptySlotText = "-";

        [Tooltip("Text shown when a skill can be used.")]
        public string ReadyText = "Ready";

        [Tooltip("Cooldown text format. {0} is the remaining cooldown in seconds.")]
        public string CooldownFormat = "{0:0.0}s";

        [Tooltip("Positive cooldown values are rounded up to this display step, avoiding 0.0s before Ready.")]
        [Min(0.01f)] public float CooldownDisplayStepSeconds = 0.1f;

        [Tooltip("Skill slot name format. {0} is slot number, {1} is skill display name.")]
        public string SlotFormat = "{0}. {1}";

        [Header("Panel Layout")]
        public Vector2 PanelPosition = new Vector2(18f, -248f);
        public Vector2 PanelSize = new Vector2(470f, 142f);
        public Color PanelColor = new Color(0.05f, 0.055f, 0.07f, 0.78f);
        public Vector2 TitlePosition = new Vector2(14f, -10f);
        public Vector2 TitleSize = new Vector2(442f, 22f);
        [Min(1)] public int TitleFontSize = 14;

        [Header("Slot Layout")]
        public Vector2 SlotStartPosition = new Vector2(14f, -40f);
        public Vector2 SlotSize = new Vector2(105f, 84f);
        public Vector2 SlotSpacing = new Vector2(112f, 0f);

        [Header("Slot Colors")]
        public Color SlotReadyColor = new Color(0.16f, 0.23f, 0.22f, 0.96f);
        public Color SlotCooldownColor = new Color(0.11f, 0.12f, 0.15f, 0.96f);
        public Color SlotEmptyColor = new Color(0.08f, 0.08f, 0.09f, 0.84f);
        public Color CooldownFillColor = new Color(0.05f, 0.55f, 0.85f, 0.65f);

        [Header("Text Colors")]
        public Color SkillNameTextColor = Color.white;
        public Color ReadyTextColor = new Color(0.78f, 1f, 0.86f, 1f);
        public Color CooldownTextColor = new Color(1f, 0.84f, 0.52f, 1f);
        public Color EmptyTextColor = new Color(0.55f, 0.58f, 0.62f, 1f);
        [Min(1)] public int SkillNameFontSize = 12;
        [Min(1)] public int CooldownFontSize = 12;

        public void EnsureDefaults()
        {
            TitleFontSize = Mathf.Max(1, TitleFontSize);
            SkillNameFontSize = Mathf.Max(1, SkillNameFontSize);
            CooldownFontSize = Mathf.Max(1, CooldownFontSize);
            CooldownDisplayStepSeconds = Mathf.Max(0.01f, CooldownDisplayStepSeconds);
            PanelSize.x = Mathf.Max(1f, PanelSize.x);
            PanelSize.y = Mathf.Max(1f, PanelSize.y);
            TitleSize.x = Mathf.Max(1f, TitleSize.x);
            TitleSize.y = Mathf.Max(1f, TitleSize.y);
            SlotSize.x = Mathf.Max(1f, SlotSize.x);
            SlotSize.y = Mathf.Max(1f, SlotSize.y);
        }

        public string FormatSlot(int _SlotNumber, string _SkillName)
        {
            return MvpTextFormatter.Format(SlotFormat, _SlotNumber, _SkillName);
        }

        public string FormatCooldown(float _RemainingSeconds)
        {
            float remainingSeconds = Mathf.Max(0f, _RemainingSeconds);
            if (remainingSeconds > 0f)
                remainingSeconds = Mathf.Ceil(remainingSeconds / CooldownDisplayStepSeconds) * CooldownDisplayStepSeconds;

            return MvpTextFormatter.Format(CooldownFormat, remainingSeconds);
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

        [Tooltip("Skill damage log format. {0}=caster, {1}=skill, {2}=target, {3}=damage, {4}=critical suffix.")]
        public string SkillDamageLogFormat = "{0} used {1} on {2}: {3}{4}";

        [Tooltip("Non-damage skill log format. {0}=caster, {1}=skill.")]
        public string SkillUsedLogFormat = "{0} used {1}.";

        public string CriticalSuffix = " CRIT";
        public string FieldReadyLog = "Field mode ready. Move to an encounter point.";

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

        public string FormatSkillDamage(string _Attacker, string _SkillName, string _Target, string _Damage, bool _IsCritical)
        {
            return MvpTextFormatter.Format(SkillDamageLogFormat, _Attacker, _SkillName, _Target, _Damage, _IsCritical ? CriticalSuffix : string.Empty);
        }

        public string FormatSkillUsed(string _ActorName, string _SkillName)
        {
            return MvpTextFormatter.Format(SkillUsedLogFormat, _ActorName, _SkillName);
        }

        public void EnsureDefaults()
        {
            StartStageNumber = Mathf.Max(1, StartStageNumber);
            MonsterHpScalePerStage = Mathf.Max(0f, MonsterHpScalePerStage);
            MonsterAttackScalePerStage = Mathf.Max(0f, MonsterAttackScalePerStage);
            MonsterDefenseScalePerStage = Mathf.Max(0f, MonsterDefenseScalePerStage);
            RewardScalePerStage = Mathf.Max(0f, RewardScalePerStage);
            SpawnDelayAfterKill = Mathf.Max(0f, SpawnDelayAfterKill);
            StageAdvanceDelay = Mathf.Max(0f, StageAdvanceDelay);
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
                return string.Empty;

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
