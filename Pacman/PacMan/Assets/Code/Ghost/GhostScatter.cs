using System.Collections.Generic;
using UnityEngine;

public class GhostScatter : GhostBehavior {

    public List<Vector2Int> availableDirections = new List<Vector2Int>();

    private void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Obstacles")) {
            if(this.enabled && ghost.State != GhostState.Frightened) {
                availableDirections = FindAvailableDirections(transform.position);
                if(availableDirections.Count == 0) {
                    Debug.Log(name + " has no available directions at " + transform.position);
                    return;
                }
                int index = Random.Range(0, availableDirections.Count);
                if(availableDirections[index] == -ghost.movement.currentDirection && availableDirections.Count > 1) {
                    index = (index + 1) % availableDirections.Count;
                }
                ghost.movement.SetDirection(availableDirections[index]);
            }
        }
    }

    public override void Disable() {
        
    }

}
