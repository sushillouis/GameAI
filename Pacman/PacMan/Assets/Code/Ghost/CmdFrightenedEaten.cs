using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdFrightenedEaten : GhostCommand
{

    public CmdFrightenedEaten(GhostNew ghost) : base(ghost) {
        //nothing more
    }
    public CmdFrightenedEaten(GhostNew ghost, float duration) : base(ghost, duration) {
        //nothing more
    }

    public override void Init() {
        base.Init();
    }

    public override void Tick(float dt) {
        base.Tick(dt);
    }

    public override void Stop() {
        base.Stop();
        ghost.GoHome();
    }

    public override Vector2Int GetNewDirection(Vector3 newDir, List<Vector2Int> directions) {
        int dirx = Random.Range(-1, 1);
        return new Vector2Int(dirx, 0);
    }


}
