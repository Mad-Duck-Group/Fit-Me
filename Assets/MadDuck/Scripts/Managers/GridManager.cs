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
    [RequireComponent(typeof(Grid))]
    [ShowOdinSerializedPropertiesInInspector]
    public class GridManager : MonoSingleton<GridManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspectors
        private enum GridType
        {
            Rectangle,
            Custom
        }

        private enum GridOffsetType
        {
            Automatic,
            Custom
        }
    
        // [Serializable]
        // public struct Contacts
        // {
        //     [SerializeField, Sirenix.OdinInspector.ReadOnly] public List<Block> contactedBlocks;
        //     [SerializeField, Sirenix.OdinInspector.ReadOnly] public BlockTypes contactType;
        // }
        
        [Title("Grid References")]
        [SerializeField] private Cell cellPrefab;
        [SerializeField] private Transform cellParent;
        [SerializeField] private Color whiteColor;
        [SerializeField] private Color blackColor;
        [SerializeField] private Color canBePlacedColor;
        [SerializeField] private Color cannotBePlacedColor;
        
        [TitleGroup("Grid Settings")]
        [SerializeField] 
        private GridType gridType = GridType.Rectangle;
        [TitleGroup("Grid Settings")]
        [Button("Refresh Custom Grid"), ShowIf(nameof(gridType), GridType.Custom), DisableInPlayMode]
        private void RefreshCustomGrid()
        {
            var newCustomGrid = new bool[gridSize.y, gridSize.x];
            var oldRow = _customGrid.GetLength(0);
            var oldColumn = _customGrid.GetLength(1);
            var newRow = newCustomGrid.GetLength(0);
            var newColumn = newCustomGrid.GetLength(1);
            for (int x = 0; x < newRow; x++)
            {
                for (int y = 0; y < newColumn; y++)
                {
                    if (x >= newRow || y >= newColumn)
                    {
                        continue;
                    }
                    if (x >= oldRow || y >= oldColumn)
                    {
                        newCustomGrid[x, y] = false;
                        continue;
                    }
                    if (x < newRow && y < gridSize.x)
                    {
                        newCustomGrid[x, y] = _customGrid[x, y];
                    }
                    else
                    {
                        newCustomGrid[x, y] = false;
                    }
                }
            }
            _customGrid = newCustomGrid;
        }
        [TitleGroup("Grid Settings")]
        [Button("Clear Custom Grid"), ShowIf(nameof(gridType), GridType.Custom), DisableInPlayMode]
        private void ClearCustomGrid()
        {
            _customGrid = new bool[gridSize.y, gridSize.x];
        }
        [TitleGroup("Grid Settings")]
        [SerializeField] [OnValueChanged(nameof(UpdateGridOffset))]
        [MinValue(1)]
        private Vector2Int gridSize = new(10, 10);
        [TitleGroup("Grid Settings")]
        [SerializeField] [MinMaxSlider(1, 20, ShowFields = true)]
        private Vector2Int randomGridXRange = new(1, 10);
        [TitleGroup("Grid Settings")]
        [SerializeField] [MinMaxSlider(1, 20, ShowFields = true)]
        private Vector2Int randomGridYRange = new(1, 10);
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
        [TableMatrix(SquareCells = true, HorizontalTitle = "Custom Grid",
            DrawElementMethod = nameof(DrawCustomGridMatrix), Transpose = true)]
        [SerializeField, ShowIf(nameof(gridType), GridType.Custom)]
        private bool[,] _customGrid = { };
        [TitleGroup("Grid Settings")]
        [SerializeField]
        private int destroyThreshold = 3;

        [Title("Grid Debug")]
        [SerializeField, Sirenix.OdinInspector.ReadOnly] 
        private Vector2Int currentOffset = new(0, 0);
        //[SerializeField, Sirenix.OdinInspector.ReadOnly] private List<Contacts> contacts = new();
        [TableMatrix(SquareCells = true, HorizontalTitle = "Cell Array", IsReadOnly = true,
            DrawElementMethod = nameof(DrawCellArrayMatrix), Transpose = true)]
        [SerializeField] private Cell[,] _cellArray = {};
        [TableMatrix(SquareCells = true, HorizontalTitle = "Vacant Schema", IsReadOnly = true,
            DrawElementMethod = nameof(DrawVacantSchemaMatrix), Transpose = true)]
        [SerializeField] private int[,] _vacantSchema = {};
        [SerializeField, Sirenix.OdinInspector.ReadOnly] private List<Block> blocksOnGrid = new();
        public List<Block> BlocksOnGrid => blocksOnGrid;
        [SerializeField, ShowIf(nameof(gridType), GridType.Custom)]
        private bool drawAllCustomGridCells = true;
        
        [Title("Infected Debug")]
        [field: SerializeField, Sirenix.OdinInspector.ReadOnly] public float RandomInfectedTime { get; private set; }
        [SerializeField, Sirenix.OdinInspector.ReadOnly] private List<Block> infectedBlocks = new();

        private void UpdateGridOffset()
        {
            switch (gridHorizontalOffsetType)
            {
                case GridOffsetType.Automatic:
                    currentOffset.x = -Mathf.FloorToInt(gridSize.x / 2f);
                    if (gridSize.x % 2 != 0)
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
                    currentOffset.y = Mathf.FloorToInt(gridSize.y / 2f);
                    break;
                case GridOffsetType.Custom:
                    currentOffset.y = customOffsetY;
                    break;
            }
        }
        #endregion

        #region Fields
        private Grid _grid;
        private List<Cell> _previousValidationCells = new();
        public static event Action<Block> OnBlockInfected;
        public static event Action<Block> OnBlockDisinfected;
        #endregion
        
        #region Initialization

        private void OnEnable()
        {
            GameManager.OnSceneActivated += OnSceneActivated;
        }

        private void OnDisable()
        {
            GameManager.OnSceneActivated -= OnSceneActivated;
        }

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

        void OnSceneActivated()
        {
            CreateCells();
        }

        private void RandomGridSize()
        {
            int randomX = Random.Range(randomGridXRange.x, randomGridXRange.y + 1);
            int randomY = Random.Range(randomGridYRange.x, randomGridYRange.y + 1);
            gridSize = new Vector2Int(randomX, randomY);
            UpdateGridOffset();
        }

        public void RegenerateGrid()
        {
            Debug.Log("Regenerating grid...");
            //contacts.Clear();
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
            RandomGridSize();
            var row = gridSize.y;
            var column = gridSize.x;
            var cellSize = _grid.cellSize.x;
            _cellArray = new Cell[row, column];
            for (int x = 0; x < row; x++)
            {
                for (int y = 0; y < column; y++)
                {
                    if (gridType is GridType.Custom && !_customGrid[x, y]) continue; 
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
    
        #region Blocks
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
            blocksOnGrid.Add(block);
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
            if (!CreateVacantSchema()) //Fit Me!
            {
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
            blocksOnGrid.Remove(block);
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
            List<Block> blocksToRemove = new List<Block>(blocksOnGrid);
            foreach (var block in blocksToRemove)
            {
                RemoveBlock(block, destroy);
            }
        }
        #endregion
    
        /// <summary>
        /// Reset the color of the previous validation cells
        /// </summary>
        public void ResetPreviousValidationCells()
        {
            if (_previousValidationCells.Count == 0) return;
            _previousValidationCells.ForEach(cell => cell.SpriteRenderer.color = cell.OriginalColor);
            _previousValidationCells.Clear();
        }

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
        public bool CreateVacantSchema()
        {
            var row = gridSize.y;
            var column = gridSize.x;
            _vacantSchema = new int[row, column];
            bool isVacant = false;
            for (int x = 0; x < row; x++)
            {
                for (int y = 0; y < column; y++)
                {
                    var cell = _cellArray[x, y];
                    if (!cell) continue;
                    if (cell.CurrentAtom) continue;
                    _vacantSchema[x, y] = 1;
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
            CreateVacantSchema();
            availableBlocks = new List<Block>();
            foreach (var block in blockToCheck)
            {
                // if (block.BlockSchemas.Count == 0)
                // {
                //     block.GenerateSchema();
                // }
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
            if (ArrayHelper.CanBlockFitInVacant(_vacantSchema, block.BlockSchemas[index].schema))
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
        
        public bool CanSchemaFitInVacant(int[,] blockSchema)
        {
            return ArrayHelper.CanBlockFitInVacant(_vacantSchema, blockSchema);
        }

        #region Utils
        /// <summary>
        /// Get the cell by array index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>A Cell if it exists, null otherwise</returns>
        public Cell GetCellByArrayIndex(int x, int y)
        {
            if (x < 0 || x >= gridSize.y || y < 0 || y >= gridSize.x)
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

        #region Infection
        private void InfectBlock(Block block)
        {
            if (GameManager.Instance.CurrentGameState.Value is not (GameState.PlaceBlock or GameState.UseItem)) return;
            infectedBlocks.Add(block);
            block.PreInfect();
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
        
        public void InfectRandomBlock()
        {
            if (blocksOnGrid.Count == 0) return;
            Block block = blocksOnGrid.GetRandomElement();
            if (block.BlockState != BlockState.Normal) return;
            InfectBlock(block);
        }

        public void InfectAdjacentBlocks(Block sourceBlock)
        {
            if (!sourceBlock || sourceBlock.BlockState != BlockState.Infected) return;

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
                InfectBlock(blockToInfect);
            }
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
            var row = gridSize.y;
            var column = gridSize.x;
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
                    if (gridType is GridType.Custom && !_customGrid[x, y])
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
        private static bool DrawCustomGridMatrix(Rect rect, bool value)
        {
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                value = !value;
                GUI.changed = true;
                Event.current.Use();
            }

            EditorGUI.DrawRect(rect.Padding(1), value ? Color.green : Color.grey);
            return value;
        }
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
