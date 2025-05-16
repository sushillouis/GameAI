
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public GameObject TeleportTarget;

    Vector3 pacManPos = Vector3.zero;
    private void OnTriggerEnter2D(Collider2D other) {
        Debug.Log("Trigger Teleport: " + name);
        pacManPos = TeleportTarget.transform.position;
        pacManPos.z = 0; //Don't change draw order
        other.transform.position = pacManPos;
    }

}
