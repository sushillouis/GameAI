
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ghost))]
public class GhostBehavior : MonoBehaviour
{

    public Ghost ghost;
    public float duration;

    private void Awake() {
        ghost = GetComponent<Ghost>();
        this.enabled = false;
    }
    
    public void SetDuration(float duration) {
        this.duration = duration;
    }

    public void Enable() {
        Enable(this.duration);
    }

    public virtual void Enable(float duration) {
        this.duration = duration;
        this.enabled = true;
        Debug.Log("Enabled behavior: " + ghost.name);
        CancelInvoke();
        Invoke(nameof(Disable), duration);
    }
    
    public virtual void Disable() {
        this.enabled = false;
        CancelInvoke();
    }




    public List<Vector2Int> FindAvailableDirections(Vector3 position) {
        List<Vector2Int> directions = new List<Vector2Int>();
        Vector2 size = new Vector2(0.4f, 0.4f); // Size of the box cast

        Vector2Int pos2D = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));

        RaycastHit2D hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.up, 1.5f, ghost.obsLayerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.up);
        }
        hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.down, 1.5f, ghost.obsLayerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.down);
        }
        hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.left, 1.5f, ghost.obsLayerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.left);
        }
        hit = Physics2D.BoxCast(pos2D, size, 0, Vector2.right, 1.5f, ghost.obsLayerMask);
        if(hit.collider == null) {
            directions.Add(Vector2Int.right);
        }
        return directions;
    }



}
