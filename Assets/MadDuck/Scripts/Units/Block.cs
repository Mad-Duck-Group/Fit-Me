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
    #endregion

    [ShowOdinSerializedPropertiesInInspector]
    public class Block : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspectors
        [Title("Block References")]
        [SerializeField] private Atom atomPrefab;
        [SerializeField] private Transform atomParent;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteResolver spriteResolver;

        [Title("Block Settings")]
        [SerializeField] private Color originalColor = Color.white;
        [SerializeField] private float pickUpScaleMultiplier = 1.2f;
        [field: SerializeField] public bool AllowPickUpAfterPlacement { get; private set; }
        
        [field: Title("Block Debug")]
        [field: SerializeField, DisplayAsString] public BlockTypes BlockType { get; private set; }
        [field: SerializeField, DisplayAsString] public string BlockFace { get; private set; }
        [field: SerializeField, ReadOnly] public List<Atom> Atoms { get; private set; } = new();
        [field: SerializeField, ReadOnly] public BlockPreset BlockPreset { get; private set; }
        [SerializeField, DisplayAsString] private FlashState flashState;
        [field: SerializeField, DisplayAsString] public BlockState BlockState { get; private set; } = BlockState.Normal;
        [field: SerializeField, DisplayAsString] public bool IsPlaced { get; private set; }
        [field: SerializeField] public List<Cell> BlockCells { get; set; }
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
        private IDisposable _infectionSubscription;
        #endregion

        #region Initialization
        private void Start()
        {
            _infectColor = GameManager.Instance.infectColor;
        }

        public void Initialize()
        {
            _originalSpriteScale = spriteRenderer.transform.localScale;
            _originalPosition = transform.position;
            _originalRotation = transform.eulerAngles;
            _originalScale = transform.localScale;
            Atoms.ForEach(a => a.ParentBlock = this);
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
        public void GenerateAtom(string blockFace, BlockPreset preset)
        {
            var row = preset.BlockSize.y;
            var column = preset.BlockSize.x;
            BlockFace = blockFace;
            BlockPreset = preset;
            for (var x = 0; x < row; x++)
            {
                for (var y = 0; y < column; y++)
                {
                    if (preset.BlockSchema.schema[x, y] == 0)
                    {
                        continue;
                    }
                    float spawnPosX = -column / 2f + 0.5f + y;
                    float spawnPosY = row / 2f - 0.5f - x;
                    Vector3 spawnPosition = new Vector3(spawnPosX, spawnPosY, 0);
                    var atom = Instantiate(atomPrefab, spawnPosition, Quaternion.identity, atomParent);
                    atom.ParentBlock = this;
                    var hasTop = HasElement(x - 1, y);
                    var hasBottom = HasElement(x + 1, y);
                    var hasLeft = HasElement(x, y - 1);
                    var hasRight = HasElement(x, y + 1);
                    atom.SpriteOutlineController.outlineTop = !hasTop;
                    atom.SpriteOutlineController.outlineBottom = !hasBottom;
                    atom.SpriteOutlineController.outlineLeft = !hasLeft;
                    atom.SpriteOutlineController.outlineRight = !hasRight;
                    atom.SpriteOutlineController.UpdateOutline();
                    Atoms.Add(atom);
                }
            }
            // var spritePositionX = -column / 2f + 0.5f;
            // var spritePositionY = row / 2f - 0.5f;
            // spriteRenderer.transform.localPosition = new Vector3(spritePositionX, spritePositionY, 0);
            //spriteRenderer.gameObject.SetActive(false); //NOTE: Disable sprite renderer for now until the sprite asset is fixed
            BlockPreset.GenerateSchema();
            
            bool HasElement(int x, int y)
            {
                if (x < 0 || x >= row || y < 0 || y >= column)
                    return false;
                return preset.BlockSchema.schema[x, y] == 1;
            }
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
            spriteRenderer.color = originalColor; //NOTE: Disable for now, until the sprite asset is fixed
            SetColor(originalColor);
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
        public void ChangeType(BlockTypes type, bool updateGrid = true)
        {
            BlockType = type;
            //NOTE: Disable sprite resolver for now until the sprite asset is fixed
            if (!RandomBlockManager.Instance.SpriteLibraryAssets.TryGetValue(type, out var spriteAsset))
            {
                Debug.LogError($"Sprite asset for block type {type} not found.");
                return;
            }
            spriteResolver.spriteLibrary.spriteLibraryAsset = spriteAsset;
            spriteResolver.SetCategoryAndLabel("Face", BlockFace);
            spriteResolver.ResolveSpriteToSpriteRenderer();
            if (!RandomBlockManager.Instance.AtomColorDictionary.TryGetValue(type, out var color))
            {
                Debug.LogError($"Color for block type {type} not found.");
                return;
            }
            originalColor = color;
            SetColor(color);
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
                    
                    /*
                    _flashTween = Tween.Color(spriteRenderer, Color.red, 0.2f, cycles: -1, cycleMode: CycleMode.Yoyo);*/ 
                    //NOTE: Disable for now, until the sprite asset is fixed
                    spriteRenderer.color = originalColor;
                    SetColor(originalColor);
                    _flashTween = Tween.Custom(originalColor, Color.red, 0.2f, cycles: -1, cycleMode: CycleMode.Yoyo,
                        onValueChange: SetColor);
                    break;
                
                case FlashState.PreInfectFlash:
                    if (BlockState != BlockState.PreInfected) return;
                    if(_preInfectTween.isAlive) return;
                    
                    /*
                    _preInfectTween = Tween.Color(spriteRenderer, _infectColor, 0.2f, cycles: -1, cycleMode: CycleMode.Yoyo);*/
                    //NOTE: Disable for now, until the sprite asset is fixed
                    spriteRenderer.color = originalColor;
                    SetColor(originalColor);
                    _preInfectTween = Tween.Custom(originalColor, _infectColor, 0.2f, cycles: -1, cycleMode: CycleMode.Yoyo,
                        onValueChange: SetColor);
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
                spriteRenderer.color = originalColor; //NOTE: Disable for now, until the sprite asset is fixed
                SetColor(originalColor);
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
                    spriteRenderer.color = _infectColor; //NOTE: Disable for now, until the sprite asset is fixed
                    SetColor(_infectColor);
                    break;
            }
        }
        
        public void StopAllFlash()
        {
            if (_flashTween.isAlive)
            { _flashTween.Stop(); }
            
            if (_preInfectTween.isAlive)
            { _preInfectTween.Stop(); }
            
            SetColor(originalColor);
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
            if (_transformTween.isAlive)
            {
                _transformTween.Stop();
            }
            _transformTween = Tween.Position(transform, _originalPosition, 0.2f).OnComplete(() => SetRendererSortingOrder(1));
            Tween.Rotation(transform, _originalRotation, 0.2f);
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
            Atoms.ForEach(atom => atom.SpriteRenderer.sortingOrder = order);
            spriteRenderer.sortingOrder = order;
        }

        public void SetColor(Color color)
        {
            //Atoms.ForEach(atom => atom.SpriteRenderer.color = color);
            spriteRenderer.color = color;
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