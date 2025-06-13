using System;
using System.Collections.Generic;
using System.Linq;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;
using MessagePipe;
using Sirenix.Utilities;

namespace MadDuck.Scripts.Items
{
    [Serializable]
    public class DestroyColorItem : Item
    {
        private IDisposable _blockHoveredSubscriber;
        private Block _blockHovered;
        private List<Block> _blocksToDestroy = new();
        public override void Initialize(ItemData itemData)
        {
            base.Initialize(itemData);
            _blockHoveredSubscriber = GlobalMessagePipe.GetSubscriber<ItemBlockHoveredEvent>()
                .Subscribe(OnBlockHovered);
        }
        
        private void OnBlockHovered(ItemBlockHoveredEvent itemBlockHoveredEvent)
        {
            if (itemBlockHoveredEvent.item != this) return;
            if (!itemBlockHoveredEvent.block || !itemBlockHoveredEvent.block.IsPlaced)
            {
                _blocksToDestroy.ForEach(b =>
                {
                    b.StopFlashing();
                });
                _blockHovered = null;
                return;
            }
            if (_blockHovered && _blockHovered.BlockType == itemBlockHoveredEvent.block.BlockType) return;
            _blocksToDestroy.ForEach(b =>
            {
                b.StopFlashing();
            });
            _blockHovered = itemBlockHoveredEvent.block;
            var sameColorBlocks = GridManager.Instance.BlocksOnGrid
                .Where(b => b.BlockType == _blockHovered.BlockType).ToList();
            sameColorBlocks.ForEach(b => b.StartFlashing());
            _blocksToDestroy = sameColorBlocks.ToList();
        }

        public override void Shutdown()
        {
            _blockHoveredSubscriber?.Dispose();
        }

        public override bool Selectable()
        {
            //TODO: change to check for infected block later.
            if (!ItemManager.Instance.CheckItemCount(ItemData.ItemType, 1)) return false;
            return GridManager.Instance.BlocksOnGrid.Count != 0;
        }

        public override void Select()
        {
            GameManager.Instance.CurrentGameState.Value = GameState.UseItem;
        }

        public override void Cancel()
        {
            NotifyCancelled();
            _blocksToDestroy.ForEach(b =>
            {
                b.StopFlashing();
            });
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
        }

        public override void Use()
        {
            if (!Selectable() || !_blockHovered)
            {
                Cancel();
                return;
            }
            ItemManager.Instance.ChangeItemCount(ItemData.ItemType, -1);
            _blocksToDestroy.ForEach(b =>
            {
                b.StopFlashing();
                GridManager.Instance.RemoveBlock(b, true);
            });
            _blocksToDestroy.Clear();
            NotifyUsed();
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
        }
    }
}