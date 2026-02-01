using System.Collections.Generic;
using System.Text;
using Core;
using Managers;
using UnityEngine;

namespace Puzzles
{
    public class TentsPuzzleGenerator : MonoBehaviour
    {
        public const int EmptyCell = -1;

        public enum CellType
        {
            Empty,
            Tree,
            Tent
        }

        public struct CellState
        {
            public CellType Type;
            public int Color;

            public CellState(CellType type, int color)
            {
                Type = type;
                Color = color;
            }
        }

        [SerializeField]
        private GameObject spawnPoint;

        [SerializeField]
        private Vector3 spawnSpacing = Vector3.right * 1.5f;

        [Header("Config")]
        public int gridSize = 5;

        public int pairCount = 7;
        public int numColors = 4;
        public int seed = -1; // -1 = random seed
        public int maxAttempts = 30000;

        [Header("Rules")]
        [SerializeField] private bool allowDiagonalTentTouching = false;

        private System.Random rng;

        private CellState[,] tentState;
        private TestPuzzle currentPuzzle;
        private TentSolution currentSolution;
        private readonly Dictionary<GameObject, int> tentInstanceColors = new Dictionary<GameObject, int>();
        private readonly Dictionary<GameObject, Vector2Int> tentInstancePositions = new Dictionary<GameObject, Vector2Int>();

        [SerializeField]
        GridController gridController;

        [SerializeField]
        List<GameObject> treePrefabs;

        [SerializeField]
        List<GameObject> tentPrefabs;

        public GameObject SelectedTotem;

        EventManager eventManager;
        private bool levelWinTriggered;
        

        private void Start()
        {
            rng = (seed >= 0) ? new System.Random(seed) : new System.Random();
            eventManager = Services.Get<EventManager>();
            eventManager.gameObjectSelected += OnGameObjectSelected;
            eventManager.GridSelected += OnGridSelected;
            eventManager.AgentReachedDestination += OnAgentReachedDestination;
            GenerateAndLog();
        }

        private void OnGridSelected(Vector2Int gridLocation)
        {
            if (SelectedTotem != null)
            {
                if (gridController == null)
                {
                    Debug.LogError("GridController is not assigned.");
                    SelectedTotem = null;
                    return;
                }
                if(sameFrame)
                    return;
                sameFrame = true;

                if (!IsCellFree(gridLocation))
                {
                    Debug.Log($"Grid cell {gridLocation} is not free.");
                    return;
                }

                if (!TryGetTentColor(SelectedTotem, out int color))
                {
                    if (TryGetTentGridPosition(SelectedTotem, out var prevGridFromState))
                    {
                        var prevState = GetCellState(prevGridFromState);
                        if (prevState.Type == CellType.Tent)
                            color = prevState.Color;
                    }
                }

                if (color == EmptyCell)
                {
                    Debug.LogWarning($"Selected totem {SelectedTotem.name} has no known color.");
                    SelectedTotem = null;
                    return;
                }

                if (TryGetTentGridPosition(SelectedTotem, out var previousGrid))
                    UpdateCellState(previousGrid, CellType.Empty, EmptyCell);

                if (!UpdateCellState(gridLocation, CellType.Tent, color))
                {
                    Debug.LogWarning($"Failed to update grid state for {gridLocation}.");
                    SelectedTotem = null;
                    return;
                }

                Debug.Log($"Selected totem: {SelectedTotem.name} to move to {gridLocation}");
                var loc = gridController.GetGridLocation(gridLocation);
                var agent = SelectedTotem.GetComponent<AIAgent>();
                if (agent != null)
                    agent.SetDestination(loc);
                else
                    Debug.LogWarning($"Selected totem {SelectedTotem.name} has no AIAgent.");

                tentInstancePositions[SelectedTotem] = gridLocation;
                SelectedTotem = null;

            }
        }

        bool sameFrame = false;
        
        void Update()
        {
            sameFrame = false;
        }

        private void OnGameObjectSelected(Transform selected)
        {
            if(sameFrame)
                return;
            sameFrame = true;
            if (SelectedTotem != null)
                return;
            
            var aiagent = selected.GetComponent<AIAgent>();
            if (aiagent != null)
            {
                if (aiagent.Totem == TotemType.tent)
                {
                    SelectedTotem = selected.gameObject;
                    Debug.Log($"Selected tent: {SelectedTotem.name}");
                }
                else
                {
                    Debug.Log("Selected object is not a tent totem.");
                }
            }
        }

