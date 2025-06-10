using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.Items
{
    public enum UsageMode
    {
        Click,
        DragAndDrop
    }
    
    [CreateAssetMenu(fileName = "ItemData", menuName = "MadDuck/Items/ItemData", order = 0)]
    public class ItemData : ScriptableObject
    {
        [field: SerializeField] 
        public string ItemName { get; private set; }
        [field: SerializeField, TextArea] 
        public string ItemDescription { get; private set; }
        [field: SerializeField] 
        public ItemType ItemType { get; private set; }
        [field: SerializeField]
        public UsageMode UsageMode { get; private set; }
        [field: SerializeField, PreviewField] 
        public Sprite ItemIcon { get; private set; }
    }
}