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

        /// <summary>
        /// Initialize the item the first time it is created.
        /// </summary>
        /// <param name="itemData"></param>
        public virtual void Initialize(ItemData itemData)
        {
            ItemData = itemData;
        }
        /// <summary>
        /// Shutdown the item and clean up any resources or subscriptions.
        /// </summary>
        public abstract void Shutdown();
        /// <summary>
        /// Check if the item is selectable.
        /// </summary>
        /// <returns>true if selectable, false otherwise</returns>
        public abstract bool Selectable();
        /// <summary>
        /// Select the item and prepare it for use.
        /// </summary>
        public abstract void Select();
        /// <summary>
        /// Cancel the item usage.
        /// </summary>
        public abstract void Cancel();
        /// <summary>
        /// Use the item.
        /// </summary>
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