        public bool IsValidMovePosition(Vector2Int gridLocation)
        {
            if (!IsInBounds(gridLocation))
                return false;
            
            if(tentState[gridLocation.x, gridLocation.y].Type == CellType.Empty || tentState[gridLocation.x, gridLocation.y].Type == CellType.Tent)
                return true;
            return false;
        }
        
        public bool IsTentPlacementValid(Vector2Int gridLocation, int tentColor)
        {
            if (!IsInBounds(gridLocation))
                return false;

            if (tentState == null)
                return false;

            var state = tentState[gridLocation.x, gridLocation.y];
            if (state.Type != CellType.Empty)
                return false;

            foreach (var q in TentAdjacencyNeighbors(gridSize, gridLocation))
            {
                if (tentState[q.x, q.y].Type == CellType.Tent)
                    return false;
            }

            bool hasAdjacentTree = false;
            foreach (var q in OrthoNeighbors(gridSize, gridLocation))
            {
                var neighbor = tentState[q.x, q.y];
                if (neighbor.Type != CellType.Tree)
                    continue;

                hasAdjacentTree = true;
                if (neighbor.Color == tentColor)
                    return false;
            }

            return hasAdjacentTree;
        }

        public bool IsTentPositionValid(Vector2Int gridLocation, int tentColor)
        {
            if (!IsInBounds(gridLocation))
                return false;

            if (tentState == null)
                return false;

            var state = tentState[gridLocation.x, gridLocation.y];
            if (state.Type == CellType.Tree)
                return false;
            if (state.Type == CellType.Tent && state.Color != tentColor)
                return false;

            foreach (var q in TentAdjacencyNeighbors(gridSize, gridLocation))
            {
                if (q == gridLocation)
                    continue;
                if (tentState[q.x, q.y].Type == CellType.Tent)
                    return false;
            }

            bool hasAdjacentTree = false;
            foreach (var q in OrthoNeighbors(gridSize, gridLocation))
            {
                var neighbor = tentState[q.x, q.y];
                if (neighbor.Type != CellType.Tree)
                    continue;

                hasAdjacentTree = true;
                if (neighbor.Color == tentColor)
                    return false;
            }

            return hasAdjacentTree;
        }
        

        private void GenerateAndLog()
        {
            TestPuzzle testPuzzle = null;
            TentSolution tentSolution = null;
            bool validated = false;

            if (gridController != null && gridSize != gridController.GridSize)
            {
                Debug.Log(
                    $"TentsPuzzleGenerator gridSize overridden by GridController: {gridSize} -> {gridController.GridSize}");
                gridSize = gridController.GridSize;
            }

            if (tentPrefabs.Count < numColors)
            {
                Debug.LogError("Not enough tent prefabs for the number of colors specified.");
                return;
            }

            if (treePrefabs.Count < numColors)
            {
                Debug.LogError("Not enough tree prefabs for the number of colors specified.");
                return;
            }

            int maxNonTouchingTents = MaxNonTouchingTents(gridSize);
            if (pairCount > maxNonTouchingTents)
            {
                Debug.LogError($"Pair count {pairCount} is impossible for {gridSize}x{gridSize}. " +
                               $"Max tents without touching (including diagonals) is {maxNonTouchingTents}.");
                return;
            }

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (GenerateLayout(gridSize, pairCount, numColors, rng, out var treeColors, out var tents,
                        out var pairing))
                {
                    testPuzzle = new TestPuzzle(gridSize, treeColors);
                    tentSolution = new TentSolution(tents, pairing);
                    if (ValidateSolution(testPuzzle, tentSolution.tents))
                    {
                        validated = true;
                        break;
                    }
                }
            }

            if (testPuzzle == null || tentSolution == null)
            {
                Debug.LogError("Generation failed. Try more colors or fewer pairs.");
                return;
            }

            if (!validated)
            {
                Debug.LogError("Failed to validate a generated layout. Try more colors or fewer pairs.");
                return;
            }

