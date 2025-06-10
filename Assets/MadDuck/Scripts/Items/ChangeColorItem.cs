using System;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;

namespace MadDuck.Scripts.Items
{
    [Serializable]
    public class ChangeColorItem : Item
    {
        public override void Shutdown()
        {
        }

        public override bool Selectable()
        {
            //TODO: change to check for infected block later.
            return GridManager.Instance.BlocksOnGrid.Count != 0;
        }
        
        public override void Select()
        {
            if (!Selectable()) return;
            ItemManager.Instance.ChangeItemCount(ItemData.ItemType, -1);
        }

        public override void Cancel()
        {
        }

        private void OnBlockSelected(Block block)
        {
            if (!block) return;
            Use();
        }

        public override void Use()
        {
            
        }
    }
}