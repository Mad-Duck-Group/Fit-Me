using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public GameObject cellPrefab;
    public int rows = 6;
    public int columns = 5;
    public float cellSize = 1.0f;
    public Vector2 startOffset = new Vector2(-2.5f, 2.5f);
    public Transform cellParent;

    void Start()
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 spawnPos = new Vector3(x * cellSize, -y * cellSize, 0) + (Vector3)startOffset;
                Instantiate(cellPrefab, spawnPos, Quaternion.identity, cellParent);
            }
        }
    }
}