using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[System.Serializable]
public class CmdChaser : GhostCommand {

    public CmdChaser(GhostNew ghost) : base(ghost) {

    }

    public CmdChaser(GhostNew ghost, float duration) : base(ghost, duration) {
        //this.duration = duration;
    }

    public override void Init() {
        base.Init();
        SfxMgr.instance.Play(SfxType.GhostChase);
    }

    public override void Tick(float dt) {
        base.Tick(dt);



    }

    public override Vector2Int GetNewDirection(Vector3 pos, List<Vector2Int> directions) {

        if(!IsDone()) {
            //List<Vector2Int> directions = Utils.FindAvailableDirections(pos, ghost.obsLayerMask);
            return GetChaserDirection(directions, pos);
        } else {
            return Vector2Int.zero;
        }
    }

    public Vector2Int GetChaserDirection(List<Vector2Int> directions, Vector3 position) {
        Vector2Int chaseDirection = Vector2Int.zero;
        float minDistance = float.MaxValue;
        float distanceSq = minDistance;
        Vector3 direction = new Vector3(0, 0, 0);
        foreach(Vector2Int dir in directions) {
            direction.x = dir.x;
            direction.y = dir.y;
            Vector3 newPos = position + direction;
            distanceSq = (newPos - ghost.target.position).sqrMagnitude;
            if(distanceSq < minDistance) {
                minDistance = distanceSq;
                chaseDirection = dir;
            }
        }
        return chaseDirection;
    }

}
