using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MadDuck.Scripts.Utils;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : MonoSingleton<GridManager>
{
    [Serializable]
    public struct Contacts
    {
        public List<Block> contactedBlocks;
        public BlockTypes contactType;
    }

    [Header("Grid Settings")]
    [FormerlySerializedAs("width")] [SerializeField] private int column;
    [FormerlySerializedAs("height")] [SerializeField] private int row;
    [SerializeField] private int cellSize;
    [SerializeField] private Vector2 offset;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject cellParent;
    [SerializeField] private Color whiteColor;
    [SerializeField] private Color blackColor;
    [SerializeField] private Color canBePlacedColor;
    [SerializeField] private Color cannotBePlacedColor;
    //[SerializeField] private Block[] blockPrototypes;
    private Cell[,] _cellArray;
    private List<Cell> _previousValidationCells = new List<Cell>();
    private int[,] _vacantSchema;
    [SerializeField] private List<Contacts> _contacts = new List<Contacts>();
    private List<Block> _blocksOnGrid = new List<Block>();

    /// <summary>
    /// Get the cell by index
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>A Cell if it exists, null otherwise</returns>
    public Cell GetCellByIndex(int x, int y)
    {
        if (x < 0 || x >= row || y < 0 || y >= column)
        {
            return null;
        }
        return _cellArray[x, y];
    }
    
    /// <summary>
    /// Get the cell by position, it will be rounded to the nearest cell
    /// </summary>
    /// <param name="position">Position to try to get a cell</param>
    /// <returns>A Cell if it exists, null otherwise</returns>
    public Cell GetCellByPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt((offset.y - position.y) / cellSize);
        int y = Mathf.RoundToInt((position.x - offset.x) / cellSize);
        return GetCellByIndex(x, y);
    }

    void Start()
    {
        CreateGrid();
    }
    
    /// <summary>
    /// Create the grid
    /// </summary>
    private void CreateGrid()
    {
        _cellArray = new Cell[row, column];
        for (int x = 0; x < row; x++)
        {
            for (int y = 0; y < column; y++)
            {
                Vector3 spawnPosition = offset + new Vector2(y, -x) * cellSize;
                _cellArray[x, y] = Instantiate(cellPrefab, spawnPosition, Quaternion.identity).GetComponent<Cell>();
                _cellArray[x, y].transform.localScale = Vector3.one * cellSize;
                _cellArray[x, y].name = "Cell " + x + ", " + y;
                _cellArray[x, y].transform.parent = cellParent.transform;
                _cellArray[x, y].Index = new int[] {x, y};
                
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
        Vector3 atomPositionBeforePlacement = block.Atoms[0].transform.position;
        List<Cell> cells = new List<Cell>();
        foreach (var atom in block.Atoms)
        {
            Vector3 atomPosition = atom.transform.position;
            Vector3 cellPosition = new Vector3(atomPosition.x, atomPosition.y, 0);
            Cell cell = GetCellByPosition(cellPosition);
            if (cell == null || cell.CurrentAtom != null)
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
            if (cell == null || cell.CurrentAtom != atom)
            {
                continue;
            }
            cell.SetAtom(null);
        }
        List<Contacts> contactsToRemove = _contacts.FindAll(contact => contact.contactedBlocks.Contains(block));
        foreach (var contact in contactsToRemove)
        {
            _contacts.Remove(contact);
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
            Cell upCell = GetCellByIndex(cell.Index[0] - 1, cell.Index[1]);
            Cell downCell = GetCellByIndex(cell.Index[0] + 1, cell.Index[1]);
            Cell leftCell = GetCellByIndex(cell.Index[0], cell.Index[1] - 1);
            Cell rightCell = GetCellByIndex(cell.Index[0], cell.Index[1] + 1);
            List<Cell> adjacentCells = new List<Cell> {upCell, downCell, leftCell, rightCell};
            foreach (var adjacentCell in adjacentCells)
            {
                if (adjacentCell == null || adjacentCell.CurrentAtom == null) continue;
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
        _contacts.Add(contacts);
        return true;
    }

    /// <summary>
    /// Check if there are more than 3 blocks in contact
    /// </summary>
    /// <param name="contacts">Current contacts</param>
    private void ContactValidation(Contacts contacts)
    {
        BlockTypes currentType = contacts.contactType;
        List<Contacts> sameTypeContacts = _contacts.FindAll(contact => contact.contactType == currentType);
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
        _contacts.Remove(contacts);
        foreach (var contact in matchedContacts)
        {
            _contacts.Remove(contact);
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
        _vacantSchema = new int[row, column];
        bool isVacant = false;
        for (int x = 0; x < row; x++)
        {
            for (int y = 0; y < column; y++)
            {
                if (_cellArray[x, y].CurrentAtom != null) continue;
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
    

    /// <summary>
    /// Draw the grid boundaries in the editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 realCenter = new Vector3(column, -row) * cellSize * 0.5f;
        Vector3 halfSize = new Vector3(-cellSize, cellSize, 1) * 0.5f;
        Gizmos.DrawWireCube((realCenter + (Vector3)offset) + halfSize, new Vector3(column * cellSize, row * cellSize, 1));
    }
}
