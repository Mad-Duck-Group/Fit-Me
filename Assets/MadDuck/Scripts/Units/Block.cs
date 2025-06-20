using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Utils;
using PrimeTween;
using R3;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.U2D.Animation;
using Random = UnityEngine.Random;

namespace MadDuck.Scripts.Units
{
    #region Enums
    public enum BlockState
    {
        Normal,
        PreInfected,
        Infected
    }
    
    public enum FlashState
    {
        None,
        Flashing,
        PreInfectFlash
    }
    
    public enum BlockTypes
    {
        Red,
        Yellow,
        Green,
        Purple
    }

    public enum BlockFaces
    {
        I1,
        I2,
        I3,
        I4,
        S,
        SMirror,
        L,
        LMirror,
        T,
        TwoByTwo
    }
    #endregion

    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public record BlockSchema
    {
        [TableMatrix(SquareCells = true, Transpose = true, DrawElementMethod = nameof(DrawSchemaMatrix),
            IsReadOnly = true)]
        [ShowInInspector]
        public int[,] schema = { };
        public int index;
        
        public BlockSchema(int[,] schema, int index)
        {
            this.schema = schema;
            this.index = index;
        }
        
        private static int DrawSchemaMatrix(Rect rect, int value)
        {
            EditorGUI.DrawRect(rect.Padding(1), value == 1 ? Color.green : Color.grey);
            return value;
        }
    }
    
    [ShowOdinSerializedPropertiesInInspector]
    public class Block : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspectors
        [Title("Block References")]
        [SerializeField] private BlockTypes blockType;
        [SerializeField] private BlockFaces blockFace;
        [SerializeField] private Atom[] atoms;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteResolver spriteResolver;
        [SerializeField] private bool allowPickUpAfterPlacement;
        
        [Title("Block Settings")]
        [SerializeField] private Color originalColor = Color.white;
        [SerializeField] private float pickUpScaleMultiplier = 1.2f;
        
        [Title("Block Debug")]
        [SerializeField, DisplayAsString] private FlashState flashState;
        [field: SerializeField, DisplayAsString] public BlockState BlockState { get; private set; } = BlockState.Normal;
        [field: SerializeField, DisplayAsString] public bool IsPlaced { get; private set; }
        [field: SerializeField] public List<Cell> BlockCells { get; set; }
        [TableList(AlwaysExpanded = true)]
        [field: SerializeField] public List<BlockSchema> BlockSchemas { get; private set; } = new();
        public int SpawnIndex { get; set; }
        #endregion
        
        #region Fields and Properties
        private Color _infectColor;
        private Vector3 _originalPosition;
        private Vector3 _originalRotation;
        private Vector3 _originalScale;
        private Vector3 _originalSpriteScale;
        private Color _beforeFlashColor;
        private Vector3 _mousePositionDifference;
        private Tween _transformTween;
        private Tween _flashTween;
        private Tween _preInfectTween;
        private bool _isDragging;
        public BlockTypes BlockType => blockType;
        public BlockFaces BlockFace => blockFace;
        public Atom[] Atoms => atoms;
        public bool AllowPickUpAfterPlacement => allowPickUpAfterPlacement;
        private IDisposable _infectionSubscription;
        #endregion

        #region Initialization
        private void Start()
        {
            foreach (var atom in atoms)
            {
                atom.ParentBlock = this;
            }

            _infectColor = GameManager.Instance.infectColor;
        }

        public void Initialize()
        {
            _originalSpriteScale = spriteRenderer.transform.localScale;
            _originalPosition = transform.position;
            _originalRotation = transform.eulerAngles;
            _originalScale = transform.localScale;
            //_originalColor = spriteRenderer.color;
            //GridManager.OnBlockInfected += OnBlockInfected;
            //GridManager.OnBlockDisinfected += OnBlockDisinfected;
        }

        private void StartInfectTimer()
        {
            _infectionSubscription?.Dispose();
            if (BlockState is not BlockState.Infected) return;
            _infectionSubscription = Observable
                .Interval(TimeSpan.FromSeconds(GridManager.Instance.RandomInfectedTime))
                .Subscribe(_ => GridManager.Instance.InfectAdjacentBlocks(this));
        }
        #endregion

        #region Events
        void OnDestroy()
        {
            _infectionSubscription?.Dispose();
        }
        #endregion

