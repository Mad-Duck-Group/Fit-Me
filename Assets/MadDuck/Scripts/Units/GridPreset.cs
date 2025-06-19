using MadDuck.Scripts.Managers;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    [CreateAssetMenu(fileName = "Grid Preset", menuName = "MadDuck/Grid Preset", order = 1)]
    [ShowOdinSerializedPropertiesInInspector]
    public class GridPreset : SerializedScriptableObject
    {
        #region Inspectors
        [TitleGroup("Grid Settings")]
        [field: SerializeField]
        [field: ValidateInput("@PresetGridType != GridType.All && PresetGridType != GridType.None", 
            "Grid preset must have either Rectangle or Custom grid type.")]
        //[field: UnflagEnum]
        public GridType PresetGridType { get; set; } = GridType.Rectangle;
        [TitleGroup("Grid Settings")]
        [field: SerializeField] [MinValue(1)]
        public Vector2Int GridSize { get; set; } = new(10, 10);
        [TitleGroup("Grid Settings")]
        [Button("Refresh Custom Grid"), ShowIf("@PresetGridType.HasFlag(GridType.Custom)"), DisableInPlayMode]
        private void RefreshCustomGrid()
        {
            var newCustomGrid = new bool[GridSize.y, GridSize.x];
            var oldRow = CustomGrid.GetLength(0);
            var oldColumn = CustomGrid.GetLength(1);
            var newRow = newCustomGrid.GetLength(0);
            var newColumn = newCustomGrid.GetLength(1);
            for (int x = 0; x < newRow; x++)
            {
                for (int y = 0; y < newColumn; y++)
                {
                    if (x >= newRow || y >= newColumn)
                    {
                        continue;
                    }
                    if (x >= oldRow || y >= oldColumn)
                    {
                        newCustomGrid[x, y] = false;
                        continue;
                    }
                    if (x < newRow && y < GridSize.x)
                    {
                        newCustomGrid[x, y] = CustomGrid[x, y];
                    }
                    else
                    {
                        newCustomGrid[x, y] = false;
                    }
                }
            }
            CustomGrid = newCustomGrid;
        }
        [TitleGroup("Grid Settings")]
        [Button("Clear Custom Grid"), ShowIf("@PresetGridType.HasFlag(GridType.Custom)"), DisableInPlayMode]
        private void ClearCustomGrid()
        {
            CustomGrid = new bool[GridSize.y, GridSize.x];
        }
        [TitleGroup("Grid Settings")]
        [field: TableMatrix(SquareCells = true, HorizontalTitle = "Custom Grid",
            DrawElementMethod = nameof(DrawCustomGridMatrix), Transpose = true)]
        [field: SerializeField, ShowIf("@PresetGridType.HasFlag(GridType.Custom)")]
        public bool[,] CustomGrid { get; set; }= { };
        #endregion

        #region Table Matrix
        private static bool DrawCustomGridMatrix(Rect rect, bool value)
        {
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                value = !value;
                GUI.changed = true;
                Event.current.Use();
            }

            EditorGUI.DrawRect(rect.Padding(1), value ? Color.green : Color.grey);
            return value;
        }
        #endregion
    }
}