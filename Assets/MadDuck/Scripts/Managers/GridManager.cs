using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils;
using Microsoft.Unity.VisualStudio.Editor;
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
        private enum GridType
        {
            Rectangle,
            Custom
        }
    
        [Serializable]
        public struct Contacts
        {
            [SerializeField, ReadOnly] public List<Block> contactedBlocks;
            [SerializeField, ReadOnly] public BlockTypes contactType;
        }
        
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
        
        [Title("Grid State")]
        [SerializeField, ReadOnly] private bool isFitMe = false;
        public bool IsFitMe => isFitMe;
        
        [Title("Infected Setting")]
        [SerializeField] private Vector2 infectedTimeRange = new Vector2(0, 10);
        [SerializeField] private List<Block> infectedBlocks = new();
        [SerializeField, ReadOnly] private float randomInfectedTime;
        public float RandomInfectedTime => randomInfectedTime;
        public List<Block> InfectedBlocks => infectedBlocks;
        
        
        
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
        [SerializeField]
        private Vector2Int gridSize = new(10, 10);
        [TitleGroup("Grid Settings")]
        [SerializeField] 
        private Vector2Int offset = new(0, 0);
        [TitleGroup("Grid Settings")]
        [TableMatrix(SquareCells = true, HorizontalTitle = "Custom Grid",
            DrawElementMethod = nameof(DrawCustomGridMatrix), Transpose = true)]
        [SerializeField, ShowIf(nameof(gridType), GridType.Custom)]
        private bool[,] _customGrid = { };

        [Title("Grid Debug")]
        [SerializeField, ReadOnly] private List<Contacts> contacts = new();
        [TableMatrix(SquareCells = true, HorizontalTitle = "Cell Array", IsReadOnly = true,
            DrawElementMethod = nameof(DrawCellArrayMatrix), Transpose = true)]
        [SerializeField] private Cell[,] _cellArray = {};
        [TableMatrix(SquareCells = true, HorizontalTitle = "Vacant Schema", IsReadOnly = true,
            DrawElementMethod = nameof(DrawVacantSchemaMatrix), Transpose = true)]
        [SerializeField] private int[,] _vacantSchema;
        [SerializeField, ReadOnly]  private List<Block> blocksOnGrid = new();
        [SerializeField, ShowIf(nameof(gridType), GridType.Custom)]
        private bool drawAllCustomGridCells = true;

        private Grid _grid;
        private List<Cell> _previousValidationCells = new();

        #region Initialization
        protected override void Awake()
        {
            base.Awake();
            _grid = GetComponent<Grid>();
            if (!_grid.cellSize.x.Equals(_grid.cellSize.y))
            {
                Debug.LogError("Grid cell size must be the same in both axes!");
            }
        }

        void Start()
        {
            CreateCells();
        }
        
        /// <summary>
        /// Create the cells
        /// </summary>
        private void CreateCells()
        {
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
                    Vector3 spawnPosition = new Vector2(halfSize, halfSize) + new Vector2(y + offset.x,  offset.y - x) * (cellSize);
                    _cellArray[x, y] = Instantiate(cellPrefab, spawnPosition, Quaternion.identity, cellParent);
                    _cellArray[x, y].transform.localScale = Vector3.one * cellSize;
                    _cellArray[x, y].name = $"Cell {x}_{y}";
                    _cellArray[x, y].ArrayIndex = new Vector2Int(x, y);
                    _cellArray[x, y].GridIndex = new Vector2Int(y + offset.x, offset.y - x);
                
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
            blocksOnGrid.Add(block);
            GameManager.Instance.AddScore(ScoreTypes.Placement);
            if (!CreateVacantSchema()) //Fit Me!
            {
                isFitMe = true;
                GameManager.Instance.AddScore(ScoreTypes.FitMe);
                //RemoveAllBlocks(true);
                ResetPreviousValidationCells();
                return true;
            }
            if (CheckForContact(block, cells, out Contacts contacts))
            {
                ContactValidation(contacts);
            }
            ResetPreviousValidationCells();
            return true;
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
            List<Contacts> contactsToRemove = contacts.FindAll(contact => contact.contactedBlocks.Contains(block));
            foreach (var contact in contactsToRemove)
            {
                contacts.Remove(contact);
            }
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
        /// Check if the block is in contact with other blocks with the same type
        /// </summary>
        /// <param name="block">Current block</param>
        /// <param name="cells">Cells that contain the current block</param>
        /// <param name="contacts">Contacts, if there are any</param>
        /// <returns>true if the block is in contact, false otherwise</returns>
        private bool CheckForContact(Block block, List<Cell> cells, out Contacts contacts)
        {
            BlockTypes currentType = block.BlockType;
            List<Block> contactedBlocks = new List<Block> { block };
            contacts = new Contacts();
            foreach (var cell in cells)
            {
                Cell upCell = GetCellByArrayIndex(cell.ArrayIndex[0] - 1, cell.ArrayIndex[1]);
                Cell downCell = GetCellByArrayIndex(cell.ArrayIndex[0] + 1, cell.ArrayIndex[1]);
                Cell leftCell = GetCellByArrayIndex(cell.ArrayIndex[0], cell.ArrayIndex[1] - 1);
                Cell rightCell = GetCellByArrayIndex(cell.ArrayIndex[0], cell.ArrayIndex[1] + 1);
                List<Cell> adjacentCells = new List<Cell> {upCell, downCell, leftCell, rightCell};
                foreach (var adjacentCell in adjacentCells)
                {
                    if (!adjacentCell || !adjacentCell.CurrentAtom) continue;
                    Block adjacentBlock = adjacentCell.CurrentAtom.ParentBlock;
                    if (adjacentBlock != block && adjacentBlock.BlockType == currentType && !contactedBlocks.Contains(adjacentBlock))
                    {
                        if(adjacentBlock.BlockState == BlockState.Normal)
                            contactedBlocks.Add(adjacentBlock);
                    }
                }
            }
            if (contactedBlocks.Count <= 1) return false;
            contacts.contactedBlocks = contactedBlocks;
            contacts.contactType = currentType;
            this.contacts.Add(contacts);
            return true;
        }

        /// <summary>
        /// Check if there are more than 3 blocks in contact
        /// </summary>
        /// <param name="contacts">Current contacts</param>
        private void ContactValidation(Contacts contacts)
        {
            BlockTypes currentType = contacts.contactType;
            List<Contacts> sameTypeContacts = this.contacts.FindAll(contact => contact.contactType == currentType);
            sameTypeContacts.Remove(contacts);
            List<Contacts> matchedContacts = new List<Contacts>();
            List<Block> contactedBlocks = new List<Block>();
            contactedBlocks.AddRange(contacts.contactedBlocks);
            foreach (Block block in contacts.contactedBlocks)
            {
                foreach (var contact in sameTypeContacts)
                {
                    if (contact.contactedBlocks.Contains(block))
                    {
                        matchedContacts.Add(contact);
                    }
                }
            }
            foreach (var contact in matchedContacts)
            {
                contactedBlocks.AddRange(contact.contactedBlocks);
            }
            contactedBlocks = contactedBlocks.Distinct().ToList();
            if (contactedBlocks.Count < 3) //DO NOT CHANGE THIS NUMBER NO MATTER THE CIRCUMSTANCE, THIS IS CURSED!!!!!
            {
                if (contactedBlocks.Count > 1) GameManager.Instance.AddScore(ScoreTypes.Combo, contactedBlocks.Count);
                return;
            }
            GameManager.Instance.AddScore(ScoreTypes.Combo, contactedBlocks.Count);
            GameManager.Instance.AddScore(ScoreTypes.Bomb, contactedBlocks.Count);
            this.contacts.Remove(contacts);
            foreach (var contact in matchedContacts)
            {
                this.contacts.Remove(contact);
            }
            foreach (var block in contactedBlocks)
            {
                RemoveBlock(block, true);
            }
        }

        /// <summary>
        /// Create a schema of the vacant cells, 1 is vacant, 0 is occupied
        /// </summary>
        /// <returns>true if there are vacant cells, false otherwise</returns>
        private bool CreateVacantSchema()
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
                if (block.BlockSchemas.Count == 0)
                {
                    block.GenerateSchema();
                }
                if (CompareSchema(block))
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
        /// <returns>true if the block can be placed, false otherwise</returns>
        private bool CompareSchema(Block block)
        {
            foreach (var schema in block.BlockSchemas)
            {
                if (ArrayHelper.CanBlockFitInVacant(_vacantSchema, schema))
                {
                    Debug.Log("Block " + block.name + " can be placed");
                    return true;
                }
            }
            return false;
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
            return GetCellByArrayIndex(offset.y - y, x - offset.x);
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

        private void InfectBlock(Block block)
        {
            block.SpriteRenderer.color = Color.gray;
            block.BlockState = BlockState.Infected;
            randomInfectedTime = Random.Range(infectedTimeRange.x, infectedTimeRange.y);
            Debug.Log("Block " + block.name + " is infected!");
        }
        
        private void UpdateInfectedBlocks()
        {
            if (blocksOnGrid.Count == 0) return;
            foreach (var block in blocksOnGrid.Where(block => block.BlockState == BlockState.Infected && !infectedBlocks.Contains(block)))
            {
                infectedBlocks.Add(block);
            }
        }
        
        public void RandomInfected()
        {
            if (blocksOnGrid == null) return;

            Block block = blocksOnGrid[Random.Range(0, blocksOnGrid.Count)];
            InfectBlock(block);
            //lock.StartInfectionAsync(infectedTimeRange, true);
            
            UpdateInfectedBlocks();
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
                
                Cell[] adjacentCells = new Cell[]
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
                var blockToInfect = candidatesForInfection[Random.Range(0, candidatesForInfection.Count)];
                InfectBlock(blockToInfect);
            }
        }
        
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
                    var gridIndex = new Vector2Int(y + offset.x, offset.y - x);
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
