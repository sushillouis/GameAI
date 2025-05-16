using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{


    public static List<Vector2Int> FindAvailableDirections(Vector3 position, LayerMask layerMask) {
        List<Vector2Int> directions = new List<Vector2Int>();
        Vector2 size = new Vector2(0.4f, 0.4f); // Size of the box cast

        Vector2Int pos2D = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));

        RaycastHit2D hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.up, 1.5f, layerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.up);
        }
        hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.down, 1.5f, layerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.down);
        }
        hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.left, 1.5f, layerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.left);
        }
        hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.right, 1.5f, layerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.right);
        }
        return directions;
    }


}
