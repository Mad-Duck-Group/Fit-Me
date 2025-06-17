using System;
using System.Collections.Generic;
using System.Linq;
using MadDuck.Scripts.Frameworks.MessagePipe;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.PopUp;
using MadDuck.Scripts.Units;
using MessagePipe;

namespace MadDuck.Scripts.Items
{
    [Serializable]
    public class ChangeColorItem : Item
    {
        private IDisposable _blockHoveredSubscriber;
        private ISubscriber<PopUpResultEvent> _popUpSubscriber;
        private IDisposable _popUpDisposable;
        private Block _blockHovered;
        private bool _popUpActive;

        public override void Initialize(ItemData itemData)
        {
            base.Initialize(itemData);
            _blockHoveredSubscriber = GlobalMessagePipe.GetSubscriber<ItemBlockHoveredEvent>()
                .Subscribe(OnBlockHovered);
            _popUpSubscriber = GlobalMessagePipe.GetSubscriber<PopUpResultEvent>();
            GridManager.OnBlockInfected += OnBlockInfected;
        }

        private void OnBlockHovered(ItemBlockHoveredEvent itemBlockHoveredEvent)
        {
            if (itemBlockHoveredEvent.item != this) return;
            if (!itemBlockHoveredEvent.block || 
                !itemBlockHoveredEvent.block.IsPlaced || 
                itemBlockHoveredEvent.block.BlockState is BlockState.Infected)
            {
                if (_blockHovered) _blockHovered.StopFlashing();
                _blockHovered = null;
                return;
            }
            if (_blockHovered && _blockHovered == itemBlockHoveredEvent.block) return;
            if (_blockHovered) _blockHovered.StopFlashing();
            _blockHovered = itemBlockHoveredEvent.block;
            _blockHovered.StartFlashing();
        }

        private void OnBlockInfected(Block block)
        {
            if (!Selectable())
            {
                Cancel();
                return;
            }
            if (block != _blockHovered) return;
            if (!_popUpActive)
            {
                if (_blockHovered) _blockHovered.StopFlashing();
                _blockHovered = null;
                return;
            }
            Cancel();
            _blockHovered = null;
        }

        public override void Shutdown()
        {
            _blockHoveredSubscriber?.Dispose();
            _popUpDisposable?.Dispose();
            GridManager.OnBlockInfected -= OnBlockInfected;
        }

        public override bool Selectable()
        {
            if (!ItemManager.Instance.CheckItemCount(ItemData.ItemType, 1)) return false;
            if (GridManager.Instance.BlocksOnGrid.Count == 0) return false;
            if (GridManager.Instance.BlocksOnGrid.All(x => x.BlockState == BlockState.Infected)) return false;
            return true;
        }

        public override void Select()
        {
            GameManager.Instance.CurrentGameState.Value = GameState.UseItem;
            GridManager.OnBlockInfected += OnBlockInfected;
        }

        public override void Cancel()
        {
            if (_blockHovered) _blockHovered.StopFlashing();
            _popUpActive = false;
            _popUpDisposable?.Dispose();
            NotifyCancelled();
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
            GridManager.OnBlockInfected -= OnBlockInfected;
        }

        public override void Use()
        {
            if (!Selectable() || !_blockHovered)
            {
                Cancel();
                return;
            }

            var popUpChoices = new List<PopUpChoiceData>();
            var hoveredBlockType = _blockHovered.BlockType;
            var blockTypes = Enum.GetValues(typeof(BlockTypes)).Cast<BlockTypes>().ToArray();
            for (var i = 0; i < blockTypes.Length; i++)
            {
                if (blockTypes[i] == hoveredBlockType) continue;
                var choice = new PopUpChoiceData(
                    blockTypes[i].ToString(),
                    i
                );
                popUpChoices.Add(choice);
            }

            var popUpData = new PopUpData(
                "Select Color",
                true,
                popUpChoices.ToArray()
            );
            if (!PopUpManager.Instance.TryCreatePopUp(popUpData, out var guid))
            {
                Cancel();
                return;
            }
            _popUpActive = true;
            var guidFilter = new GuidIdentifierFilter<PopUpResultEvent>(guid);
            _popUpDisposable = _popUpSubscriber.Subscribe(OnPopUpResult, guidFilter);
            if (_blockHovered) _blockHovered.StopFlashing();
        }

        private void OnPopUpResult(PopUpResultEvent popUpResultEvent)
        {
            if (popUpResultEvent.result == PopUpResult.Cancel)
            {
                Cancel();
                return;
            }

            if (popUpResultEvent.choiceId.HasValue && _blockHovered)
            {
                var blockType = (BlockTypes)popUpResultEvent.choiceId.Value;
                _blockHovered.ChangeColor(blockType);
            }
            else
            {
                Cancel();
                return;
            }

            _popUpDisposable?.Dispose();
            ItemManager.Instance.ChangeItemCount(ItemData.ItemType, -1);
            NotifyUsed();
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
        }
    }
}