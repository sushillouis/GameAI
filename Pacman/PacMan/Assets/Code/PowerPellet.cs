
using UnityEngine;

public class PowerPellet : Pellet
{
    public float duration = 8f;
    protected override void Eat() {
        this.spriteRenderer.enabled = false;
        this.isEaten = true;
        GameMgr.instance.PowerPelletEaten(this);
    }
}