            currentPuzzle = testPuzzle;
            currentSolution = tentSolution;
            InitializeTentState(gridSize, testPuzzle);
            levelWinTriggered = false;

            SpawnTrees(testPuzzle);
            StartCoroutine(SpawnTentsAfterDelay(testPuzzle, tentSolution, 2f));
            Debug.Log(RenderSolution(testPuzzle, tentSolution));
        }

        private int MaxNonTouchingTents(int n)
        {
            if (allowDiagonalTentTouching)
                return (n * n + 1) / 2;

            int k = (n + 1) / 2;
            return k * k;
        }

        private void InitializeTentState(int size, TestPuzzle puzzle)
        {
            tentState = new CellState[size, size];
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tentState[x, y] = new CellState(CellType.Empty, EmptyCell);

            if (puzzle == null)
                return;

            foreach (var kvp in puzzle.treeColors)
                tentState[kvp.Key.x, kvp.Key.y] = new CellState(CellType.Tree, kvp.Value);
        }

        public CellState GetCellState(Vector2Int gridPosition)
        {
            if (!IsInBounds(gridPosition))
                return new CellState(CellType.Empty, EmptyCell);

            if (tentState == null)
                return new CellState(CellType.Empty, EmptyCell);

            return tentState[gridPosition.x, gridPosition.y];
        }

        public bool IsCellFree(Vector2Int gridPosition)
        {
            if (!IsInBounds(gridPosition))
                return false;

            if (tentState == null)
                return true;

            return tentState[gridPosition.x, gridPosition.y].Type == CellType.Empty;
        }

        public bool UpdateCellState(Vector2Int gridPosition, CellType type, int color)
        {
            if (!IsInBounds(gridPosition))
                return false;

            if (tentState == null)
                InitializeTentState(gridSize, currentPuzzle);

            var existing = tentState[gridPosition.x, gridPosition.y];
            if (existing.Type == CellType.Tree && type != CellType.Tree)
                return false;

            tentState[gridPosition.x, gridPosition.y] = new CellState(type, color);

            if (type == CellType.Tent)
            {
                LogInvalidTentPlacement(gridPosition, color);
            }
            return true;
        }

        public bool UpdateTentState(Vector2Int gridPosition, int color)
        {
            return UpdateCellState(
                gridPosition,
                color == EmptyCell ? CellType.Empty : CellType.Tent,
                color);
        }

        public bool IsSolved()
        {
            if (currentPuzzle == null || tentState == null)
                return false;

            var tents = BuildTentDictionaryFromState();
            return ValidateSolution(currentPuzzle, tents);
        }

        private Dictionary<Vector2Int, int> BuildTentDictionaryFromState()
        {
            var tents = new Dictionary<Vector2Int, int>();
            for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
            {
                var state = tentState[x, y];
                if (state.Type == CellType.Tent && state.Color != EmptyCell)
                    tents[new Vector2Int(x, y)] = state.Color;
            }

            return tents;
        }

        private bool IsInBounds(Vector2Int gridPosition)
        {
            if (gridPosition.x < 0 || gridPosition.x >= gridSize)
                return false;
            if (gridPosition.y < 0 || gridPosition.y >= gridSize)
                return false;
            return true;
        }

        private bool TryGetTentColor(GameObject tent, out int color)
        {
            if (tentInstanceColors.TryGetValue(tent, out color))
                return true;

            color = EmptyCell;
            return false;
        }

        private bool TryGetTentGridPosition(GameObject tent, out Vector2Int gridPosition)
        {
            if (tentInstancePositions.TryGetValue(tent, out gridPosition))
                return true;

            if (gridController != null &&
                gridController.TryGetGridPositionFromWorld(tent.transform.position, out gridPosition))
                return true;

            gridPosition = Vector2Int.zero;
            return false;
        }

        // =======================
        // Data
        // =======================

        private class TestPuzzle
        {
            public int size;
            public Dictionary<Vector2Int, int> treeColors;

            public TestPuzzle(int size, Dictionary<Vector2Int, int> treeColors)
            {
                this.size = size;
                this.treeColors = treeColors;
            }
        }

        private class TentSolution
        {
            public Dictionary<Vector2Int, int> tents;
            public Dictionary<Vector2Int, Vector2Int> pairing;

