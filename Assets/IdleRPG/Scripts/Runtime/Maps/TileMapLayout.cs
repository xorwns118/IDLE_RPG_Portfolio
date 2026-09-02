using System.Collections.Generic;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Maps
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Idle RPG/Tile Map Layout")]
    public sealed class TileMapLayout : MonoBehaviour
    {
        private const int PathMoveCost = 10;
        private const int PathTurnCost = 2;

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

        public Bounds GetWorldBounds()
        {
            if (SettingsValue == null)
                return new Bounds(transform.position, Vector3.zero);

            Bounds localBounds = SettingsValue.GetLocalBounds();
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;
            Bounds worldBounds = new Bounds(transform.TransformPoint(min), Vector3.zero);
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(min.x, max.y, min.z)));
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(max.x, min.y, min.z)));
            worldBounds.Encapsulate(transform.TransformPoint(max));

            return worldBounds;
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
                return origin;

            int maxDistance = SettingsValue.Columns + SettingsValue.Rows;
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                for (int y = 0; y < SettingsValue.Rows; y++)
                {
                    for (int x = 0; x < SettingsValue.Columns; x++)
                    {
                        Vector2Int candidate = new Vector2Int(x, y);
                        if (GetCellDistance(origin, candidate) == distance && IsWalkable(candidate))
                            return candidate;
                    }
                }
            }

            return origin;
        }

        public int GetAttackRangeInCells(float _AttackRange)
        {
            return GetAttackRangeInCells(_AttackRange, 0f);
        }

        public int GetAttackRangeInCells(float _AttackRange, float _AttackRangePadding)
        {
            Vector2 cellSize = SettingsValue.GetSafeCellSize();
            float cellStep = Mathf.Max(0.01f, Mathf.Min(cellSize.x, cellSize.y));
            float range = Mathf.Max(0.01f, _AttackRange + Mathf.Max(0f, _AttackRangePadding));
            return Mathf.Max(1, Mathf.FloorToInt(range / cellStep));
        }

        public Vector2Int GetNextCellToward(Vector2Int _From, Vector2Int _Target, int _StopDistance)
        {
            Vector2Int from = ClampCell(_From);
            List<Vector2Int> path = new List<Vector2Int>();
            if (!TryGetPathToward(from, _Target, _StopDistance, path) || path.Count == 0)
                return from;

            return path[0];
        }

        public bool TryGetPathToward(Vector2Int _From, Vector2Int _Target, int _StopDistance, List<Vector2Int> _Path)
        {
            if (_Path == null)
                return false;

            _Path.Clear();
            Vector2Int from = ClampCell(_From);
            Vector2Int target = ClampCell(_Target);
            int stopDistance = Mathf.Max(1, _StopDistance);
            if (GetCellDistance(from, target) <= stopDistance)
                return true;

            Dictionary<Vector2Int, Vector2Int> previousCells = new Dictionary<Vector2Int, Vector2Int>();
            Vector2Int destination = FindPathDestination(from, target, stopDistance, previousCells);
            if (destination == from)
                return false;

            BuildPath(from, destination, previousCells, _Path);
            return _Path.Count > 0;
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

        private Vector2Int FindPathDestination(
            Vector2Int _From,
            Vector2Int _Target,
            int _StopDistance,
            Dictionary<Vector2Int, Vector2Int> _PreviousCells)
        {
            List<Vector2Int> openCells = new List<Vector2Int> { _From };
            HashSet<Vector2Int> closedCells = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, int> movementCosts = new Dictionary<Vector2Int, int>
            {
                [_From] = 0
            };
            Dictionary<Vector2Int, int> pathScores = new Dictionary<Vector2Int, int>
            {
                [_From] = GetPathHeuristic(_From, _Target, _StopDistance)
            };
            Dictionary<Vector2Int, Vector2Int> pathDirections = new Dictionary<Vector2Int, Vector2Int>
            {
                [_From] = Vector2Int.zero
            };

            while (openCells.Count > 0)
            {
                Vector2Int current = PopBestOpenCell(openCells, pathScores, movementCosts, _Target);
                if (current != _From && GetCellDistance(current, _Target) <= _StopDistance)
                    return current;

                closedCells.Add(current);
                Vector2Int currentDirection = pathDirections.TryGetValue(current, out Vector2Int direction)
                    ? direction
                    : Vector2Int.zero;
                List<Vector2Int> candidates = BuildStepCandidates(current, _Target - current, currentDirection);

                for (int i = 0; i < candidates.Count; i++)
                {
                    Vector2Int next = ClampCell(candidates[i]);
                    if (next == current || closedCells.Contains(next) || !IsWalkable(next))
                        continue;

                    Vector2Int nextDirection = GetStepDirection(current, next);
                    int nextMovementCost = movementCosts[current] + PathMoveCost + GetTurnCost(currentDirection, nextDirection);
                    if (movementCosts.TryGetValue(next, out int existingCost) && nextMovementCost >= existingCost)
                        continue;

                    _PreviousCells[next] = current;
                    pathDirections[next] = nextDirection;
                    movementCosts[next] = nextMovementCost;
                    pathScores[next] = nextMovementCost + GetPathHeuristic(next, _Target, _StopDistance);
                    if (!openCells.Contains(next))
                        openCells.Add(next);
                }
            }

            return _From;
        }

        private Vector2Int PopBestOpenCell(
            List<Vector2Int> _OpenCells,
            Dictionary<Vector2Int, int> _PathScores,
            Dictionary<Vector2Int, int> _MovementCosts,
            Vector2Int _Target)
        {
            int bestIndex = 0;
            Vector2Int bestCell = _OpenCells[0];
            int bestScore = GetScore(_PathScores, bestCell);
            int bestDistance = GetCellDistance(bestCell, _Target);
            int bestMovementCost = GetScore(_MovementCosts, bestCell);

            for (int i = 1; i < _OpenCells.Count; i++)
            {
                Vector2Int candidate = _OpenCells[i];
                int candidateScore = GetScore(_PathScores, candidate);
                int candidateDistance = GetCellDistance(candidate, _Target);
                int candidateMovementCost = GetScore(_MovementCosts, candidate);
                if (!IsBetterPathCandidate(
                    candidateScore,
                    candidateDistance,
                    candidateMovementCost,
                    bestScore,
                    bestDistance,
                    bestMovementCost))
                    continue;

                bestIndex = i;
                bestCell = candidate;
                bestScore = candidateScore;
                bestDistance = candidateDistance;
                bestMovementCost = candidateMovementCost;
            }

            _OpenCells.RemoveAt(bestIndex);
            return bestCell;
        }

        private static bool IsBetterPathCandidate(
            int _CandidateScore,
            int _CandidateDistance,
            int _CandidateMovementCost,
            int _BestScore,
            int _BestDistance,
            int _BestMovementCost)
        {
            if (_CandidateScore != _BestScore)
                return _CandidateScore < _BestScore;

            if (_CandidateDistance != _BestDistance)
                return _CandidateDistance < _BestDistance;

            return _CandidateMovementCost < _BestMovementCost;
        }

        private static int GetScore(Dictionary<Vector2Int, int> _Scores, Vector2Int _Cell)
        {
            return _Scores.TryGetValue(_Cell, out int score) ? score : int.MaxValue;
        }

        private int GetPathHeuristic(Vector2Int _Cell, Vector2Int _Target, int _StopDistance)
        {
            return Mathf.Max(0, GetCellDistance(_Cell, _Target) - _StopDistance) * PathMoveCost;
        }

        private static int GetTurnCost(Vector2Int _CurrentDirection, Vector2Int _NextDirection)
        {
            if (_CurrentDirection == Vector2Int.zero || _CurrentDirection == _NextDirection)
                return 0;

            return PathTurnCost;
        }

        private static Vector2Int GetStepDirection(Vector2Int _From, Vector2Int _To)
        {
            Vector2Int delta = _To - _From;
            return new Vector2Int(Mathf.Clamp(delta.x, -1, 1), Mathf.Clamp(delta.y, -1, 1));
        }

        private static void BuildPath(
            Vector2Int _From,
            Vector2Int _Destination,
            Dictionary<Vector2Int, Vector2Int> _PreviousCells,
            List<Vector2Int> _Path)
        {
            Vector2Int current = _Destination;
            while (current != _From)
            {
                _Path.Add(current);
                if (!_PreviousCells.TryGetValue(current, out Vector2Int previous))
                    break;

                current = previous;
            }

            _Path.Reverse();
        }

        private static List<Vector2Int> BuildStepCandidates(
            Vector2Int _From,
            Vector2Int _Delta,
            Vector2Int _CurrentDirection)
        {
            List<Vector2Int> candidates = new List<Vector2Int>(4);
            if (_CurrentDirection != Vector2Int.zero)
                AddUniqueCandidate(candidates, _From + _CurrentDirection);

            AddGoalBiasedStepCandidates(candidates, _From, _Delta);
            return candidates;
        }

        private static void AddGoalBiasedStepCandidates(List<Vector2Int> _Candidates, Vector2Int _From, Vector2Int _Delta)
        {
            bool preferHorizontal = Mathf.Abs(_Delta.x) >= Mathf.Abs(_Delta.y);
            if (preferHorizontal)
            {
                AddHorizontalCandidate(_Candidates, _From, _Delta.x);
                AddVerticalCandidate(_Candidates, _From, _Delta.y);
                AddVerticalCandidate(_Candidates, _From, 1);
                AddVerticalCandidate(_Candidates, _From, -1);
                AddHorizontalCandidate(_Candidates, _From, -_Delta.x);
            }
            else
            {
                AddVerticalCandidate(_Candidates, _From, _Delta.y);
                AddHorizontalCandidate(_Candidates, _From, _Delta.x);
                AddHorizontalCandidate(_Candidates, _From, 1);
                AddHorizontalCandidate(_Candidates, _From, -1);
                AddVerticalCandidate(_Candidates, _From, -_Delta.y);
            }
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

        private Transform FindOrCreateTileRoot()
        {
            Transform tileRoot = transform.Find("Tiles");
            if (tileRoot != null)
                return tileRoot;

            GameObject tileRootObject = new GameObject("Tiles");
            tileRootObject.transform.SetParent(transform, false);
            return tileRootObject.transform;
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
    }
}
