
using System.Collections.Generic;
using UnityEngine;

public class CmdFrightened : GhostCommand
{

    public CmdFrightened(GhostNew ghost) : base(ghost) {

    }

    public CmdFrightened(GhostNew ghost, float duration) : base(ghost, duration) {
        //this.duration = duration;
    }

    public override void Init() {
        base.Init();
    }

    public override void Tick(float dt) {
        base.Tick(dt);
        if(ghost.isEaten) {
            ghost.isEaten = false; //reset eaten state
            ghost.GoHome(); //teleport home
            ghost.SetFrightenedEatenAppearance();//just eyes showing
        }
    }

    public override Vector2Int GetNewDirection(Vector3 pos, List<Vector2Int> directions) {

        if(!IsDone()) {
            //List<Vector2Int> directions = Utils.FindAvailableDirections(pos, ghost.obsLayerMask);
            return GetFleeDirection(directions, pos);
        } else {
            return Vector2Int.zero;
        }
    }

    public Vector2Int GetFleeDirection(List<Vector2Int> directions, Vector3 position) {
        Vector2Int chaseDirection = Vector2Int.zero;
        float maxDistance = float.MinValue;
        float distanceSq = maxDistance;
        Vector3 direction = new Vector3(0, 0, 0);
        foreach(Vector2Int dir in directions) {
            direction.x = dir.x;
            direction.y = dir.y;
            Vector3 newPos = position + direction;
            distanceSq = (newPos - ghost.target.position).sqrMagnitude;
            if(distanceSq > maxDistance) {
                maxDistance = distanceSq;
                chaseDirection = dir;
            }
        }
        return chaseDirection;
    }



}
