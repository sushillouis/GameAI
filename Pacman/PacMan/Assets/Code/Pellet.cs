using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int points = 10;
    public bool isNavNode = false;
    public List<Vector2Int> directions;
    public SpriteRenderer spriteRenderer;
    public bool isEaten = false;

    private void Awake() {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }


    private void OnTriggerEnter2D(Collider2D other) {
        if(other.gameObject.GetComponent<Pacman>() != null && !isEaten) {
            Eat();
        }

        if(isNavNode) {
            //Debug.Log("Nav Node Triggered @: " + transform.position + " by " + other.gameObject.sfx);
            GhostNew ghost = other.gameObject.GetComponent<GhostNew>();
            if(ghost != null) {
                //Debug.Log(other.gameObject.sfx + " Triggered Nav Node");
                ghost.physics.HandleNavigation(this.transform.position, directions);
            }
        }
    }

    [ContextMenu("Set Nav Node")]
    void SetNavNode() {//for testing
        isNavNode = true;
        directions = Utils.FindAvailableDirections(this.transform.position, GridMgr.instance.obsLayerMask);
    }

    public void SetNavNode(List<Vector2Int> dirs) {
        isNavNode = true;
        directions = dirs;
    }

    protected virtual void Eat() {
        this.spriteRenderer.enabled = false;
        isEaten = true;
        GameMgr.instance.PelletEaten(this);
    }



}
