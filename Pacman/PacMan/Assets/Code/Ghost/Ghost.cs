using System;
using System.Runtime.InteropServices;
using UnityEngine;


public class Ghost : MonoBehaviour {

    public int points = 200;
    public GhostState State = GhostState.Home;

    public Movement movement;

    [Header("Ghost Behaviors")]
    public GhostHome home;
    public GhostChase chase;
    public GhostFrightened frightened;
    public GhostScatter scatter;
    public GhostBehavior initialBehavior;
    public GhostBehavior currentBehavior;

    public Transform target;

    public LayerMask obsLayerMask; // Layer mask for obstacles

    private void Awake() {
        movement = GetComponent<Movement>();
        home = GetComponent<GhostHome>();
        chase = GetComponent<GhostChase>();
        frightened = GetComponent<GhostFrightened>();
        scatter = GetComponent<GhostScatter>();
    }

    private void Start() {
        ResetState();
    }
    public void ResetState() {
        gameObject.SetActive(true);
        movement.ResetState();
        if(initialBehavior != null)
            initialBehavior.Enable();
    }

    public void SetPosition(Vector3 position) {
        transform.position = position;
    }
    //--------------------------------------------------
    public void DisableAll() {
        chase.enabled = false;
        chase.CancelInvoke();
        scatter.enabled = false;
        scatter.CancelInvoke();
        frightened.enabled = false;
        frightened.CancelInvoke();
        home.enabled = false;
        home.CancelInvoke();

        //--------------------------------------------------
    }
}