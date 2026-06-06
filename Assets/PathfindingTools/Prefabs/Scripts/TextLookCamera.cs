using UnityEngine;

public class TextLookCamera : MonoBehaviour
{
    void Update()
    {
        var dir = Camera.main.transform.position - transform.position;
        dir.y = 0;

        transform.rotation = Quaternion.LookRotation(-dir);
    }
}
