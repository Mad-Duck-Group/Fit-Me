using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MadDuck.Scripts.Items;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Managers
{
    [Serializable]
    public record ItemRecord
    {
        public ItemType itemType;
        public int count;
        
        public ItemRecord(ItemType itemType, int count)
        {
            this.itemType = itemType;
            this.count = count;
        }
    }

    public class ItemManager : MonoSingleton<ItemManager>
    {
        [Title("Item References")] 
        [TableList] 
        [SerializeField]
        private List<ItemRecord> itemRecords = new()
        {
            new ItemRecord(ItemType.DestroyColor, 0),
            new ItemRecord(ItemType.ChangeColor, 0),
            new ItemRecord(ItemType.Disinfectant, 0)
        };
        [SerializeField]
        private SerializableDictionary<ItemType, ItemData> itemDataDictionary = new()
        {
            { ItemType.DestroyColor, null },
            { ItemType.ChangeColor, null },
            { ItemType.Disinfectant, null }
        };
        [SerializeField] private ItemView itemViewPrefab;
        [SerializeField] private Transform itemViewParent;

        [Title("Item Debug")]
        [SerializeReference, ReadOnly] private List<Item> items = new();
        [SerializeField, ReadOnly] private List<ItemView> itemViews = new();

        public static event Action<ItemType, int> OnItemCountChanged;
        
        
        private void OnEnable()
        {
            SaveManager.OnLoadCompleted += LoadItems;
        }
        
        private void OnDisable()
        {
            SaveManager.OnLoadCompleted -= LoadItems;
        }
        
        private void LoadItems()
        {
            foreach (var records in itemRecords)
            {
                var itemType = records.itemType;
                var itemData = itemDataDictionary[itemType];
                if (!itemData)
                {
                    Debug.LogWarning($"Item data for {records.itemType} is not set.");
                    continue;
                }
                var item = ItemFactory.CreateItem(itemType, itemData);
                items.Add(item);
                var itemView = Instantiate(itemViewPrefab, itemViewParent);
                itemView.Initialize(item);
                itemViews.Add(itemView);
            }
        }
        
        public bool CheckItemCount(ItemType itemType, int requiredCount)
        {
            var item = itemRecords.Find(i => i.itemType == itemType);
            if (item == null)
            {
                Debug.LogWarning($"Item of type {itemType} not found.");
                return false;
            }
            return item.count >= requiredCount;
        }
        
        public void ChangeItemCount(ItemType itemType, int changeAmount)
        {
            var item = itemRecords.Find(i => i.itemType == itemType);
            if (item == null)
            {
                Debug.LogWarning($"Item of type {itemType} not found.");
                return;
            }
            item.count += changeAmount;
            if (item.count < 0) item.count = 0; // Ensure count doesn't go negative
            OnItemCountChanged?.Invoke(itemType, item.count);
        }
    }
}
