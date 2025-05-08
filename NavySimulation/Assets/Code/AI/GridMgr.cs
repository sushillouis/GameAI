using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMgr : MonoBehaviour
{
    public static GridMgr inst;
    private void Awake() {
        inst = this; // Singleton instance
    }

    [Header("Grid Settings")]
    public int gridXSize = 4000; // Width of the grid
    public int gridZSize = 4000; // Length of the grid
    public Vector3 bottomLeft = Vector3.zero;

    public Node[,] grid; // 2D array to hold the nodes
    public bool gridDone = false;
    public int nCellsX;
    public int nCellsZ;
    public float cellSize = 200f; // Size of each cell in the grid
    public float halfCellSize;

    [Header("Obstacles Settings")]
    public int nObstacles = 0; // Number of obstacles in the grid
    public int maxObsSize = 5;
    public float minSize = 100f;
    public float maxSize = 1000f;

    public List<Node> obstacleNodes = new List<Node>();

    [Header("Path")]
    public List<Node> path;
    // Start is called before the first frame update
    void Start()  {
        StartCoroutine(InitGrid(cellSize));
        //GenerateObstacles(nObstacles); // Generate 100 obstacles
    }

    IEnumerator InitGrid(float cellSize) {
        gridDone = false;
        halfCellSize = cellSize / 2;

        nCellsX = Mathf.RoundToInt(gridXSize / cellSize);
        nCellsZ = Mathf.RoundToInt(gridZSize / cellSize);
        grid = new Node[nCellsX, nCellsZ];


        bottomLeft = new Vector3(-gridXSize / 2, 0f, -gridZSize / 2) ; // Bottom left corner of the grid

        GenerateObstacles2(nObstacles); // Generate nObstacle obstacles
        yield return null;

        for(int i = 0; i < nCellsX; i++) {
            for(int j = 0; j < nCellsZ; j++) {
                Vector3 position = new Vector3(bottomLeft.x + (i * cellSize) + halfCellSize, bottomLeft.y, bottomLeft.z + (j * cellSize) + halfCellSize);
                Vector3Int gridPosition = new Vector3Int(i, 0, j);
                grid[i, j] = new Node(position, gridPosition, false);
                if(Physics.CheckSphere(position, halfCellSize, obstacleLayerMask)) { // Check if the cell is an obstacle
                    grid[i, j].isObstacle = true;
                    obstacleNodes.Add(grid[i, j]);
                }
            }

        }
        gridDone = true;
        yield return null;
    }


    public Node GetNodeFromWorldPos(Vector3 pos) {
        float xFrac = Mathf.Clamp01((pos.x + gridXSize / 2) / gridXSize);
        float zFrac = Mathf.Clamp01((pos.z + gridZSize / 2) / gridZSize);
        int x = Mathf.RoundToInt(xFrac * (nCellsX - 1));
        int z = Mathf.RoundToInt(zFrac * (nCellsZ - 1));
        return grid[x, z];
    }

    public List<Node> GetNeighbors(Node node) {
        List<Node> neighbors = new List<Node>();
        for(int i = -1; i <= 1; i++) {
            for(int j = -1; j <= 1; j++) {
                if(i == 0 && j == 0)
                    continue; // Skip the node itself
                int x = node.gridPosition.x + i;
                int z = node.gridPosition.z + j;
                if(x >= 0 && x < nCellsX && z >= 0 && z < nCellsZ) {
                    neighbors.Add(grid[x, z]);
                }
            }
        }
        return neighbors;
    }



    public GameObject ObstaclePrefab;
    public List<GameObject> obstacles = new List<GameObject>();
    public LayerMask obstacleLayerMask;


    // Method to generate obstacles in the grid
    [ContextMenu("Generate Obstacles")]
    public void GenerateObstacles() {
        GenerateObstacles(nObstacles);
    }
    public void GenerateObstacles(int nObstacles) {
        for(int i = 0; i < nObstacles; i++) {
            Vector3 pos = new Vector3(Random.Range(-gridXSize / 2, gridXSize / 2), 0f, Random.Range(-gridZSize / 2, gridZSize / 2));
            CreateObstacle(pos);
            int x = Random.Range(0, nCellsX);
            int z = Random.Range(0, nCellsZ);
            for(int j = 0; j < Random.Range(0, maxObsSize); j++) {
                for(int k = 0; k < Random.Range(0, maxObsSize); k++) { // an obstacles is made of multiple cells
                    int ix = Mathf.Clamp(x + j, 0, nCellsX - 1);
                    int iz = Mathf.Clamp(z + k, 0, nCellsZ - 1);
                    grid[ix, iz].isObstacle = true;
                }
            }

        }
    }

    public void GenerateObstacles2(int nObstacles) {
        obstacles.Clear();
        for(int i = 0; i < nObstacles; i++) {
            Vector3 pos = new Vector3(Random.Range(-gridXSize / 2, gridXSize / 2), 0f, Random.Range(-gridZSize / 2, gridZSize / 2));
            GameObject obs = CreateObstacle(pos);
            obstacles.Add(obs);
        }
    }


    GameObject CreateObstacle(Vector3 pos) {
        float length = Random.Range(minSize, maxSize);
        float width = Random.Range(minSize, maxSize);
        GameObject obstacle = Instantiate(ObstaclePrefab, pos, Quaternion.identity, transform);
        obstacle.transform.localScale = new Vector3(length, 100f, width);
        return obstacle;
    }

    public bool drawGrid = false;
    private void OnDrawGizmos() {

        if(drawGrid)
            DrawGrid();
        DrawObstacles();
        DrawPath(path);
    }

    void DrawGrid() {
        if(grid != null && gridDone) {
            for(int i = 0; i < nCellsX; i++) {
                for(int j = 0; j < nCellsZ; j++) {
                    Gizmos.color = Color.grey;
                    Gizmos.DrawWireCube(grid[i, j].position, new Vector3(cellSize, 0f, cellSize) * 0.9f);
                }
            }
        }
    }
    void DrawObstacles() {
        if(grid != null && gridDone) {
            for(int i = 0; i < nCellsX; i++) {
                for(int j = 0; j < nCellsZ; j++) {
                    if(grid[i, j].isObstacle) {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(grid[i, j].position, new Vector3(cellSize, 0f, cellSize) * 0.9f);
                    }
                }
            }
        }
    }

    void DrawPath(List<Node> path) {
        if(path != null) {
            Gizmos.color = Color.green;
            foreach(Node node in path) {
                Gizmos.DrawSphere(node.position, cellSize * 0.2f);
            }
        }
    }
}
