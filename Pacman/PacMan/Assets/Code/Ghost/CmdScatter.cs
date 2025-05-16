using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdScatter : GhostCommand
{
    public CmdScatter(GhostNew ghost) : base(ghost) {
        this.duration = 4;
    }

    public CmdScatter(GhostNew ghost, float duration) : base(ghost, duration) {
        this.duration = duration;
    }

    public override void Init() {
        base.Init();
    }

    public override void Tick(float dt) {
        base.Tick(dt);

    }



    public override Vector2Int GetNewDirection(Vector3 pos, List<Vector2Int> directions) {

        if(!IsDone()) {
            //List<Vector2Int> directions = Utils.FindAvailableDirections(pos, ghost.obsLayerMask);
            if(directions.Count == 0) {
                Debug.Log(ghost.name + ": no directions available @ " + pos);
                return Vector2Int.zero;
            } else {
                return directions[Random.Range(0, directions.Count)];
            }
        } else {
            return Vector2Int.zero;
        }

    }

}
