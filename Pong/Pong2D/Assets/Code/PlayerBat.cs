
using UnityEngine;

public class PlayerBat : MonoBehaviour
{
    public float speed = 10f;
    void Update()
    {
        float move = Input.GetAxis("Vertical");
        transform.Translate(Vector2.up * move * speed * Time.deltaTime);
        ClampPosition();
    }

    void ClampPosition() {
        float y = Mathf.Clamp(transform.position.y, -4.5f, 4.5f);
        transform.position = new Vector2(transform.position.x, y);
    }
}
