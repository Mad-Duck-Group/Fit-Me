using System;
using System.Collections.Generic;
using System.Linq;
using MadDuck.Scripts.Utils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityCommunity.UnitySingleton;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Managers
{
    [RequireComponent(typeof(Grid))]
    public class GridManager : MonoSingleton<GridManager>
    {
        private enum GridType
        {
            Rectangle,
            Custom
        }
    
        [Serializable]
        public struct Contacts
        {
            public List<Block> contactedBlocks;
            public BlockTypes contactType;
        }
        
        [Title("Grid References")]
        [SerializeField] private Cell cellPrefab;
        [SerializeField] private Transform cellParent;
        [SerializeField] private Color whiteColor;
        [SerializeField] private Color blackColor;
        [SerializeField] private Color canBePlacedColor;
        [SerializeField] private Color cannotBePlacedColor;
        
        [Title("Grid Settings")]
        [SerializeField] private GridType gridType = GridType.Rectangle;
        [SerializeField] private Vector2Int gridSize = new(10, 10);
        [SerializeField] private Vector2Int offset = new(0, 0);
        
        [Title("Grid Debug")]
        [SerializeField, ReadOnly] private List<Contacts> contacts = new();

        private Grid _grid;
        private Cell[,] _cellArray = {};
        private List<Cell> _previousValidationCells = new();
        private int[,] _vacantSchema;
        private List<Block> _blocksOnGrid = new();

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
            _blocksOnGrid.Add(block);
            GameManager.Instance.AddScore(ScoreTypes.Placement);
            if (!CreateVacantSchema()) //Fit Me!
            {
                GameManager.Instance.AddScore(ScoreTypes.FitMe);
                RemoveAllBlocks(true);
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
            _blocksOnGrid.Remove(block);
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
            List<Block> blocksToRemove = new List<Block>(_blocksOnGrid);
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
                    if (_cellArray[x, y].CurrentAtom) continue;
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
            var index = (Vector3Int)cell.GridIndex;
            var cellBounds = _grid.GetBoundsLocal(index);
            var centerWorld = _grid.GetCellCenterWorld(index);
            cellBounds.center = centerWorld;
            return cellBounds;
        }
        #endregion

        #if UNITY_EDITOR
        #region Editor
        public void OnSceneGUI()
        {
            DrawGrid();
        }

        private void DrawGrid()
        {
            Handles.color = Color.green;
            foreach (var cell in _cellArray)
            {
                var bounds = GetCellBounds(cell);
                Handles.DrawWireCube(bounds.center, bounds.size);
                Handles.Label(bounds.center, cell.ArrayIndex.ToString(), style: new GUIStyle()
                {
                    fontSize = 10,
                    normal = new GUIStyleState()
                    {
                        textColor = Color.green
                    },
                    alignment = TextAnchor.MiddleCenter
                });
            }
        }
        #endregion
        #endif
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