            public TentSolution(Dictionary<Vector2Int, int> tents, Dictionary<Vector2Int, Vector2Int> pairing)
            {
                this.tents = tents;
                this.pairing = pairing;
            }
        }

        // =======================
        // Generator
        // =======================

        private bool GenerateLayout(
            int n,
            int pairs,
            int numColors,
            System.Random rng,
            out Dictionary<Vector2Int, int> treeColors,
            out Dictionary<Vector2Int, int> tents,
            out Dictionary<Vector2Int, Vector2Int> pairing)
        {
            tents = new Dictionary<Vector2Int, int>();
            treeColors = new Dictionary<Vector2Int, int>();
            pairing = new Dictionary<Vector2Int, Vector2Int>();

            var cells = new List<Vector2Int>();
            for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                cells.Add(new Vector2Int(r, c));

            Shuffle(cells, rng);

            foreach (var p in cells)
            {
                if (tents.Count >= pairs)
                    break;

                if (TentsConflict(n, tents, p))
                    continue;

                var adj = OrthoNeighbors(n, p);
                Shuffle(adj, rng);

                foreach (var tpos in adj)
                {
                    if (treeColors.ContainsKey(tpos) || tents.ContainsKey(tpos))
                        continue;

                    int treeColor = rng.Next(numColors);
                    if (SameColorTreeConflict(n, treeColors, tpos, treeColor))
                        continue;

                    var colors = new List<int>();
                    for (int i = 0; i < numColors; i++)
                        colors.Add(i);
                    Shuffle(colors, rng);

                    bool placed = false;
                    foreach (var tentColor in colors)
                    {
                        if (!TentColorInvalid(treeColors, p, tentColor, n, tpos, treeColor))
                        {
                            tents[p] = tentColor;
                            treeColors[tpos] = treeColor;
                            pairing[tpos] = p;
                            placed = true;
                            break;
                        }
                    }

                    if (placed)
                        break;
                }
            }

            return tents.Count == pairs;
        }

        // =======================
        // Validation
        // =======================

        private bool ValidateSolution(TestPuzzle puz, Dictionary<Vector2Int, int> tents)
        {
            int n = puz.size;
            var treeColors = puz.treeColors;

            var usedTents = new HashSet<Vector2Int>();

            foreach (var kvp in treeColors)
            {
                var treePos = kvp.Key;

                var neighbors = OrthoNeighbors(n, treePos);
                var matching = new List<Vector2Int>();

                foreach (var p in neighbors)
                {
                    if (tents.ContainsKey(p))
                    {
                        matching.Add(p);
                    }
                }

                if (matching.Count != 1)
                {
                    // Debug.Log($"Tree at {treePos} has wrong number of tents");
                    return false;
                }

                usedTents.Add(matching[0]);
            }

            var tentPositions = new HashSet<Vector2Int>(tents.Keys);
            foreach (var p in tentPositions)
            {
                foreach (var q in TentAdjacencyNeighbors(n, p))
                {
                    if (tentPositions.Contains(q) && q != p)
                    {
                        Debug.Log($"Tents touching at {p} and {q}");
                        return false;
                    }
                }
            }

            if (usedTents.Count != tentPositions.Count)
            {
                Debug.Log("Extra tent not assigned to a tree");
                return false;
            }

            foreach (var kvp in treeColors)
            {
                var pos = kvp.Key;
                int color = kvp.Value;

                foreach (var q in KingNeighbors(n, pos))
                {
                    if (treeColors.TryGetValue(q, out int other) && other == color && q != pos)
                    {
                        Debug.Log($"Same color trees touching at {pos} and {q}");
                        return false;
                    }
                }
            }

            foreach (var p in tentPositions)
            {
                if (treeColors.ContainsKey(p))
                {
                    Debug.Log($"Tent placed on tree at {p}");
                    return false;
                }
            }

            foreach (var kvp in tents)
            {
                var tentPos = kvp.Key;
                int tentColor = kvp.Value;

                foreach (var q in OrthoNeighbors(n, tentPos))
                {
                    if (treeColors.TryGetValue(q, out int treeColor) && treeColor == tentColor)
                    {
                        Debug.Log($"Tent at {tentPos} matches adjacent tree color");
                        return false;
                    }
                }
            }

            return true;
        }

