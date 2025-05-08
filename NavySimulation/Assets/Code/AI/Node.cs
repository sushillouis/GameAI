using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Node 
{
    public Vector3 position;
    public Vector3Int gridPosition;
    public bool isObstacle;
    public Node parent;

    public float gCost;
    public float hCost;

    public float fCost    {
        get {
            return gCost + hCost;
        }
    }

    public Node(Vector3 position, Vector3Int gridPosition, bool isObstacle) {
        this.position = position;
        this.gridPosition = gridPosition;
        this.parent = null;
        this.isObstacle = isObstacle;
    }

}
