using System.Collections.Generic;
using UnityEngine;

public class GhostPhysics : MonoBehaviour
{

    public Vector3 direction;
    public float speed = 10;
    public GhostNew ghost;

    private void Awake() {
        ghost = GetComponent<GhostNew>();
    }

    private void FixedUpdate() {
        CenterOnPath();
        transform.position += direction * speed * Time.fixedDeltaTime;
    }

    [ContextMenu("Center On Path")]
    void CenterOnPath() {
        Vector3 pos = transform.position;
        pos.z = 0;
        if(direction.x == 0)
            pos.x = Mathf.Round(pos.x);
        if(direction.y == 0)
            pos.y = Mathf.Round(pos.y);

        transform.position = pos;
    }

    public void SetDirection(Vector2Int dir) {
        direction.x = dir.x;
        direction.y = dir.y;
        direction.z = 0;
    }

    //for tiles with no pellets
    private void OnCollisionEnter2D(Collision2D collision) {
        if(!HandleGhostPacman(collision)) {
            Vector3 roundedPos = RoundPosition(transform.position);
            HandleNavigation(roundedPos, Utils.FindAvailableDirections(roundedPos, ghost.obsLayerMask));
        }
    }

    //for tiles with pellets
    public void HandleNavigation(Vector3 tileCenter, List<Vector2Int> directions) {
        transform.position = tileCenter;
        //Debug.Log(sfx + " @: " + transform.position);
        Vector2Int newDir = ghost.behavior.GetNewDirection(transform.position, directions);

        SetDirection(newDir);

    }

    Vector3 RoundPosition(Vector3 position) {
        Vector3 pos = new Vector3(transform.position.x, transform.position.y, 0);
        pos.x = Mathf.Round(pos.x);
        pos.y = Mathf.Round(pos.y);
        return pos;
    }

    bool HandleGhostPacman(Collision2D collision) {
        Pacman pacman = collision.gameObject.GetComponent<Pacman>();
        if(pacman != null) {
            if(ghost.State != GhostState.Frightened && !ghost.isEaten) {
                GameMgr.instance.PacmanEaten(); //Resets ghosts and pacman starting states and pacman loses a life
                return true;
            } else if(ghost.State == GhostState.Frightened) {
                GameMgr.instance.GhostEaten(ghost); //Ghost changes state 
                                                    //goes home and TODO: shows the "only eyes" animation
                return true;
            }
        }
        return false;
    }

}