        // =======================
        // Rendering
        // =======================

        private void SpawnTrees(TestPuzzle puz)
        {
            if (gridController == null)
            {
                Debug.LogError("GridController is not assigned.");
                return;
            }

            if (spawnPoint == null)
            {
                Debug.LogError("SpawnPoint is not assigned.");
                return;
            }

            int index = 0;
            for (int r = 0; r < puz.size; r++)
            {
                for (int c = 0; c < puz.size; c++)
                {
                    var pos = new Vector2Int(r, c);
                    if (!puz.treeColors.TryGetValue(pos, out int color))
                        continue;

                    if (color < 0 || color >= treePrefabs.Count)
                    {
                        Debug.LogWarning($"No tree prefab for color {color} at {pos}.");
                        continue;
                    }

                    GameObject treePrefab = treePrefabs[color];
                    if (treePrefab == null)
                    {
                        Debug.LogWarning($"Tree prefab for color {color} is null.");
                        continue;
                    }

                    Vector3 spawnOffset = spawnSpacing * index;
                    Vector3 startPos = spawnPoint.transform.position + spawnOffset;
                    GameObject instance = Instantiate(treePrefab, startPos, Quaternion.identity);
                    var agent = instance.GetComponent<AIAgent>();
                    if (agent != null)
                        agent.SetDestination(gridController.GetWorldCenter(pos));

                    index++;
                }
            }
        }

        private string RenderSolution(TestPuzzle puz, TentSolution sol)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SOLUTION");

            for (int r = 0; r < puz.size; r++)
            {
                for (int c = 0; c < puz.size; c++)
                {
                    var pos = new Vector2Int(r, c);

                    string cell = "..";
                    if (puz.treeColors.TryGetValue(pos, out int treeColor))
                        cell = $"T{treeColor}";
                    if (sol.tents.TryGetValue(pos, out int tentColor))
                        cell = $"^{tentColor}";

                    sb.Append(cell);
                    if (c < puz.size - 1)
                        sb.Append(' ');
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private void SpawnTents(TestPuzzle puz, TentSolution sol)
        {
            if (gridController == null)
            {
                Debug.LogError("GridController is not assigned.");
                return;
            }

            if (spawnPoint == null)
            {
                Debug.LogError("SpawnPoint is not assigned.");
                return;
            }

            tentInstanceColors.Clear();
            tentInstancePositions.Clear();

            int index = 0;
            foreach (var kvp in sol.tents)
            {
                var gridPos = kvp.Key;
                int color = kvp.Value;

                if (color < 0 || color >= tentPrefabs.Count)
                {
                    Debug.LogWarning($"No tent prefab for color {color} at {gridPos}.");
                    continue;
                }

                GameObject tentPrefab = tentPrefabs[color];
                if (tentPrefab == null)
                {
                    Debug.LogWarning($"Tent prefab for color {color} is null.");
                    continue;
                }

                Vector3 spawnOffset = spawnSpacing * index;
                Vector3 startPos = spawnPoint.transform.position + spawnOffset;
                var instance = Instantiate(tentPrefab, startPos, Quaternion.identity);

                var agent = instance.GetComponent<AIAgent>();
                if (agent == null)
                {
                    Debug.LogWarning($"Spawned tent at {gridPos} has no AIAgent.");
                    index++;
                    continue;
                }

                tentInstanceColors[instance] = color;

                index++;
            }
        }

        private System.Collections.IEnumerator SpawnTentsAfterDelay(TestPuzzle puz, TentSolution sol,
            float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            SpawnTents(puz, sol);
        }

        private void OnAgentReachedDestination(AIAgent agent)
        {
            if (levelWinTriggered)
                return;

            if (!IsSolved())
                return;

            if (!AreAllAgentsAtDestinations())
                return;

            TriggerLevelWin();
        }

        // =======================
        // Helpers
        // =======================

        private bool InBounds(int n, int r, int c) => r >= 0 && r < n && c >= 0 && c < n;

        private List<Vector2Int> OrthoNeighbors(int n, Vector2Int p)
        {
            var res = new List<Vector2Int>();
            int r = p.x,
                c = p.y;

            if (InBounds(n, r + 1, c))
                res.Add(new Vector2Int(r + 1, c));
            if (InBounds(n, r - 1, c))
                res.Add(new Vector2Int(r - 1, c));
            if (InBounds(n, r, c + 1))
                res.Add(new Vector2Int(r, c + 1));
            if (InBounds(n, r, c - 1))
                res.Add(new Vector2Int(r, c - 1));

            return res;
        }

        private List<Vector2Int> KingNeighbors(int n, Vector2Int p)
        {
            var res = new List<Vector2Int>();
            int r = p.x,
                c = p.y;

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0)
                        continue;
                    int rr = r + dr;
                    int cc = c + dc;
                    if (InBounds(n, rr, cc))
                        res.Add(new Vector2Int(rr, cc));
                }
            }

