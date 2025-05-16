using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CmdHome : GhostCommand
{
    public CmdHome(GhostNew ghost) : base(ghost) {
        //nothing more
    }
    public CmdHome(GhostNew ghost, float duration) : base(ghost, duration) {
        //duration means something else here
    }

    public override void Init() {
        base.Init();

    }

    public override void Tick(float dt) {
        base.Tick(dt);
        //go left/right randomly

    }

    public override void Stop() {
        base.Stop();
        ghost.LeaveHome(); // need a coroutine animation so has to be in a monobehavior
    }

    public override Vector2Int GetNewDirection(Vector3 newDir, List<Vector2Int> directions) {
        int dirx = Random.Range(-1, 1);
        return new Vector2Int(dirx, 0);
    }


}
