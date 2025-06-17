using UnityEngine;

namespace MadDuck.Scripts.Utils
{
    public static class ArrayHelper
    {
        public static void PrintSchema(int[,] schema)
        {
            string schemaString = "\n";
            for (int i = 0; i < schema.GetLength(0); i++)
            {
                for (int j = 0; j < schema.GetLength(1); j++)
                {
                    schemaString += schema[i, j] + " ";
                }
                schemaString += "\n";
            }
            Debug.Log(schemaString);
        }
    
        public static int[,] Rotate90(int[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            int[,] rotatedArray = new int[cols, rows];

            // Rotate the array 90 degrees clockwise
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    rotatedArray[j, rows - 1 - i] = array[i, j];
                }
            }

            return rotatedArray;
        }
    
        public static int[,] Rotate180(int[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            int[,] rotatedArray = new int[rows, cols];

            // Rotate 180 degrees
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    // Map (i, j) to the rotated position
                    rotatedArray[rows - 1 - i, cols - 1 - j] = array[i, j];
                }
            }

            return rotatedArray;
        }
    
        public static int[,] Rotate270(int[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            int[,] rotatedArray = new int[cols, rows];

            // Rotate the array 90 degrees counterclockwise
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    rotatedArray[cols - 1 - j, i] = array[i, j];
                }
            }

            return rotatedArray;
        }
    
        public static bool CanBFitInA(int[,] a, int[,] b, out int[,] placedArray, bool simulatePlacement = false)
        {
            int rowsA = a.GetLength(0);
            int colsA = a.GetLength(1);
            int rowsB = b.GetLength(0);
            int colsB = b.GetLength(1);
            placedArray = a;

            // Check if B can even fit into A
            if (rowsB > rowsA || colsB > colsA) 
                return false;

            // Slide a window over A and compare
            for (int i = 0; i <= rowsA - rowsB; i++)
            {
                for (int j = 0; j <= colsA - colsB; j++)
                {
                    if (!CompareMemberBToA(a, b, i, j, out var tempPlacedArray, simulatePlacement)) continue;
                    placedArray = simulatePlacement ? tempPlacedArray : a; // If not simulating, return original array
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Perform element-by-element comparison of member B to member A at position (ax, ay).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="placedArray"></param>
        /// <param name="simulatePlacement"></param>
        /// <returns></returns>
        private static bool CompareMemberBToA(int[,] a, int[,] b, int ax, int ay, out int[,] placedArray, bool simulatePlacement = false)
        {
            int rowsA = a.GetLength(0);
            int colsA = a.GetLength(1);
            int rowsB = b.GetLength(0);
            int colsB = b.GetLength(1);
            placedArray = a;
            var tempPlacedArray = a.Clone() as int[,];

            if (rowsB > rowsA || colsB > colsA)
            {
                return false; // B cannot fit in A
            }
            
            // Compare each element
            for (int i = 0; i < rowsB; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    if (b[i, j] != 0 && a[i + ax, j + ay] != b[i, j])
                    {
                        return false;
                    }
                    if (!simulatePlacement) continue;
                    // If simulating placement, mark the position in placedArray
                    if (tempPlacedArray != null) tempPlacedArray[i + ax, j + ay] = 0;
                }
            }
            placedArray = simulatePlacement ? tempPlacedArray : a; // If not simulating, return original array
            return true;
        }
    }
}
