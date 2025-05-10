using UnityEngine;

public class BallController : MonoBehaviour
{

    public float speed = 5f;
    private Rigidbody2D rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        LaunchBall();
    }

    public void LaunchBall() {
        // Calculate a random direction
        speed = 5f;
        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-1f, 1f);
        Vector2 direction = new Vector2(randomX, randomY).normalized;
        // Apply force to the ball
        rb.velocity = direction * speed;
    }


    private void OnCollisionEnter2D(Collision2D collision) {
        Vector2 vel = rb.velocity;
        speed += 0.5f;
        speed = Mathf.Clamp(speed, 5f, 20f);
        if(collision.gameObject.name.Contains("PlayerBat")) {
            Debug.Log("Hit PlayerBat");
            vel.y += Random.Range(-0.5f, 0.5f);
            //rb.velocity = vel.normalized * speed;
        } else if(collision.gameObject.name.Contains("Left")) {
            Debug.Log("Hit Left");
            ScoreMgr.instance.IncScore(true);
        } else if(collision.gameObject.name.Contains("Right")) {
            Debug.Log("Hit Right");
            ScoreMgr.instance.IncScore(false);

        }
        rb.velocity = vel.normalized * speed;
    }

}
