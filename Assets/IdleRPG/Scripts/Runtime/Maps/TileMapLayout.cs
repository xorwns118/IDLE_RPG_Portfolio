using System.Collections.Generic;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Maps
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Idle RPG/Tile Map Layout")]
    public sealed class TileMapLayout : MonoBehaviour
    {
        [SerializeField] private MvpTileMapSettings SettingsValue = new MvpTileMapSettings();

        public MvpTileMapSettings Settings => SettingsValue;
        public bool IsEnabled => SettingsValue != null && SettingsValue.Enabled;

        public void Configure(MvpTileMapSettings _Settings)
        {
            SettingsValue = _Settings ?? new MvpTileMapSettings();
            SettingsValue.EnsureDefaults();
        }

        public void RebuildVisuals(Sprite _TileSprite)
        {
            if (!IsEnabled || _TileSprite == null)
            {
                ClearTileRoot();
                return;
            }

            SettingsValue.EnsureDefaults();
            Transform tileRoot = FindOrCreateTileRoot();
            HashSet<string> expectedTileNames = new HashSet<string>();

            for (int y = 0; y < SettingsValue.Rows; y++)
            {
                for (int x = 0; x < SettingsValue.Columns; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    string tileName = GetTileName(cell);
                    expectedTileNames.Add(tileName);
                    ConfigureTile(tileRoot, _TileSprite, cell, tileName);
                }
            }

            RemoveUnexpectedTiles(tileRoot, expectedTileNames);
        }

        public Vector3 CellToWorld(Vector2Int _Cell)
        {
            return transform.TransformPoint(SettingsValue.CellToLocal(_Cell));
        }

        public Vector3 CellToActorWorld(Vector2Int _Cell)
        {
            return CellToWorld(_Cell) + SettingsValue.ActorAnchorOffset;
        }

        public Vector2Int WorldToCell(Vector3 _WorldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(_WorldPosition - SettingsValue.ActorAnchorOffset);
            return SettingsValue.LocalToCell(localPosition);
        }

        public Vector2Int ClampCell(Vector2Int _Cell)
        {
            return SettingsValue.ClampCell(_Cell);
        }

        public int GetCellDistance(Vector2Int _From, Vector2Int _To)
        {
            return SettingsValue.GetCellDistance(_From, _To);
        }

        public bool IsWalkable(Vector2Int _Cell)
        {
            return SettingsValue.IsWalkable(_Cell);
        }

        public Vector2Int GetNearestWalkableCell(Vector2Int _Cell)
        {
            Vector2Int origin = ClampCell(_Cell);
            if (IsWalkable(origin))
            {
                return origin;
            }

            int maxDistance = SettingsValue.Columns + SettingsValue.Rows;
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                for (int y = 0; y < SettingsValue.Rows; y++)
                {
                    for (int x = 0; x < SettingsValue.Columns; x++)
                    {
                        Vector2Int candidate = new Vector2Int(x, y);
                        if (GetCellDistance(origin, candidate) == distance && IsWalkable(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return origin;
        }

        public int GetAttackRangeInCells(float _AttackRange)
        {
            return Mathf.Max(1, Mathf.CeilToInt(_AttackRange));
        }

        public Vector2Int GetNextCellToward(Vector2Int _From, Vector2Int _Target, int _StopDistance)
        {
            Vector2Int from = ClampCell(_From);
            Vector2Int target = ClampCell(_Target);
            int stopDistance = Mathf.Max(1, _StopDistance);
            if (GetCellDistance(from, target) <= stopDistance)
                return from;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> previousCells = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(from);
            visitedCells.Add(from);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                List<Vector2Int> candidates = BuildStepCandidates(current, target - current);

                foreach (Vector2Int candidate in candidates)
                {
                    Vector2Int next = ClampCell(candidate);
                    if (next == current || visitedCells.Contains(next) || !IsWalkable(next))
                        continue;

                    visitedCells.Add(next);
                    previousCells[next] = current;

                    if (GetCellDistance(next, target) <= stopDistance)
                        return GetFirstStep(from, next, previousCells);

                    queue.Enqueue(next);
                }
            }

            return from;
        }

        public int GetActorSortingOrder(Vector3 _WorldPosition, int _FallbackSortingOrder)
        {
            if (!IsEnabled)
                return _FallbackSortingOrder;

            Vector2Int cell = WorldToCell(_WorldPosition);
            return SettingsValue.ActorSortingOrderBase - SettingsValue.GetCellDepth(cell) * SettingsValue.ActorSortingOrderStep;
        }

        public int GetOverlaySortingOrder(int _ActorSortingOrder)
        {
            return _ActorSortingOrder + SettingsValue.OverlaySortingOffset;
        }

        private void ConfigureTile(Transform _TileRoot, Sprite _TileSprite, Vector2Int _Cell, string _TileName)
        {
            MvpTileSpriteSettings spriteSettings = SettingsValue.GetSpriteSettings(SettingsValue.GetTileVisualKind(_Cell));
            Sprite tileSprite = spriteSettings != null && spriteSettings.Sprite != null ? spriteSettings.Sprite : _TileSprite;

            Transform tileTransform = _TileRoot.Find(_TileName);
            GameObject tileObject = tileTransform != null ? tileTransform.gameObject : new GameObject(_TileName);
            tileObject.transform.SetParent(_TileRoot, false);
            tileObject.transform.localPosition = SettingsValue.CellToLocal(_Cell) + GetTileLocalOffset(spriteSettings);
            tileObject.transform.localScale = GetTileLocalScale(tileSprite, spriteSettings);

            SpriteRenderer renderer = tileObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = tileObject.AddComponent<SpriteRenderer>();

            renderer.sprite = tileSprite;
            renderer.color = SettingsValue.GetTileColor(_Cell);
            renderer.sortingOrder = SettingsValue.TileSortingOrderBase
                + SettingsValue.GetCellDepth(_Cell) * SettingsValue.TileSortingOrderStep
                + GetTileSortingOffset(spriteSettings);
        }

        private Vector3 GetTileLocalScale(Sprite _TileSprite, MvpTileSpriteSettings _SpriteSettings)
        {
            Vector2 drawSizeInCells = _SpriteSettings != null ? _SpriteSettings.DrawSizeInCells : Vector2.one;
            Vector2 spriteSize = _TileSprite != null ? _TileSprite.bounds.size : Vector2.one;
            float targetWidth = SettingsValue.CellSize.x * Mathf.Max(0.01f, drawSizeInCells.x);
            float targetHeight = SettingsValue.CellSize.y * Mathf.Max(0.01f, drawSizeInCells.y);

            return new Vector3(
                targetWidth / Mathf.Max(0.001f, spriteSize.x),
                targetHeight / Mathf.Max(0.001f, spriteSize.y),
                1f);
        }

        private static Vector3 GetTileLocalOffset(MvpTileSpriteSettings _SpriteSettings)
        {
            return _SpriteSettings != null ? _SpriteSettings.LocalOffset : Vector3.zero;
        }

        private static int GetTileSortingOffset(MvpTileSpriteSettings _SpriteSettings)
        {
            return _SpriteSettings != null ? _SpriteSettings.SortingOffset : 0;
        }

        private static List<Vector2Int> BuildStepCandidates(Vector2Int _From, Vector2Int _Delta)
        {
            List<Vector2Int> candidates = new List<Vector2Int>(4);
            bool preferHorizontal = Mathf.Abs(_Delta.x) >= Mathf.Abs(_Delta.y);
            if (preferHorizontal)
            {
                AddHorizontalCandidate(candidates, _From, _Delta.x);
                AddVerticalCandidate(candidates, _From, _Delta.y);
                AddVerticalCandidate(candidates, _From, 1);
                AddVerticalCandidate(candidates, _From, -1);
                AddHorizontalCandidate(candidates, _From, -_Delta.x);
            }
            else
            {
                AddVerticalCandidate(candidates, _From, _Delta.y);
                AddHorizontalCandidate(candidates, _From, _Delta.x);
                AddHorizontalCandidate(candidates, _From, 1);
                AddHorizontalCandidate(candidates, _From, -1);
                AddVerticalCandidate(candidates, _From, -_Delta.y);
            }

            return candidates;
        }

        private static void AddHorizontalCandidate(List<Vector2Int> _Candidates, Vector2Int _From, int _Direction)
        {
            if (_Direction == 0)
                return;

            AddUniqueCandidate(_Candidates, new Vector2Int(_From.x + (_Direction > 0 ? 1 : -1), _From.y));
        }

        private static void AddVerticalCandidate(List<Vector2Int> _Candidates, Vector2Int _From, int _Direction)
        {
            if (_Direction == 0)
                return;

            AddUniqueCandidate(_Candidates, new Vector2Int(_From.x, _From.y + (_Direction > 0 ? 1 : -1)));
        }

        private static void AddUniqueCandidate(List<Vector2Int> _Candidates, Vector2Int _Cell)
        {
            if (!_Candidates.Contains(_Cell))
                _Candidates.Add(_Cell);
        }

        private static Vector2Int GetFirstStep(Vector2Int _From, Vector2Int _Destination, Dictionary<Vector2Int, Vector2Int> _PreviousCells)
        {
            Vector2Int current = _Destination;
            while (_PreviousCells.TryGetValue(current, out Vector2Int previous) && previous != _From)
            {
                current = previous;
            }

            return current;
        }

        private Transform FindOrCreateTileRoot()
        {
            Transform tileRoot = transform.Find("Tiles");
            if (tileRoot != null)
                return tileRoot;

            tileRoot = new GameObject("Tiles").transform;
            tileRoot.SetParent(transform, false);
            return tileRoot;
        }

        private static string GetTileName(Vector2Int _Cell)
        {
            return "Tile " + _Cell.x + "," + _Cell.y;
        }

        private static void RemoveUnexpectedTiles(Transform _TileRoot, HashSet<string> _ExpectedTileNames)
        {
            for (int i = _TileRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _TileRoot.GetChild(i);
                if (_ExpectedTileNames.Contains(child.name))
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void ClearTileRoot()
        {
            Transform tileRoot = transform.Find("Tiles");
            if (tileRoot == null)
                return;

            if (Application.isPlaying)
                Destroy(tileRoot.gameObject);
            else
                DestroyImmediate(tileRoot.gameObject);
        }
    }
}
