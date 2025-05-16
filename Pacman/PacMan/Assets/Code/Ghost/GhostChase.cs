using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostChase : GhostBehavior {

    public List<Vector2Int> availableDirections = new List<Vector2Int>();
    public Vector2Int chaseMovDirection = Vector2Int.zero;
    private void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Obstacles")) {
            if(this.enabled && ghost.State != GhostState.Frightened) {
                availableDirections = FindAvailableDirections(transform.position);
                if(availableDirections.Count == 0) {
                    Debug.Log(name + " Flee has no available directions at " + transform.position);
                    return;
                }
                Vector3 closestDirection = FindClosestDirection(ghost.target.position);
                chaseMovDirection.x = Mathf.RoundToInt(closestDirection.x);
                chaseMovDirection.y = Mathf.RoundToInt(closestDirection.y);
                ghost.movement.SetDirection(chaseMovDirection);
            }
        }
    }

    public Vector3 FindClosestDirection(Vector3 targetPos) {
        Vector3 closestDirection = Vector3.zero;
        float closestDistance = float.MaxValue;
        Vector3 direction3 = Vector3.zero;
        foreach(Vector2Int direction in availableDirections) {
            direction3 = new Vector3(direction.x, direction.y, 0);
            Vector3 newPosition = transform.position + direction3;
            float distanceSq = (newPosition - targetPos).sqrMagnitude;
            if(distanceSq < closestDistance) {
                closestDistance = distanceSq;
                closestDirection = direction3;
            }
        }
        return closestDirection;
    }
}