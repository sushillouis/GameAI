using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum GhostState {
    Chasing,
    Scatter,
    Frightened,
    Home,
}

public class GhostNew : MonoBehaviour {
    public int points = 200;
    public float chaseDuration = 20f;
    public float scatterDuration = 4f;
    public float frightenedDuration = 5f;
    public float homeDuration = 5f;

    public Transform target; //pacman


    public GhostCommand chaser = null;
    public GhostCommand scatter = null;
    public GhostCommand frightened = null;
    public GhostCommand home = null;
    public bool isEaten = false;

    public GhostCommand behavior;
    public GhostPhysics physics;

    public LayerMask obsLayerMask; // Layer mask for obstacles

    public Vector3 startPosition;

    private void Awake() {
        physics = GetComponent<GhostPhysics>();
        startPosition = transform.position;//always start at the same location
    }

    private void Start() {
        CreateBehaviors();
        State = GhostState.Home; // Set initial State to Home
    }


    private void Update() {
        if(behavior.IsDone()) {
            behavior.Stop();
            State = GetNextState(State);
            behavior.Init();
        }
        behavior.Tick(Time.deltaTime);
    }

    public void Init() {
        State = GhostState.Home; // Set initial State to Home
    }


    public GhostState _state = GhostState.Home;
    public GhostState State
    {
        get {
            return _state;
        }
        set {
            _state = value;
            behavior.Stop();
            behavior = GetBehaviorFromState(_state);
            SetAnimationForState(_state);
            behavior.Init();
        }
    }

    public GhostState GetNextState(GhostState currentState) {
        GhostState tmp = GhostState.Home;
        switch(currentState) {
            case GhostState.Chasing:
                tmp = GhostState.Scatter;
                break;
            case GhostState.Scatter:
                tmp =  GhostState.Chasing;
                break;
            case GhostState.Frightened:
                tmp = GhostState.Home;
                break;
            case GhostState.Home:
                tmp = GhostState.Scatter;
                break;
            default:
                tmp = GhostState.Chasing;
                break;
        }
        Debug.Log(name + ": State: " + currentState + " -> " + tmp);
        return tmp;
    }

    public GhostCommand GetBehaviorFromState(GhostState state) {
        GhostCommand behavior = null;
        switch(state) {
            case GhostState.Chasing:
                behavior = chaser;
                break;
            case GhostState.Scatter:
                behavior = scatter;
                break;
            case GhostState.Frightened:
                behavior = frightened;
                break;
            case GhostState.Home:
                behavior = home;
                break;
            default:
                behavior = home;
                break;
        }
        return behavior;

    }

    public void StopAllBehaviors() {
        if(chaser != null)
            chaser.Stop();
        if(scatter != null)
            scatter.Stop();
        if(frightened != null)
            frightened.Stop();
        if(home != null)
            home.Stop();

    }

    void CreateBehaviors() {
        chaser = new CmdChaser(this, chaseDuration);
        scatter = new CmdScatter(this, scatterDuration);
        frightened = new CmdFrightened(this, frightenedDuration);
        home = new CmdHome(this, homeDuration);


    }

    public void ResetState() {
        gameObject.SetActive(true);
        transform.position = startPosition;
        StopAllBehaviors();
        State = GhostState.Home; // Set initial State to Scatter
    }


    public Vector3 belowGatePos = new Vector3(-0.5f, 3, 0);
    public Vector3 aboveGatePos = new Vector3(-0.5f, 4, 0);
    public void LeaveHome() {
        if(!name.Contains("Blinky")) { //blinky starts outside!!
            StartCoroutine(MoveToPosition(belowGatePos, 0.5f, 0));
            StartCoroutine(MoveToPosition(aboveGatePos, 0.5f, 0.5f));
        }
    }

    public IEnumerator MoveToPosition(Vector3 targetPos1, float duration, float waitBeforeStart) {
        yield return new WaitForSeconds(waitBeforeStart);
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        while(elapsedTime < duration) {
            transform.position = Vector3.Lerp(startPos, targetPos1, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos1;
    }

    public void GoHome() {
        transform.position = startPosition;

    }

    void SetAnimationForState(GhostState state) {
        switch(state) {
            case GhostState.Chasing:
            case GhostState.Scatter:
            case GhostState.Home:
                SetNormalAppearance();
                // Set animation for chasing state
                break;
            case GhostState.Frightened:
                SetFrightenedAppearance();
                break;
            default:
                SetNormalAppearance();
                break;
        }
    }

    public GameObject Body;
    public GameObject Eyes;
    public GameObject BlueWhite;
    void SetNormalAppearance() {
        Body.SetActive(true);
        Eyes.SetActive(true);
        BlueWhite.SetActive(false);
    }

    void SetFrightenedAppearance() {
        Body.SetActive(false);
        Eyes.SetActive(true);
        BlueWhite.SetActive(true);
    }

    public void SetFrightenedEatenAppearance() {
        Body.SetActive(false);
        Eyes.SetActive(true);
        BlueWhite.SetActive(false);
    }

}
