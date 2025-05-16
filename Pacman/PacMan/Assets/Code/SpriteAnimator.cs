
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public SpriteRenderer spriteRenderer { get; private set; }

    public Sprite[] sprites;
    public float animationInterval = 0.25f;
    public int frameIndex {get; private set;}
    public bool loop = true;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start() {
        InvokeRepeating(nameof(Advance), animationInterval, animationInterval);
    }

    void Advance() {
        if(!this.spriteRenderer.enabled) return;

        frameIndex = (frameIndex + 1) % sprites.Length;
        if(frameIndex >= 0 && frameIndex < sprites.Length) {
            this.spriteRenderer.sprite = sprites[frameIndex];
        }
    }

    public void Restart() {
        frameIndex = -1;
        Advance();
    }

}
