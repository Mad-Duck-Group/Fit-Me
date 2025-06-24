using System;
using System.Collections.Generic;
using MadDuck.Scripts.Utils;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    
    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public record BlockSchema
    {
        [field: TableMatrix(SquareCells = true, Transpose = true, DrawElementMethod = nameof(DrawSchemaMatrix))]
        [field: SerializeField]
        public int[,] schema = { };
        public int Index { get; private set; }

        public BlockSchema(Vector2Int size)
        {
            schema = new int[size.y, size.x];
        }
        
        public BlockSchema(int[,] schema, int index)
        {
            this.schema = schema;
            Index = index;
        }
        
        private static int DrawSchemaMatrix(Rect rect, int value)
        {
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                value = value == 1 ? 0 : 1; // Toggle between 0 and 1
                GUI.changed = true;
                Event.current.Use();
            }

            EditorGUI.DrawRect(rect.Padding(1), value == 1 ? Color.green : Color.grey);
            return value;
        }
    }
    
    [CreateAssetMenu(fileName = "Block Preset", menuName = "MadDuck/Block Preset", order = 1)]
    [ShowOdinSerializedPropertiesInInspector]
    public class BlockPreset : SerializedScriptableObject
    {
        [field: TitleGroup("Block Preset Settings")]
        [field: SerializeField] public Sprite BlockSprite { get; private set; }
        [field: TitleGroup("Block Preset Settings")]
        [field: SerializeField] [field: MinValue(1)] 
        public Vector2Int BlockSize { get; private set; } = new(3, 3);

        [field: TitleGroup("Block Preset Settings")]
        [field: OdinSerialize, HideReferenceObjectPicker]
        public BlockSchema BlockSchema { get; private set; } = new(new Vector2Int(3, 3));
        [TitleGroup("Block Preset Settings")]
        [Button("Refresh Schema")]
        private void RefreshSchema()
        {
            ArrayHelper.ResizeArrayKeepMembers(ref BlockSchema.schema, BlockSize);
        }
        
        [field: TitleGroup("Block Debug")]
        [field: OdinSerialize, HideReferenceObjectPicker]
        public List<BlockSchema> BlockSchemas { get; private set; } = new();
        [TitleGroup("Block Debug")]
        [Button("Test Schema")]
        public void GenerateSchema()
        {
            BlockSchemas.Clear();
            var originalBlockSchema = new BlockSchema(BlockSchema.schema.Clone() as int[,], 0);
            BlockSchemas.Add(new BlockSchema(originalBlockSchema.schema, 0));
            BlockSchemas.Add(new BlockSchema(ArrayHelper.Rotate270(originalBlockSchema.schema), 1));
            BlockSchemas.Add(new BlockSchema(ArrayHelper.Rotate180(originalBlockSchema.schema), 2));
            BlockSchemas.Add(new BlockSchema(ArrayHelper.Rotate90(originalBlockSchema.schema), 3));
        }
    }
}