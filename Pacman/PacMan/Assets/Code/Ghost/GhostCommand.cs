

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GhostCommand 
{
    [SerializeField]
    protected float duration = 2;
    [SerializeField]
    private float timeRemaining = 0;
    public GhostNew ghost;
    public bool stop = false;

    public GhostCommand(GhostNew ghost) {
        this.ghost = ghost;
    }

    public GhostCommand(GhostNew ghost, float duration) {
        this.ghost = ghost;
        this.duration = duration;
    }

    public virtual void Init() {
        this.timeRemaining = this.duration;
        stop = false;
    }

    public virtual void Tick(float dt) {
        this.timeRemaining -= dt;
    }

    public virtual void Stop() {
        stop = true;
    }

    public virtual bool IsDone() {
        return this.timeRemaining <= 0 || stop;
    }

    public virtual Vector2Int GetNewDirection(Vector3 newDir, List<Vector2Int> directions) {
        return Vector2Int.zero;
    }



}
