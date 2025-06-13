using System;
using MadDuck.Scripts.Managers;

namespace MadDuck.Scripts.Items
{
    [Serializable]
    public class DisinfectantItem : Item
    {
        public override void Shutdown()
        {
            throw new NotImplementedException();
        }

        public override bool Selectable()
        {
            // Implement logic to determine if the disinfectant can be used
            return true; // Placeholder, replace with actual logic
        }

        public override void Select()
        {
            if (!Selectable()) return;
            ItemManager.Instance.ChangeItemCount(ItemData.ItemType, -1);
        }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        public override void Use()
        {
            throw new NotImplementedException();
        }
    } 
}