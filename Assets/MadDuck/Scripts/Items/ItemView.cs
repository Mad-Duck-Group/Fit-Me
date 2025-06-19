using System;
using System.Linq;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils.Inspectors;
using MessagePipe;
using PrimeTween;
using Redcode.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MadDuck.Scripts.Items
{
    public struct ItemBlockHoveredEvent
    {
        public readonly Item item;
        public readonly Block block;

        public ItemBlockHoveredEvent(Item item, Block block)
        {
            this.item = item;
            this.block = block;
        }
    }
    public class ItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Inspectors
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Canvas itemCanvas;
        [SerializeField] private Canvas itemCountCanvas;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countText;
        [SerializeReference, Sirenix.OdinInspector.ReadOnly] private Item item;
        [SerializeField, SortingLayer] private int originalSortingLayer;
        [SerializeField, SortingLayer] private int targetSortingLayer;
        #endregion

        #region Fields and Properties
        private Vector3 _initialPosition;
        private IPublisher<ItemBlockHoveredEvent> _itemBlockHoveredPublisher;
        private Transform _parentTransform;
        private int _parentSiblingIndex;
        private Vector3 _mousePositionDifference;
        private Canvas _canvas;
        private bool _isDragging;
        #endregion

        #region Initialization
        private void Awake()
        {
            _itemBlockHoveredPublisher = GlobalMessagePipe.GetPublisher<ItemBlockHoveredEvent>();
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        public void Initialize(Item item)
        {
            this.item = item;
            icon.sprite = item.ItemData.ItemIcon;
            _parentTransform = canvasGroup.transform.parent;
            _parentSiblingIndex = canvasGroup.transform.GetSiblingIndex();
            itemCanvas.sortingLayerID = originalSortingLayer;
            itemCountCanvas.sortingLayerID = originalSortingLayer;
            item.OnUsed += OnItemUsed;
            item.OnCancelled += OnItemCancelled;
        }
        #endregion
        
        #region Events
        public void OnEnable()
        {
            ItemManager.OnItemCountChanged += UpdateCount;
        }

        public void OnDisable()
        {
            ItemManager.OnItemCountChanged -= UpdateCount;
        }

        private void OnItemUsed()
        {
            ReturnToInitialPosition();
        }
        
        private void OnItemCancelled()
        {
            ReturnToInitialPosition();
        }
        #endregion
        
        #region UI Interactions
        public void OnPointerClick(PointerEventData eventData)
        {
            if (item.ItemData.UsageMode != UsageMode.Click)
                return;
            item.Select();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (item.ItemData.UsageMode != UsageMode.DragAndDrop)
                return;
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            if (GameManager.Instance.CurrentGameState.Value is not GameState.PlaceBlock) return;
            if (!item.Selectable()) return;
            item.Select();
            var position = canvasGroup.transform.position;
            var mousePosition = PointerManager.Instance.MouseWorldPosition;
            Debug.Log($"Mouse position: {mousePosition}, Item position: {position}");
            _mousePositionDifference = (mousePosition - position).WithZ(0);
            _initialPosition = canvasGroup.transform.position;
            canvasGroup.transform.SetParent(_canvas.transform);
            canvasGroup.blocksRaycasts = false;
            itemCanvas.sortingLayerID = targetSortingLayer;
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (item.ItemData.UsageMode != UsageMode.DragAndDrop)
                return;
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            if (!_isDragging) return;
            if (!item.Selectable()) return;
            var mousePosition = PointerManager.Instance.MouseWorldPosition;
            canvasGroup.transform.position = mousePosition - _mousePositionDifference;
            //Debug.Log($"All hovered objects: {string.Join(", ", eventData.hovered)}");
            var blockUnderMouse = eventData.hovered
                .Select(go => go.GetComponent<Block>())
                .FirstOrDefault(b => b != null);
            _itemBlockHoveredPublisher.Publish(new ItemBlockHoveredEvent(item, blockUnderMouse));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (item.ItemData.UsageMode != UsageMode.DragAndDrop)
                return;
            if (!_isDragging) return;
            if (!item.Selectable()) return;
            item.Use();
            _isDragging = false;
        }
        #endregion
        
        #region Utils
        private void ReturnToInitialPosition()
        {
            Tween.Position(canvasGroup.transform, _initialPosition, 0.2f).OnComplete(() =>
            {
                canvasGroup.transform.SetParent(_parentTransform);
                canvasGroup.transform.SetSiblingIndex(_parentSiblingIndex);
                itemCanvas.sortingLayerID = originalSortingLayer;
                //LayoutRebuilder.ForceRebuildLayoutImmediate(_parentTransform as RectTransform);
                canvasGroup.blocksRaycasts = true;
            });
        }
        
        private void UpdateCount(ItemType itemType, int count)
        {
            if (item.ItemData.ItemType != itemType) return;
            countText.text = count.ToString();
        }
        #endregion
    }
}