using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


[Serializable]
public enum DistanceMetric {
    Manhattan,
    Euclidean,
    PathFinding,
}

public class AStarPather : MonoBehaviour
{

    public static AStarPather inst;
    private void Awake() {
        inst = this; // Singleton instance
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public Transform startPos;
    public Transform endPos;
    public DistanceMetric distanceMetric;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space)) {
            if(SelectionMgr.inst.selectedEntity != null)
                FindPath(SelectionMgr.inst.selectedEntity.position, endPos.position);
        }
    }

    public List<Node> OpenList = new List<Node>();
    public HashSet<Node> ClosedList = new HashSet<Node>();
    public Node startNode;
    public Node endNode;
    public List<Node> FindPath(Vector3 startPos, Vector3 endPos) {
        Stopwatch sw = new Stopwatch();
        startNode = GridMgr.inst.GetNodeFromWorldPos(startPos);
        endNode = GridMgr.inst.GetNodeFromWorldPos(endPos);
        OpenList.Clear();
        ClosedList.Clear();
        OpenList.Add(startNode);
        startNode.gCost = 0;

        while(OpenList.Count > 0) {
            Node currentNode = GetLowestFCostNode(OpenList);
            OpenList.Remove(currentNode);
            ClosedList.Add(currentNode);
            if(currentNode == endNode) {
                List<Node> path = RetracePath(startNode, endNode);
                sw.Stop();
                UnityEngine.Debug.Log($"Pathfinding took {sw.ElapsedMilliseconds} ms");
                return path;
            }

            foreach(Node neighbor in GridMgr.inst.GetNeighbors(currentNode)) {
                if(neighbor.isObstacle || ClosedList.Contains(neighbor)) 
                    continue;

                float newCostToNeighbor = currentNode.gCost + GetAStarDistance(currentNode, neighbor);
                if(newCostToNeighbor < neighbor.gCost || !OpenList.Contains(neighbor)) {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetAStarDistance(neighbor, endNode);
                    neighbor.parent = currentNode;
                    if(!OpenList.Contains(neighbor)) {
                        OpenList.Add(neighbor);
                    } 
                }
            }

        }
        return null;

    }

    List<Node> RetracePath(Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while(currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path = SimplifyPath(path);
        path.Reverse();

        GridMgr.inst.path = path;
        return path;
    }

    List<Node> SimplifyPath(List<Node> path) {
        List<Node> simplifiedPath = new List<Node>();
        Vector3Int lastDirection = Vector3Int.zero;

        for(int i = 1; i < path.Count; i++) {
            Vector3Int currentDirection = path[i].gridPosition - path[i - 1].gridPosition;
            if(currentDirection != lastDirection) 
                simplifiedPath.Add(path[i]);
            lastDirection = currentDirection;
        }
        return simplifiedPath;
    }

    public Node GetLowestFCostNode(List<Node> openList) {
        Node lowestFCostNode = openList[0];
        for(int i = 1; i < openList.Count; i++) {
            if(openList[i].fCost < lowestFCostNode.fCost || (openList[i].fCost == lowestFCostNode.fCost && openList[i].gCost < lowestFCostNode.gCost)) {
                lowestFCostNode = openList[i];
            }
        }
        return lowestFCostNode;
    }

    public int GetManhattanDistance(Node a, Node b) {
        return Mathf.Abs(a.gridPosition.x - b.gridPosition.x) + Mathf.Abs(a.gridPosition.z - b.gridPosition.z);
    }
    public int GetEuclideanDistance(Node a, Node b) {
        return (int) Vector3.Distance(a.position, b.position);
    }
    public int GetPathFindingDistance(Node a, Node b) {
        int xDist = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
        int zDist = Mathf.Abs(a.gridPosition.z - b.gridPosition.z);
        if(xDist > zDist) {
            return 14 * zDist + 10 * (xDist - zDist);
        } else {
            return 14 * xDist + 10 * (zDist - xDist);
        }

    }

    public int GetAStarDistance(Node a, Node b) {
        switch(distanceMetric) {
            case DistanceMetric.Manhattan:
                return GetManhattanDistance(a, b);
            case DistanceMetric.Euclidean:
                return GetEuclideanDistance(a, b);
            case DistanceMetric.PathFinding:
                return GetPathFindingDistance(a, b);
            default:
                return GetPathFindingDistance(a, b);
        }
    }
}
