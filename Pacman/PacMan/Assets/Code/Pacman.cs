using UnityEngine;

public enum PacmanState {
    Idle,
    Moving,
    Dead,
    Chasing,
}

public class Pacman : MonoBehaviour {
    public SpriteAnimator deathSequence;
    public SpriteRenderer spriteRenderer;
    public CircleCollider2D circleCollider;
    public Movement movement;

    private Vector3 rotationEulerAngle = Vector3.zero;
    public Vector3 startPosition = Vector3.zero;

    public PacmanState state = PacmanState.Idle;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        movement = GetComponent<Movement>();
        state = PacmanState.Idle;
        startPosition = transform.position;
    }

    private void Update() {
        HandlePacmanInput();
    }

    void HandlePacmanInput() {
        // Handle input for direction change
        state = PacmanState.Moving;
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
            movement.SetDirection(new Vector2Int(0, 1)); // Up
        } else if(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
            movement.SetDirection(new Vector2Int(0, -1)); // Down
        } else if(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
            movement.SetDirection(new Vector2Int(-1, 0)); // Left
        } else if(Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
            movement.SetDirection(new Vector2Int(1, 0)); // Right
        } else if(Input.GetKeyDown(KeyCode.Space)) {
            // Handle space key for some action
            movement.currentDirection = Vector2Int.zero; // Stop moving
            state = PacmanState.Idle;
        }
        float angle = Mathf.Atan2(movement.currentDirection.y, movement.currentDirection.x) * Mathf.Rad2Deg;
        rotationEulerAngle.z = angle;
        transform.localEulerAngles = rotationEulerAngle; // Rotate the object to face the direction of movement

    }

    public void ResetState() {
        transform.position = startPosition;
        spriteRenderer.enabled = true;
        circleCollider.enabled = true;
        //deathSequence.enabled = false;
        movement.ResetState();
        gameObject.SetActive(true);
        state = PacmanState.Idle;
    }

    public void DeathSequence() {
        spriteRenderer.enabled = false;
        circleCollider.enabled = false;
        movement.enabled = false;
        deathSequence.enabled = true;
        deathSequence.Restart();
        state = PacmanState.Dead;
    }

}