            return res;
        }

        private IEnumerable<Vector2Int> TentAdjacencyNeighbors(int n, Vector2Int p)
        {
            return allowDiagonalTentTouching ? OrthoNeighbors(n, p) : KingNeighbors(n, p);
        }

        private void LogInvalidTentPlacement(Vector2Int gridPosition, int tentColor)
        {
            bool hasAdjacentTree = false;
            bool invalid = false;

            // bool invalid = false;
            // List<Vector2Int> invalidPositons = new List<Vector2Int>();
            // invalidPositons.Add(gridPosition);
            
            foreach (var q in TentAdjacencyNeighbors(gridSize, gridPosition))
            {
                if (tentState[q.x, q.y].Type == CellType.Tent)
                {
                    Debug.Log($"Invalid: Tent at {gridPosition} touches tent at {q}");
                    // invalidPositons.Add(q);
                    invalid = true;
                }
            }

            foreach (var q in OrthoNeighbors(gridSize, gridPosition))
            {
                var neighbor = tentState[q.x, q.y];
                if (neighbor.Type != CellType.Tree)
                    continue;

                hasAdjacentTree = true;
                if (neighbor.Color == tentColor)
                {
                    Debug.Log($"Invalid: Tent at {gridPosition} matches adjacent tree color at {q}");
                    // invalidPositons.Add(q);
                    invalid = true;
                }
            }

            if (!hasAdjacentTree)
            {
                Debug.Log($"Invalid: Tent at {gridPosition} has no adjacent tree");
                invalid = true;
            }
            if (invalid)
                eventManager.invalidPosition?.Invoke(gridPosition);
            else
                eventManager.validPosition?.Invoke(gridPosition);
            // eventManager.invalidPositions?.Invoke(invalidPositons);
            // foreach( var x in invalidPositons) {
            //     Debug.Log(x.ToString());
            // }
        }

        private void TriggerLevelWin()
        {
            levelWinTriggered = true;
            Debug.Log("You win");
            eventManager?.LevelWin?.Invoke();
        }

        private bool AreAllAgentsAtDestinations()
        {
            var agents = FindObjectsOfType<AIAgent>();
            foreach (var agent in agents)
            {
                if (agent == null || !agent.isActiveAndEnabled)
                    continue;

                if (agent.HasDestination() && !agent.HasReachedDestination())
                    return false;
            }

            return true;
        }

        private bool TentsConflict(int n, Dictionary<Vector2Int, int> tents, Vector2Int p)
        {
            if (tents.ContainsKey(p))
                return true;

            foreach (var q in TentAdjacencyNeighbors(n, p))
            {
                if (tents.ContainsKey(q))
                    return true;
            }

            return false;
        }

        private bool SameColorTreeConflict(int n, Dictionary<Vector2Int, int> treeColors, Vector2Int p, int color)
        {
            foreach (var q in KingNeighbors(n, p))
            {
                if (treeColors.TryGetValue(q, out int other) && other == color)
                    return true;
            }

            return false;
        }

        private bool TentColorInvalid(
            Dictionary<Vector2Int, int> treeColors,
            Vector2Int tentPos,
            int tentColor,
            int n,
            Vector2Int newTreePos,
            int newTreeColor)
        {
            foreach (var q in OrthoNeighbors(n, tentPos))
            {
                if (q == newTreePos)
                {
                    if (newTreeColor == tentColor)
                        return true;
                    continue;
                }

                if (treeColors.TryGetValue(q, out int other) && other == tentColor)
                    return true;
            }

            return false;
        }

        private void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
