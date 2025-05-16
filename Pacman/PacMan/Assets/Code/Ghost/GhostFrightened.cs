using System.Collections.Generic;
using UnityEngine;

public class GhostFrightened : GhostBehavior
{
    public List<Vector2Int> availableDirections = new List<Vector2Int>();
    public Vector2Int fleeMovDirection = Vector2Int.right;
    private void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Obstacles")) {
            if(this.enabled && ghost.State == GhostState.Frightened) {
                availableDirections = FindAvailableDirections(transform.position);
                if(availableDirections.Count == 0) {
                    Debug.Log(name + " Flee has no available directions at " + transform.position);
                    return;
                }
                Vector3 closestDirection = FindFleeDirection(ghost.target.position);
                fleeMovDirection.x = Mathf.RoundToInt(closestDirection.x);
                fleeMovDirection.y = Mathf.RoundToInt(closestDirection.y);
                ghost.movement.SetDirection(fleeMovDirection);
            }
        }
    }

    public Vector3 FindFleeDirection(Vector3 targetPos) {
        Vector3 fleeDirection = Vector3.zero;
        float maxDistance = float.MinValue;
        Vector3 direction3 = Vector3.zero;
        foreach(Vector2Int direction in availableDirections) {
            direction3 = new Vector3(direction.x, direction.y, 0);
            Vector3 newPosition = transform.position + direction3;
            float distanceSq = (newPosition - targetPos).sqrMagnitude;
            if(distanceSq > maxDistance) {
                maxDistance = distanceSq;
                fleeDirection = direction3;
            }
        }
        return fleeDirection;
    }

    public override void Disable() {
        base.Disable();
        ghost.State = GhostState.Chasing;
    }
}
