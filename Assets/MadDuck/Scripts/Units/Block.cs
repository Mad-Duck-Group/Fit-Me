using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Utils;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MadDuck.Scripts.Units
{
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
    public class Block : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Title("Block References")]
        [SerializeField] private BlockTypes blockType;
        [SerializeField] private BlockFaces blockFace;
        [SerializeField] private Atom[] atoms;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool allowPickUpAfterPlacement;
        
        [Title("Block Debug")]
        [field: SerializeField, DisplayAsString] public bool IsPlaced { get; private set; }

        public List<int[,]> BlockSchemas { get; private set; } = new();
        public int SpawnIndex { get; set; }
        private Vector3 _originalPosition;
        private Vector3 _originalRotation;
        private Vector3 _originalScale;
        private Vector3 _originalSpriteScale;
        private Color _originalColor;
        private Vector3 _mousePositionDifference;
        private Tween _transformTween;
        private Tween _flashTween;
        private bool _isDragging;

        public BlockTypes BlockType => blockType;
        public Atom[] Atoms => atoms;
        public bool AllowPickUpAfterPlacement => allowPickUpAfterPlacement;

        private void Start()
        {
            foreach (var atom in atoms)
            {
                atom.ParentBlock = this;
            }
        }

        public void Initialize()
        {
            _originalSpriteScale = spriteRenderer.transform.localScale;
            _originalPosition = transform.position;
            _originalRotation = transform.eulerAngles;
            _originalScale = transform.localScale;
            _originalColor = spriteRenderer.color;
        }

    
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
            BlockSchemas.Add(ArrayHelper.Rotate180(originalSchema));
            BlockSchemas.Add(ArrayHelper.Rotate90(BlockSchemas[0]));
            BlockSchemas.Add(originalSchema);
            BlockSchemas.Add(ArrayHelper.Rotate270(BlockSchemas[0]));
            BlockSchemas = BlockSchemas.Distinct().ToList(); //Remove duplicates
            transform.localScale = currentScale;
        }

        public void PickUpBlock()
        {
            //Tween the block to (1, 1, 1) scale
            if (_transformTween.isAlive)
            {
                _transformTween.Stop();
            }
            _transformTween = Tween.Scale(spriteRenderer.transform, spriteRenderer.transform.localScale * 1.2f, 0.2f);
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

        /// <summary>
        /// Handle rotation of the block
        /// </summary>
        private void HandleBlockManipulation()
        {
            
        }

        public void StartFlashing()
        {
            _flashTween = Tween.Color(spriteRenderer, Color.red, 0.2f, cycles: -1, cycleMode: CycleMode.Yoyo);
        }
        
        public void StopFlashing()
        {
            if (_flashTween.isAlive)
            {
                _flashTween.Complete();
            }
            spriteRenderer.color = _originalColor;
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
                RandomBlockManager.Instance.FreeSpawnPoint(SpawnIndex);
                RandomBlockManager.Instance.DestroyBlock();
                RandomBlockManager.Instance.SpawnRandomBlock();
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
    }
}