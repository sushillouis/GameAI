using UnityEngine;

public class BatAI : MonoBehaviour
{
    public float speed = 10f;
    public Transform ball;
    public float yOffset = 0.0f;
    void Update() {
        if(ball != null) {
            Vector2 targetPosition = new Vector2(transform.position.x, ball.position.y + yOffset);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            ClampPosition();
        }
    }
    void ClampPosition() {
        float y = Mathf.Clamp(transform.position.y, -4.5f, 4.5f);
        transform.position = new Vector2(transform.position.x, y);
    }

}
