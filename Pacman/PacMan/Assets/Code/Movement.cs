
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour {
    public float speed = 10f;
    public float speedMultiplier = 1f;
    public Vector2Int currentDirection;
    public Vector2Int startDirection = Vector2Int.right; // Default starting direction
    public Vector2Int nextDirection;
    public Vector3 rotationEulerAngle;
    public Vector3 startingPosition;

    public LayerMask obsLayerMask; // Layer mask for collision detection
    public Rigidbody2D rb; // Reference to the Rigidbody2D component
    private void Awake() {
        rb = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component
        startingPosition = transform.position; // Store the starting position
    }

    void Start() {
        ResetState();
    }

    public void ResetState() {
        startingPosition = transform.position; // Store the starting position
        currentDirection = startDirection;
        nextDirection = Vector2Int.zero; // Reset next direction
        rotationEulerAngle = new Vector3(0, 0, 0);
        this.enabled = true;
    }

    void Update() {
        if(nextDirection != Vector2Int.zero) {
            SetDirection(nextDirection);
        }
    }
    public Vector3 tmp = Vector3.zero;
    public Vector3 dir3 = Vector3.zero;
    void FixedUpdate() {

        dir3.x = currentDirection.x;
        dir3.y = currentDirection.y;
        rb.MovePosition(transform.position + dir3 * speed * Time.fixedDeltaTime);
    }

    public void SetDirection(Vector2Int direction, bool forced = false) {
        if(CanMove(direction) || forced) {
            currentDirection = direction; // Set the new direction if it's not blocked
            nextDirection = Vector2Int.zero; // Reset next direction
        } else {
            nextDirection = direction; // Set the next direction if it's not blocked or forced                   }
        }
    }

    public void CenterOnPath() {
        if(currentDirection.x == 0)
            tmp.x = Mathf.Round(transform.position.x);
        if(currentDirection.y == 0)
            tmp.y = Mathf.Round(transform.position.y);
        rb.MovePosition(tmp); // Round the position to avoid floating point errors

    }

    Vector2 size = new Vector2(0.8f, 0.8f); // Size of the box cast

    public bool CanMove(Vector2Int direction) {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, size, 0, direction, 1.5f, obsLayerMask);
        return hit.collider == null;
    }


}
