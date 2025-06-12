using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MadDuck.Scripts.Items;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Managers
{
    public class ItemManager : MonoSingleton<ItemManager>
    {
        [Title("Item References")]
        [SerializeField]
        private SerializableDictionary<ItemType, int> itemRecords = new()
        {
            { ItemType.DestroyColor, 0 },
            { ItemType.ChangeColor, 0 },
            { ItemType.Disinfectant, 0 }
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
        [Button("Save All Items")]
        private void DebugSaveAllItems() => SaveAllItems();

        public static event Action<ItemType, int> OnItemCountChanged;
        
        
        private void OnEnable()
        {
            SaveManager.OnLoadCompleted += LoadAllItems;
        }
        
        private void OnDisable()
        {
            SaveManager.OnLoadCompleted -= LoadAllItems;
        }
        
        private void Start()
        {
            InitializeItems();
        }

        private void InitializeItems()
        {
            foreach (var records in itemRecords)
            {
                var itemType = records.Key;
                var itemData = itemDataDictionary[itemType];
                if (!itemData)
                {
                    Debug.LogWarning($"Item data for {itemType} is not set.");
                    continue;
                }
                var item = ItemFactory.CreateItem(itemType, itemData);
                items.Add(item);
                var itemView = Instantiate(itemViewPrefab, itemViewParent);
                itemView.Initialize(item);
                itemViews.Add(itemView);
            }
        }

        #region Save/Load
        private void LoadItem(ItemType type)
        {
            var itemCount = SaveManager.Instance.CurrentSaveFile.GetData(type.ToString(), -1);
            if (itemCount > -1)
            {
                SetItemCount(type, itemCount);
            }
            else
            {
                Debug.LogWarning($"No saved data found for item type {type}. Defaulting to 0.");
                SetItemCount(type, 0);
            }
        }
        
        private void LoadAllItems()
        {
            foreach (var itemType in itemRecords.Keys.ToList())
            {
                LoadItem(itemType);
            }
        }

        private void SaveItem(ItemType type, bool saveImmediately = true)
        {
            if (!itemRecords.ContainsKey(type))
            {
                Debug.LogWarning($"Item of type {type} not found in records.");
                return;
            }
            int itemCount = itemRecords[type];
            SaveManager.Instance.CurrentSaveFile.AddOrUpdateData(type.ToString(), itemCount);
            Debug.Log($"Saved {itemCount} of item type {type}.");
            if (saveImmediately)
            {
                SaveManager.Instance.Save();
            }
        }

        private void SaveAllItems()
        {
            foreach (var itemType in itemRecords.Keys.ToList())
            {
                SaveItem(itemType, false);
            }
            SaveManager.Instance.Save();
        }
        #endregion

        #region Utils
        public bool CheckItemCount(ItemType itemType, int requiredCount)
        {
            if (!itemRecords.ContainsKey(itemType))
            {
                Debug.LogWarning($"Item of type {itemType} not found in records.");
                return false;
            }
            var itemCount = itemRecords[itemType];
            return itemCount >= requiredCount;
        }
        
        public void ChangeItemCount(ItemType itemType, int changeAmount)
        {
            if (!itemRecords.ContainsKey(itemType))
            {
                Debug.LogWarning($"Item of type {itemType} not found in records.");
                return;
            }
            itemRecords[itemType] += changeAmount;
            if (itemRecords[itemType] < 0) itemRecords[itemType] = 0; // Ensure count doesn't go negative
            SaveItem(itemType);
            OnItemCountChanged?.Invoke(itemType, itemRecords[itemType]);
        }
        
        public void SetItemCount(ItemType itemType, int count)
        {
            if (!itemRecords.ContainsKey(itemType))
            {
                Debug.LogWarning($"Item of type {itemType} not found in records.");
                return;
            }
            itemRecords[itemType] = count;
            SaveItem(itemType);
            OnItemCountChanged?.Invoke(itemType, itemRecords[itemType]);
        }
        #endregion
    }
}
