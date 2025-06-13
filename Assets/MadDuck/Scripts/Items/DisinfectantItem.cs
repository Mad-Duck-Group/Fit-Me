using System;
using System.Linq;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;
using MessagePipe;

namespace MadDuck.Scripts.Items
{
    [Serializable]
    public class DisinfectantItem : Item
    {
        private IDisposable _blockHoveredSubscriber;
        private Block _blockHovered;
        
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
                itemBlockHoveredEvent.block.BlockState is BlockState.Normal)
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
        
        public override void Shutdown()
        {
            _blockHoveredSubscriber?.Dispose();
        }

        public override bool Selectable()
        {
            if (!ItemManager.Instance.CheckItemCount(ItemData.ItemType, 1)) return false;
            if (GridManager.Instance.BlocksOnGrid.Count == 0) return false;
            if (GridManager.Instance.BlocksOnGrid.All(x => x.BlockState == BlockState.Normal)) return false;
            return true;
        }

        public override void Select()
        {
            GameManager.Instance.CurrentGameState.Value = GameState.UseItem;
        }

        public override void Cancel()
        {
            if (_blockHovered) _blockHovered.StopFlashing();
            NotifyCancelled();
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
        }

        public override void Use()
        {
            if (!Selectable() || !_blockHovered)
            {
                Cancel();
                return;
            }
            
            _blockHovered.StopFlashing();
            GridManager.Instance.DisinfectBlock(_blockHovered);
            _blockHovered = null;
            NotifyUsed();
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
        }
    } 
}