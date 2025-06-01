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
    
        public static bool CanBlockFitInVacant(int[,] vacant, int[,] block)
        {
            int rowsA = vacant.GetLength(0);
            int colsA = vacant.GetLength(1);
            int rowsB = block.GetLength(0);
            int colsB = block.GetLength(1);
    
            // Check if B can even fit into A
            if (rowsB > rowsA || colsB > colsA)
                return false;
    
            var sumB = SumB(block, rowsB, colsB);
    
            // Slide a window over A and compare
            for (int i = 0; i <= rowsA - rowsB; i++)
            {
                for (int j = 0; j <= colsA - colsB; j++)
                {
                    var sumSubA = SumSubA(vacant, rowsB, colsB, i, j);
                    // If the sums match, do a detailed element-by-element check
                    if (sumSubA < sumB) continue;
                    if (CompareMembers(vacant, block, rowsB, colsB, i, j)) return true;
                }
            }
    
            return false;
        }
    
        private static int SumB(int[,] b, int rowsB, int colsB)
        {
            // Precompute the sum of all elements in B for quick comparison
            int sumB = 0;
            for (int i = 0; i < rowsB; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    sumB += b[i, j];
                }
            }
    
            return sumB;
        }
    
        private static int SumSubA(int[,] a, int rowsB, int colsB, int i, int j)
        {
            // Compute the sum of the subarray in A
            int sumSubA = 0;
            for (int m = 0; m < rowsB; m++)
            {
                for (int n = 0; n < colsB; n++)
                {
                    sumSubA += a[i + m, j + n];
                }
            }
    
            return sumSubA;
        }
    
        private static bool CompareMembers(int[,] a, int[,] b, int rowsB, int colsB, int i, int j)
        {
            bool match = true;
            for (int m = 0; m < rowsB; m++)
            {
                for (int n = 0; n < colsB; n++)
                {
                    if (a[i + m, j + n] == 1 && b[m, n] == 0) continue;
                    if (a[i + m, j + n] == b[m, n]) continue;
                    match = false;
                    break;
                }
    
                if (!match) break;
            }
    
            if (match) return true;
            return false;
        }
    }
}
