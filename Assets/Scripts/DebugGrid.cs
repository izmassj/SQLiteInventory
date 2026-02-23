using UnityEngine;

public class InfiniteGrid : MonoBehaviour
{
    public int gridSize = 100;     // How far the grid extends
    public float cellSize = 1f;    // Size of each square

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        int startX = Mathf.FloorToInt(transform.position.x / cellSize) - gridSize;
        int endX = Mathf.FloorToInt(transform.position.x / cellSize) + gridSize;

        int startZ = Mathf.FloorToInt(transform.position.z / cellSize) - gridSize;
        int endZ = Mathf.FloorToInt(transform.position.z / cellSize) + gridSize;

        float yOffset = transform.position.y;

        for (int x = startX; x <= endX; x++)
        {
            float worldX = x * cellSize;

            Gizmos.DrawLine(
                new Vector3(worldX, yOffset, startZ * cellSize),
                new Vector3(worldX, yOffset, endZ * cellSize)
            );
        }

        // Horizontal lines
        for (int z = startZ; z <= endZ; z++)
        {
            float worldZ = z * cellSize;

            Gizmos.DrawLine(
                new Vector3(startX * cellSize, yOffset, worldZ),
                new Vector3(endX * cellSize, yOffset, worldZ)
            );
        }
    }
}