        #region Schema
        /// <summary>
        /// Generate the schema of the block, 1 is an atom, 0 is empty
        /// </summary>
        [Button("Test Schema")]
        public void GenerateSchema()
        {
            Vector3 currentScale = transform.localScale;
            transform.localScale = Vector3.one;
            Atom[] sortByX = Atoms.OrderByDescending(atom => atom.transform.position.x).ToArray();
            Atom[] sortByY = Atoms.OrderByDescending(atom => atom.transform.position.y).ToArray();
            Atom mostRight = sortByX.First();
            Atom mostLeft = sortByX.Last();
            Atom mostUp = sortByY.First();
            Atom mostDown = sortByY.Last();
            int column = Mathf.RoundToInt(mostRight.transform.position.x - mostLeft.transform.position.x) + 1;
            int row = Mathf.RoundToInt(mostUp.transform.position.y - mostDown.transform.position.y) + 1;
            int[,] originalSchema = new int[row, column];
            Debug.Log("row: " + row + " column: " + column);
            foreach (var atom in Atoms)
            {
                int x = Mathf.RoundToInt(mostRight.transform.position.x - atom.transform.position.x);
                int y = Mathf.RoundToInt(atom.transform.position.y - mostDown.transform.position.y);
                originalSchema[y, x] = 1;
            }
            BlockSchemas.Clear();
            BlockSchemas.Add(new BlockSchema(ArrayHelper.Rotate180(originalSchema), 0));
            BlockSchemas.Add(new BlockSchema(ArrayHelper.Rotate270(BlockSchemas[0].schema), 1));
            BlockSchemas.Add(new BlockSchema(originalSchema, 2));
            BlockSchemas.Add(new BlockSchema(ArrayHelper.Rotate90(BlockSchemas[0].schema), 3));
            //BlockSchemas = BlockSchemas.Distinct().ToList(); //Remove duplicates
            transform.localScale = currentScale;
        }
        #endregion

        #region Infection
        public async UniTask PreInfect()
        {
            BlockState = BlockState.PreInfected;
            if (BlockState == BlockState.PreInfected)
                StartFlashing(FlashState.PreInfectFlash);
            
            if (BlockState != BlockState.PreInfected) return;
            await UniTask.WaitForSeconds(GameManager.Instance.PreInfectTime,
                cancellationToken: destroyCancellationToken);
            GridManager.Instance.InfectBlock(this);
        }
        
        public void Infect()
        {
            BlockState = BlockState.Infected;
            StopFlashing();
            StartInfectTimer();
        }
        
        public void Disinfect()
        {
            spriteRenderer.color = originalColor;
            //_beforeFlashColor = spriteRenderer.color;
            BlockState = BlockState.Normal;
            _infectionSubscription?.Dispose();
        }
        #endregion
        
