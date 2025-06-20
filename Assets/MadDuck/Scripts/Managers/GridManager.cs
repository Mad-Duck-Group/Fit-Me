using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils;
using Microsoft.Unity.VisualStudio.Editor;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityCommunity.UnitySingleton;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace MadDuck.Scripts.Managers
{
    [Flags]
    public enum GridType
    {
        None = 0,
        Rectangle = 1 << 0,
        Custom = 1 << 1,
        All = Rectangle | Custom
    }
    
    [RequireComponent(typeof(Grid))]
    [ShowOdinSerializedPropertiesInInspector]
    public class GridManager : MonoSingleton<GridManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspector

        private enum GridOffsetType
        {
            Automatic,
            Custom
        }

        [Flags]
        private enum EndlessType
        {
            None = 0,
            Preset = 1 << 0,
            Generated = 1 << 1,
            All = Preset | Generated
        }
        
        [Title("Grid References")]
        [SerializeField] private Cell cellPrefab;
        [SerializeField] private Transform cellParent;
        [SerializeField] private Color whiteColor;
        [SerializeField] private Color blackColor;
        [SerializeField] private Color canBePlacedColor;
        [SerializeField] private Color cannotBePlacedColor;
        [SerializeField] private List<GridPreset> gridPresets = new();
        
        [TitleGroup("Grid Settings")]
        [SerializeField]
        [ValidateInput("@endlessType != EndlessType.None", "Endless type cannot be None")]
        private EndlessType endlessType = EndlessType.All;
        [TitleGroup("Grid Settings")]
        [SerializeField] [ValidateInput("@generatedGridType != GridType.None", "Grid type cannot be None")]
        [ShowIf("@endlessType.HasFlag(EndlessType.Generated)")]
        private GridType generatedGridType = GridType.Rectangle;
        [TitleGroup("Grid Settings")]
        [SerializeField] [MinMaxSlider(1, 20, ShowFields = true)]
        private Vector2Int randomGridXRange = new(1, 10);
        [TitleGroup("Grid Settings")]
        [SerializeField] [MinMaxSlider(1, 20, ShowFields = true)]
        private Vector2Int randomGridYRange = new(1, 10);
        [TitleGroup("Grid Settings")]
        [SerializeField] [MinMaxSlider(1, 20, ShowFields = true)] 
        [ShowIf("@generatedGridType.HasFlag(GridType.Custom)")]
        private Vector2Int bridgeWidthRange = new(2, 3);
        [TitleGroup("Grid Settings")]
        [SerializeField] [OnValueChanged(nameof(UpdateGridOffset))]
        private GridOffsetType gridHorizontalOffsetType = GridOffsetType.Automatic;
        [TitleGroup("Grid Settings")]
        [SerializeField, ShowIf(nameof(gridHorizontalOffsetType), GridOffsetType.Custom)]
        [OnValueChanged(nameof(UpdateGridOffset))]
        private int customOffsetX = 0;
        [TitleGroup("Grid Settings")]
        [SerializeField] [OnValueChanged(nameof(UpdateGridOffset))]
        private GridOffsetType gridVerticalOffsetType = GridOffsetType.Custom;
        [SerializeField, ShowIf(nameof(gridVerticalOffsetType), GridOffsetType.Custom)]
        [OnValueChanged(nameof(UpdateGridOffset))]
        private int customOffsetY = 0;
        [TitleGroup("Grid Settings")]
        [SerializeField]
        private int destroyThreshold = 3;

        [Title("Grid Debug")] 
        [SerializeField, DisableInPlayMode] [OnValueChanged(nameof(OnPresetChanged))]
        [InlineEditor]
        private GridPreset currentGridPreset;
        [Button("Refresh Grid Size")]
        private void OnPresetChanged()
        {
            currentGridSize = currentGridPreset ? currentGridPreset.GridSize : new Vector2Int(6, 8);
        }
        [SerializeField, Sirenix.OdinInspector.ReadOnly] [OnValueChanged(nameof(UpdateGridOffset))]
        [MinValue(1)]
        private Vector2Int currentGridSize = new(10, 10);
        [SerializeField, Sirenix.OdinInspector.ReadOnly] 
        private Vector2Int currentOffset = new(0, 0);
        [TableMatrix(SquareCells = true, HorizontalTitle = "Cell Array", IsReadOnly = true,
            DrawElementMethod = nameof(DrawCellArrayMatrix), Transpose = true)]
        [SerializeField] private Cell[,] _cellArray = {};
        [TableMatrix(SquareCells = true, HorizontalTitle = "Vacant Schema", IsReadOnly = true,
            DrawElementMethod = nameof(DrawVacantSchemaMatrix), Transpose = true)]
        [SerializeField] private int[,] _vacantSchema = {};
        [field: SerializeField, Sirenix.OdinInspector.ReadOnly] 
        public List<Block> BlocksOnGrid { get; private set; } = new();
        [SerializeField, ShowIf("@currentGridPreset && currentGridPreset.PresetGridType.HasFlag(GridType.Custom)")]
        private bool drawAllCustomGridCells = true;

        [Title("Infected Debug")]
        [field: SerializeField, Sirenix.OdinInspector.ReadOnly] public float RandomInfectedTime { get; private set; }
        [SerializeField, Sirenix.OdinInspector.ReadOnly] private List<Block> infectedBlocks = new();
        private void UpdateGridOffset()
        {
            switch (gridHorizontalOffsetType)
            {
                case GridOffsetType.Automatic:
                    currentOffset.x = -Mathf.FloorToInt(currentGridSize.x / 2f);
                    if (currentGridSize.x % 2 != 0)
                    {
                        if (!_grid) _grid = GetComponent<Grid>();
                        transform.SetPositionX(-_grid.cellSize.x / 2f);
                    }
                    else
                    {
                        transform.SetPositionX(0f);
                    }
                    break;
                case GridOffsetType.Custom:
                    currentOffset.x = customOffsetX;
                    break;
            }
            
            switch (gridVerticalOffsetType)
            {
                case GridOffsetType.Automatic:
                    currentOffset.y = Mathf.FloorToInt(currentGridSize.y / 2f);
                    break;
                case GridOffsetType.Custom:
                    currentOffset.y = customOffsetY;
                    break;
            }
        }
        #endregion

        #region Fields and Properties
        private Grid _grid;
        private List<Cell> _previousValidationCells = new();
        public static event Action<Block> OnBlockInfected;
        public static event Action<Block> OnBlockDisinfected;
        public Grid Grid => _grid;
        #endregion
        
        #region Events
        private void OnEnable()
        {
            GameManager.OnSceneActivated += OnSceneActivated;
        }

        private void OnDisable()
        {
            GameManager.OnSceneActivated -= OnSceneActivated;
        }
        
        void OnSceneActivated()
        {
            CreateCells();
        }
        #endregion
        
        #region Initialization
        protected override void Awake()
        {
            base.Awake();
            _grid = GetComponent<Grid>();
            if (!_grid.cellSize.x.Equals(_grid.cellSize.y))
            {
                Debug.LogError("Grid cell size must be the same in both axes!");
            }
            RandomInfectedTime = Random.Range(GameManager.Instance.InfectionTimeRange.x, GameManager.Instance.InfectionTimeRange.y);
        }
        #endregion
        
        #region Grid Generation
        private void SetUpGridPreset()
        {
            var currentEndlessType = endlessType;
            if (endlessType is EndlessType.All)
            {
                currentEndlessType = Random.Range(0, 2) == 0 ? EndlessType.Preset : EndlessType.Generated;
            }
            if (currentEndlessType is EndlessType.Preset && gridPresets.Count > 0)
            {
                currentGridPreset = gridPresets.GetRandomElement();
                return;
            }

            var newGridPreset = ScriptableObject.CreateInstance<GridPreset>();
            newGridPreset.name = "Auto-Generated Grid Preset";
            if (generatedGridType is GridType.All) newGridPreset.PresetGridType = Random.Range(0, 2) == 0 ? GridType.Rectangle : GridType.Custom;
            else newGridPreset.PresetGridType = generatedGridType;
            int randomX = Random.Range(randomGridXRange.x, randomGridXRange.y + 1);
            int randomY = Random.Range(randomGridYRange.x, randomGridYRange.y + 1);
            newGridPreset.GridSize = new Vector2Int(randomX, randomY);
            if (newGridPreset.PresetGridType is GridType.Custom)
            {
                newGridPreset.CustomGrid = new bool[randomY, randomX];
                var row = newGridPreset.CustomGrid.GetLength(0);
                var column = newGridPreset.CustomGrid.GetLength(1);
                for (int x = 0; x < row; x++)
                {
                    var hasBridge = Random.Range(0, 2) == 0;
                    var bridgeIndices = new bool[column];
                    if (hasBridge)
                    {
                        var bridgeWidth = Random.Range(bridgeWidthRange.x, bridgeWidthRange.y + 1);
                        bridgeIndices = GetBridgeIndex(bridgeWidth, column);
                    }
                    for (int y = 0; y < column; y++)
                    {
                        if (hasBridge) newGridPreset.CustomGrid[x, y] = bridgeIndices[y];
                        else newGridPreset.CustomGrid[x, y] = true;
                    }
                }
            }
            currentGridPreset = newGridPreset;
        }

        private bool[] GetBridgeIndex(int bridgeWidth, int columnCount)
        {
            var divisible = columnCount % bridgeWidth == 0 ? 0 : 1;
            var maxBridge = Mathf.FloorToInt(columnCount / (float)bridgeWidth) + divisible;
            var bridgeCount = Random.Range(1, maxBridge);
            var possibleRanges = new List<(int start, int end)>();
            for (var i = 0; i <= columnCount - bridgeWidth; i++)
            {
                possibleRanges.Add((i, i + bridgeWidth - 1));
            }
            var shuffledRanges = possibleRanges.Shuffled().ToList();
            //pick the ones where there are no overlap
            var validRanges = new List<(int start, int end)>();
            foreach (var range in shuffledRanges)
            {
                validRanges.Add(range);
                //TODO: Make a tree search
                var nonOverlapRange = shuffledRanges.Where(x => 
                        range.start > x.end || range.end < x.start).Take(bridgeCount);
                validRanges.AddRange(nonOverlapRange);
                if (validRanges.Count >= bridgeCount) break;
                validRanges.Clear();
            }
            //create bool array from validRanges
            var bridgeIndices = new bool[columnCount];
            if (validRanges.Count == 0)
            {
                Debug.LogWarning("No valid ranges found, creating a full bridge.");
                for (var i = 0; i < columnCount; i++)
                {
                    bridgeIndices[i] = true;
                }
                return bridgeIndices;
            }
            foreach (var range in validRanges)
            {
                Debug.Log($"Adding bridge from {range.start} to {range.end}");
                for (var i = range.start; i <= range.end; i++)
                {
                    bridgeIndices[i] = true;
                }
            }
            return bridgeIndices;
        }
        public void RegenerateGrid()
        {
            Debug.Log("Regenerating grid...");
            ResetPreviousValidationCells();
            foreach (var cell in _cellArray)
            {
                if (cell) Destroy(cell.gameObject);
            }
            _cellArray = new Cell[0, 0];
            _vacantSchema = new int[0, 0];
            CreateCells();
        }
        
        /// <summary>
        /// Create the cells
        /// </summary>
        private void CreateCells()
        {
            SetUpGridPreset();
            currentGridSize = currentGridPreset.GridSize;
            UpdateGridOffset();
            var row = currentGridSize.y;
            var column = currentGridSize.x;
            var cellSize = _grid.cellSize.x;
            _cellArray = new Cell[row, column];
            for (int x = 0; x < row; x++)
            {
                for (int y = 0; y < column; y++)
                {
                    if (currentGridPreset.PresetGridType is GridType.Custom && !currentGridPreset.CustomGrid[x, y]) continue; 
                    var halfSize = cellSize / 2;
                    Vector3 spawnPosition =
                        (Vector3)(new Vector2(halfSize, halfSize) +
                                  new Vector2(y + currentOffset.x, currentOffset.y - x) * cellSize) +
                        transform.position;
                    _cellArray[x, y] = Instantiate(cellPrefab, spawnPosition, Quaternion.identity, cellParent);
                    _cellArray[x, y].transform.localScale = Vector3.one * cellSize;
                    _cellArray[x, y].name = $"Cell {x}_{y}";
                    _cellArray[x, y].ArrayIndex = new Vector2Int(x, y);
                    _cellArray[x, y].GridIndex = new Vector2Int(y + currentOffset.x, currentOffset.y - x);
                
                    //Chessboard Pattern
                    if (x % 2 == 0)
                    {
                        _cellArray[x, y].SpriteRenderer.color =
                            y % 2 == 0 ? blackColor : whiteColor;
                        _cellArray[x, y].OriginalColor = _cellArray[x, y].SpriteRenderer.color;
                    }
                    else
                    {
                        _cellArray[x, y].SpriteRenderer.color =
                            y % 2 == 0 ? whiteColor : blackColor;
                        _cellArray[x, y].OriginalColor = _cellArray[x, y].SpriteRenderer.color;
                    }
                }
            }
        }
        #endregion
        
        #region Blocks
        /// <summary>
        /// Validate the placement of the block and change the color of the cells
        /// </summary>
        /// <param name="block">Block to validate</param>
        /// <returns>true if the placement is valid, false otherwise</returns>
        public bool ValidatePlacement(Block block)
        {
            List<Cell> cells = new List<Cell>();
            foreach (var atom in block.Atoms)
            {
                Vector3 atomPosition = atom.transform.position;
                Vector3 cellPosition = new Vector3(atomPosition.x, atomPosition.y, 0);
                Cell cell = GetCellByPosition(cellPosition);
                if (cell == null || cell.CurrentAtom != null)
                {
                    continue;
                }
                cells.Add(cell);
            }
            if (_previousValidationCells.Count > 0)
            {
                ResetPreviousValidationCells();
            }
            _previousValidationCells = cells;
            if (cells.Count < block.Atoms.Length)
            {
                cells.ForEach(cell => cell.SpriteRenderer.color = cannotBePlacedColor);
                return false;
            }
            cells.ForEach(cell => cell.SpriteRenderer.color = canBePlacedColor);
            return true;
        }
        
        /// <summary>
        /// Place the block in the grid
        /// </summary>
        /// <param name="block">Block to place</param>
        /// <returns>true if the placement is valid, false otherwise</returns>
        public bool PlaceBlock(Block block)
        {
            var cellSize = _grid.cellSize.x;
            block.transform.localScale = Vector3.one * cellSize;
            Vector3 atomPositionBeforePlacement = block.Atoms[0].transform.position;
            List<Cell> cells = new List<Cell>();
            foreach (var atom in block.Atoms)
            {
                Vector3 atomPosition = atom.transform.position;
                Vector3 cellPosition = new Vector3(atomPosition.x, atomPosition.y, 0);
                Cell cell = GetCellByPosition(cellPosition);
                if (!cell || cell.CurrentAtom)
                {
                    return false;
                }
                cells.Add(cell);
            }
            for (var i = 0; i < block.Atoms.Length; i++)
            {
                var atom = block.Atoms[i];
                cells[i].SetAtom(atom);
            }
            Vector3 atomPositionAfterPlacement = cells[0].transform.position;
            Vector3 blockPositionRelativeToAtom = atomPositionAfterPlacement - atomPositionBeforePlacement;
            block.transform.position += blockPositionRelativeToAtom;
            block.transform.SetParent(transform);
            block.BlockCells = cells;
            BlocksOnGrid.Add(block);
            GameManager.Instance.AddScore(ScoreTypes.Placement);
            ResetPreviousValidationCells();
            if (!UpdateBlockOnGrid(block))
            {
                RandomBlockManager.Instance.FreeSpawnPoint(block.SpawnIndex);
                RandomBlockManager.Instance.SpawnRandomBlock();
            }
            else
            {
                RandomBlockManager.Instance.ResetSpawnPoint();
                RandomBlockManager.Instance.SpawnRandomBlock();
            }
            return true;
        }

        /// <summary>
        /// Update the block on the grid, check for contacts and validate placement
        /// </summary>
        /// <param name="block"></param>
        /// <returns>true if Fit Me, false otherwise</returns>
        public bool UpdateBlockOnGrid(Block block)
        {
            if (!CreateVacantSchema(out var vacantSchema)) //Fit Me!
            {
                _vacantSchema = vacantSchema;
                GameManager.Instance.AddScore(ScoreTypes.FitMe);
                RemoveAllBlocks(true);
                RegenerateGrid();
                return true;
            }
            var contacts = new List<Block>();
            if (!CheckForContact(block, contacts)) return false;
            GameManager.Instance.AddScore(ScoreTypes.Combo, contacts.Count);
            GameManager.Instance.AddScore(ScoreTypes.Bomb, contacts.Count);
            contacts.ForEach(b => RemoveBlock(b, true));
            return false;
        }

        /// <summary>
        /// Remove the block from the grid
        /// </summary>
        /// <param name="block">Block to remove</param>
        /// <param name="destroy">Destroy the block, false by default</param>
        public void RemoveBlock(Block block, bool destroy = false)
        {
            foreach (var atom in block.Atoms)
            {
                Cell cell = GetCellByPosition(atom.transform.position);
                if (!cell || cell.CurrentAtom != atom)
                {
                    continue;
                }
                cell.SetAtom(null);
            }
            DisinfectBlock(block);
            BlocksOnGrid.Remove(block);
            if (destroy)
            {
                Destroy(block.gameObject);
            }
        }
    
        /// <summary>
        /// Remove all blocks from the grid
        /// </summary>
        /// <param name="destroy">Destroy the blocks, false by default</param>
        public void RemoveAllBlocks(bool destroy = false)
        {
            List<Block> blocksToRemove = new List<Block>(BlocksOnGrid);
            foreach (var block in blocksToRemove)
            {
                RemoveBlock(block, destroy);
            }
        }
        
        /// <summary>
        /// Reset the color of the previous validation cells
        /// </summary>
        public void ResetPreviousValidationCells()
        {
            if (_previousValidationCells.Count == 0) return;
            _previousValidationCells.ForEach(cell => cell.SpriteRenderer.color = cell.OriginalColor);
            _previousValidationCells.Clear();
        }
        #endregion
    
        #region Contacts
        /// <summary>
        /// Check for contact with other blocks
        /// </summary>
        /// <param name="block">Current block</param>
        /// <param name="contactedBlocks">List of contacted blocks</param>
        /// <returns>true if the contacted blocks count is greater than or equal to the destroy threshold, false otherwise</returns>
        private bool CheckForContact(Block block, List<Block> contactedBlocks)
        {
            BlockTypes currentType = block.BlockType;
            contactedBlocks.Add(block);
            foreach (var cell in block.BlockCells)
            {
                var upCell = GetCellByArrayIndex(cell.ArrayIndex[0] - 1, cell.ArrayIndex[1]);
                var downCell = GetCellByArrayIndex(cell.ArrayIndex[0] + 1, cell.ArrayIndex[1]);
                var leftCell = GetCellByArrayIndex(cell.ArrayIndex[0], cell.ArrayIndex[1] - 1);
                var rightCell = GetCellByArrayIndex(cell.ArrayIndex[0], cell.ArrayIndex[1] + 1);
                var adjacentCells = new List<Cell> {upCell, downCell, leftCell, rightCell};
                foreach (var adjacentCell in adjacentCells)
                {
                    if (!adjacentCell || !adjacentCell.CurrentAtom) continue;
                    var adjacentBlock = adjacentCell.CurrentAtom.ParentBlock;
                    if (adjacentBlock.BlockState == BlockState.Infected) continue;
                    if (adjacentBlock.BlockType != currentType) continue;
                    if (contactedBlocks.Contains(adjacentBlock)) continue;
                    CheckForContact(adjacentBlock, contactedBlocks);
                }
            }
            return contactedBlocks.Count >= destroyThreshold;
        }

        /// <summary>
        /// Create a schema of the vacant cells, 1 is vacant, 0 is occupied
        /// </summary>
        /// <returns>true if there are vacant cells, false otherwise</returns>
        public bool CreateVacantSchema(out int[,] vacantSchema)
        {
            var row = currentGridSize.y;
            var column = currentGridSize.x;
            vacantSchema = new int[row, column];
            bool isVacant = false;
            for (int x = 0; x < row; x++)
            {
                for (int y = 0; y < column; y++)
                {
                    var cell = _cellArray[x, y];
                    if (!cell) continue;
                    if (cell.CurrentAtom) continue;
                    vacantSchema[x, y] = 1;
                    isVacant = true;
                }
            }
            //ArrayHelper.PrintSchema(_vacantSchema);
            return isVacant;
        }

        /// <summary>
        /// Check if the block can be placed in the grid
        /// </summary>
        /// <param name="blockToCheck">Blocks to check</param>
        /// <param name="availableBlocks">Available blocks</param>
        /// <returns>true if the block can be placed, false otherwise</returns>
        public bool CheckAvailableBlock(List<Block> blockToCheck, out List<Block> availableBlocks)
        {
            CreateVacantSchema(out var vacantSchema);
            _vacantSchema = vacantSchema;
            availableBlocks = new List<Block>();
            foreach (var block in blockToCheck)
            {
                if (CompareSchema(block, block.transform.eulerAngles.z))
                {
                    availableBlocks.Add(block);
                    continue;
                }
                Debug.Log("Block " + block.name + " cannot be placed");
            }
            if (availableBlocks.Count != 0) return true;
            Debug.Log("No blocks can be placed");
            return false;
        }

        /// <summary>
        /// Compare the schema of the block with the vacant schema
        /// </summary>
        /// <param name="block">Block to compare</param>
        /// <param name="currentZEulerAngle">current Z euler angle of the block</param>
        /// <returns>true if the block can be placed, false otherwise</returns>
        private bool CompareSchema(Block block, float currentZEulerAngle)
        {
            var index = (int)currentZEulerAngle / 90;
            Debug.Log("Block " + block.name + " angle: " + currentZEulerAngle + ", index: " + index);
            if (ArrayHelper.CanBFitInA(_vacantSchema, block.BlockSchemas[index].schema, out _))
            {
                Debug.Log("Block " + block.name + " can be placed");
                return true;
            }
            return false;
        }
        
        public bool CompareSchema(Block block, int schemaIndex)
        {
            if (schemaIndex < 0 || schemaIndex >= block.BlockSchemas.Count)
            {
                Debug.LogError("Invalid schema index: " + schemaIndex);
                return false;
            }
            return CompareSchema(block, schemaIndex * 90f);
        }
        #endregion
        
        #region Infection

        public void InfectBlock(Block block)
        {
            if (GameManager.Instance.CurrentGameState.Value is not (GameState.PlaceBlock or GameState.UseItem)) return;
            infectedBlocks.Add(block);
            block.Infect();
            OnBlockInfected?.Invoke(block);
            RandomInfectedTime = Random.Range(GameManager.Instance.InfectionTimeRange.x, GameManager.Instance.InfectionTimeRange.y);
        }

        public void DisinfectBlock(Block block)
        {
            if (GameManager.Instance.CurrentGameState.Value is not (GameState.PlaceBlock or GameState.UseItem)) return;
            block.Disinfect();
            OnBlockDisinfected?.Invoke(block);
            infectedBlocks.Remove(block);
        }
        
        public async UniTask InfectRandomBlock()
        {
            if (blocksOnGrid.Count == 0) return;
            Block block = blocksOnGrid.GetRandomElement();
            
            if (block.BlockState != BlockState.Normal) return;
            block.PreInfect();
        }

        public void InfectAdjacentBlocks(Block sourceBlock)
        {
            if (!sourceBlock || sourceBlock.BlockState is not (BlockState.Infected or BlockState.PreInfected)) return;

            var candidatesForInfection = new List<Block>();

            foreach (var atom in sourceBlock.Atoms)
            {
                Cell cell = GetCellByPosition(atom.transform.position);
                if (!cell) continue;

                int x = cell.ArrayIndex[0];
                int y = cell.ArrayIndex[1];
                
                Cell[] adjacentCells =
                {
                    GetCellByArrayIndex(x - 1, y),
                    GetCellByArrayIndex(x + 1, y),
                    GetCellByArrayIndex(x, y - 1),
                    GetCellByArrayIndex(x, y + 1)
                };

                foreach (var adjacentCell in adjacentCells)
                {
                    if (!adjacentCell || !adjacentCell.CurrentAtom) continue;
                    Block adjacentBlock = adjacentCell.CurrentAtom.ParentBlock;
            
                    if (adjacentBlock && adjacentBlock.BlockState == BlockState.Normal)
                    {
                        candidatesForInfection.Add(adjacentBlock);
                    }
                }
            }

            if (candidatesForInfection.Count > 0)
            {
                var blockToInfect = candidatesForInfection.GetRandomElement(); 
                blockToInfect.PreInfect();
            }
        }
        #endregion
        
        #region Utils
        /// <summary>
        /// Get the cell by array index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>A Cell if it exists, null otherwise</returns>
        public Cell GetCellByArrayIndex(int x, int y)
        {
            if (x < 0 || x >= currentGridSize.y || y < 0 || y >= currentGridSize.x)
            {
                return null;
            }
            return _cellArray[x, y];
        }
        
        public Cell GetCellByArrayIndex(Vector2Int index)
        {
            return GetCellByArrayIndex(index.x, index.y);
        }
        
        public Cell GetCellByGridIndex(int x, int y)
        {
            return GetCellByArrayIndex(currentOffset.y - y, x - currentOffset.x);
        }
        
        public Cell GetCellByGridIndex(Vector2Int gridIndex)
        {
            return GetCellByGridIndex(gridIndex.x, gridIndex.y);
        }
    
        /// <summary>
        /// Get the cell by position, it will be rounded to the nearest cell
        /// </summary>
        /// <param name="position">Position to try to get a cell</param>
        /// <returns>A Cell if it exists, null otherwise</returns>
        public Cell GetCellByPosition(Vector3 position)
        {
            var worldToCell = _grid.WorldToCell(position);
            int x = worldToCell.x;
            int y = worldToCell.y;
            return GetCellByGridIndex(x, y);
        }
        
        public Bounds GetCellBounds(Cell cell)
        {
            var index = cell.GridIndex;
            return GetCellBounds(index);
        }

        public Bounds GetCellBounds(Vector2Int gridIndex)
        {
            var cellBounds = _grid.GetBoundsLocal((Vector3Int)gridIndex);
            var centerWorld = _grid.GetCellCenterWorld((Vector3Int)gridIndex);
            cellBounds.center = centerWorld;
            return cellBounds;
        }
        #endregion
        
        #region Editor
        #if UNITY_EDITOR
        public void OnSceneGUI()
        {
            DrawGrid();
        }
        private void DrawGrid()
        {
            if (!currentGridPreset) return;
            var row = currentGridSize.y;
            var column = currentGridSize.x;
            if (!_grid)
            {
                _grid = GetComponent<Grid>();
            }
            for (int x = 0; x < row; x++)
            {
                for (int y = 0; y < column; y++)
                {
                    var textColor = Color.green;
                    var handleColor = Color.green;
                    if (currentGridPreset.PresetGridType is GridType.Custom && !currentGridPreset.CustomGrid[x, y])
                    {
                        if (!drawAllCustomGridCells) continue;
                        handleColor = Color.red;
                        textColor = Color.red;
                    }
                    Handles.color = handleColor;
                    var arrayIndex = new Vector2Int(x, y);
                    var gridIndex = new Vector2Int(y + currentOffset.x, currentOffset.y - x);
                    var bounds = GetCellBounds(gridIndex);
                    Handles.DrawWireCube(bounds.center, bounds.size);
                    Handles.Label(bounds.center, arrayIndex.ToString(), style: new GUIStyle()
                    {
                        fontSize = 10,
                        normal = new GUIStyleState()
                        {
                            textColor = textColor
                        },
                        alignment = TextAnchor.MiddleCenter
                    });
                }
            }
        }
        #endif
        #endregion

        #region Table Matrix
        private static int DrawVacantSchemaMatrix(Rect rect, int value)
        {
            EditorGUI.DrawRect(rect.Padding(1), value == 1 ? Color.green : Color.grey);
            return value;
        }
        
        private static Cell DrawCellArrayMatrix(Rect rect, Cell cell)
        {
            if (!cell) return null;
            EditorGUI.DrawRect(rect.Padding(1), cell.CurrentAtom ? Color.green : Color.grey);
            return cell;
        }
        #endregion
        
        #region Serialization
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }

        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        public SerializationData SerializationData 
        { 
            get => serializationData;
            set => serializationData = value;
        }
        #endregion
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(GridManager), true)]
    public class GridManagerEditor : OdinEditor
    {
        public void OnSceneGUI()
        {
            if (target is not GridManager proceduralGrid) return;
            proceduralGrid.OnSceneGUI();
        }
    }
    #endif
}
