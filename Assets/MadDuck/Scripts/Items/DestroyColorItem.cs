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
            if (!itemBlockHoveredEvent.block || 
                !itemBlockHoveredEvent.block.IsPlaced || 
                itemBlockHoveredEvent.block.BlockState is BlockState.Infected)
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
                .Where(b => b.BlockType == _blockHovered.BlockType && b.BlockState is BlockState.Normal).ToList();
            sameColorBlocks.ForEach(b => b.StartFlashing());
            _blocksToDestroy = sameColorBlocks.ToList();
        }
        
        private void OnBlockInfected(Block block)
        {
            if (!Selectable())
            {
                Cancel();
                return;
            }
            if (_blocksToDestroy.Contains(block))
            {
                _blocksToDestroy.Remove(block);
                block.StopFlashing();
            }
            if (block == _blockHovered)
            {
                _blockHovered = null;
                _blocksToDestroy.ForEach(b =>
                {
                    b.StopFlashing();
                });
            }
        }

        public override void Shutdown()
        {
            _blockHoveredSubscriber?.Dispose();
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
            NotifyCancelled();
            _blocksToDestroy.ForEach(b =>
            {
                b.StopFlashing();
            });
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