using System;
using System.Collections.Generic;
using MadDuck.Scripts.Managers;
using UnityEngine;

namespace MadDuck.Scripts.Items
{
    public enum ItemType
    {
        Disinfectant,
        DestroyColor,
        ChangeColor
    }
    
    [Serializable]
    public abstract class Item
    {
        public ItemData ItemData { get; protected set; }
        
        public event Action OnCancelled;
        public event Action OnUsed;

        public virtual void Initialize(ItemData itemData)
        {
            ItemData = itemData;
        }
        public abstract void Shutdown();
        public abstract bool Selectable();
        public abstract void Select();
        public abstract void Cancel();
        public abstract void Use();
        
        protected void NotifyCancelled()
        {
            OnCancelled?.Invoke();
        }
        
        protected void NotifyUsed()
        {
            OnUsed?.Invoke();
        }
    }
    
    public static class ItemFactory
    {
        public static Item CreateItem(ItemType itemType, ItemData itemData)
        {
            Item item = itemType switch
            {
                ItemType.Disinfectant => new DisinfectantItem(),
                ItemType.DestroyColor => new DestroyColorItem(),
                ItemType.ChangeColor => new ChangeColorItem(),
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
            item.Initialize(itemData);
            return item;
        }
    }
}