        #region Interactions
        /// <summary>
        /// Handle rotation of the block
        /// </summary>
        private void HandleBlockManipulation()
        {
            
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left) return;
            if (GameManager.Instance.CurrentGameState.Value is GameState.GameOver or GameState.GameClear)
            {
                OnEndDrag(eventData);
                return;
            }
            if (GameManager.Instance.CurrentGameState.Value is not GameState.PlaceBlock) return;
            if (IsPlaced && !AllowPickUpAfterPlacement) return;
            var position = transform.position;
            var mousePosition = PointerManager.Instance.MouseWorldPosition;
            _mousePositionDifference = new Vector3(mousePosition.x - position.x,
                mousePosition.y - position.y, 0);
            SetRendererSortingOrder(2);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left) return;
            if (GameManager.Instance.CurrentGameState.Value is GameState.GameOver or GameState.GameClear)
            {
                OnEndDrag(eventData);
                return;
            }
            if (GameManager.Instance.CurrentGameState.Value is not GameState.PlaceBlock) return;
            if (IsPlaced && !AllowPickUpAfterPlacement) return;
            HandleBlockManipulation();
            GridManager.Instance.ValidatePlacement(this);
            var mousePosition = PointerManager.Instance.MouseWorldPosition;
            transform.position = mousePosition - _mousePositionDifference;
            if (_isDragging) return; //Prevent unnecessary calculations
            PickUpBlock();
            GridManager.Instance.RemoveBlock(this);
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left) return;
            if (GameManager.Instance.CurrentGameState.Value is GameState.CountOff or GameState.Pause) return;
            if (!_isDragging) return;
            if (GridManager.Instance.PlaceBlock(this))
            {
                IsPlaced = true;
                _mousePositionDifference = Vector3.zero;
                SetRendererSortingOrder(1);
                Tween.Scale(spriteRenderer.transform, _originalSpriteScale, 0.2f);
                RandomBlockManager.Instance.GameOverCheck().Forget();
            }
            else
            {
                ReturnToOriginal();
                IsPlaced = false;
            }
            _isDragging = false;
        }
        #endregion
        
        #region Utils
        public void ChangeColor(BlockTypes type, bool updateGrid = true)
        {
            blockType = type;
            if (!RandomBlockManager.Instance.SpriteLibraryAssets.TryGetValue(type, out var spriteAsset))
            {
                Debug.LogError($"Sprite asset for block type {type} not found.");
                return;
            }
            spriteResolver.spriteLibrary.spriteLibraryAsset = spriteAsset;
            spriteResolver.SetCategoryAndLabel("Face", blockFace.ToString());
            spriteResolver.ResolveSpriteToSpriteRenderer();
            if (!updateGrid) return;
            GridManager.Instance.UpdateBlockOnGrid(this);
        }
        
        public void StartFlashing(FlashState flashState)
        {
            switch (flashState)
            {
                case FlashState.Flashing:
                    if(_flashTween.isAlive) return;
                    
                    if (_preInfectTween.isAlive)
                    { _preInfectTween.Complete(); }
                    
                    spriteRenderer.color = originalColor;
                    _flashTween = Tween.Color(spriteRenderer, Color.red, 0.2f, cycles: -1, cycleMode: CycleMode.Yoyo);
                    break;
                
                case FlashState.PreInfectFlash:
                    if (BlockState != BlockState.PreInfected) return;
                    if(_preInfectTween.isAlive) return;
                    
                    spriteRenderer.color = originalColor;
                    _preInfectTween = Tween.Color(spriteRenderer, _infectColor, 0.2f, cycles: -1, cycleMode: CycleMode.Yoyo);
                    break;
                
                case FlashState.None:
                    if (BlockState is BlockState.PreInfected)
                        StartFlashing(FlashState.PreInfectFlash);
                    else if (BlockState is BlockState.Infected or BlockState.Normal)
                        StopFlashing();
                    break;
            }
        }
        
        public void StopFlashing()
        {
            if (_flashTween.isAlive)
            {
                _flashTween.Complete();
                _flashTween = default;
                spriteRenderer.color = originalColor;
            }

            switch (BlockState)
            {
                case BlockState.Infected or BlockState.PreInfected:
                    StopPreInfectFlash();
                    break;
                
                case BlockState.Normal:
                    flashState = FlashState.None;
                    break;
            }
        }
        
        public void StopPreInfectFlash()
        {
            if (_preInfectTween.isAlive)
            {
                _preInfectTween.Complete();
                _preInfectTween = default;
            }
            
            switch (BlockState)
            {
                case BlockState.PreInfected:
                    StartFlashing(FlashState.PreInfectFlash);
                    break;
                
                case BlockState.Infected:
                    flashState = FlashState.None;
                    spriteRenderer.color = _infectColor;
                    break;
            }
        }
        
        public void PickUpBlock()
        {
            //Tween the block to (1, 1, 1) scale
            if (_transformTween.isAlive)
            {
                _transformTween.Stop();
            }
            var gridSize = GridManager.Instance.Grid.cellSize;
            Tween.Scale(transform, gridSize, 0.2f);
            _transformTween = Tween.Scale(spriteRenderer.transform, spriteRenderer.transform.localScale * pickUpScaleMultiplier, 0.2f);
        }

        /// <summary>
        /// Return the block to its original position, rotation and scale
        /// </summary>
        public void ReturnToOriginal()
        {
            //Tween the block to the original position
            if (_transformTween.isAlive)
            {
                _transformTween.Stop();
            }
            _transformTween = Tween.Position(transform, _originalPosition, 0.2f).OnComplete(() => SetRendererSortingOrder(1));
            //Tween the block to the original rotation
            Tween.Rotation(transform, _originalRotation, 0.2f);
            //Tween the block to the original scale
            Tween.Scale(transform, _originalScale, 0.2f);
            Tween.Scale(spriteRenderer.transform, _originalSpriteScale, 0.2f);
            GridManager.Instance.ResetPreviousValidationCells();
        }

        /// <summary>
        /// Set the sorting order of atoms
        /// </summary>
        /// <param name="order">Order to render</param>
        public void SetRendererSortingOrder(int order)
        {
            if (!spriteRenderer)
            {
                foreach (var atom in atoms)
                {
                    atom.SpriteRenderer.sortingOrder = order;
                }
                return;
            }
            spriteRenderer.sortingOrder = order;
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
}