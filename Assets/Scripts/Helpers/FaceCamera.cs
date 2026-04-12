using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 lookDir = transform.position - Camera.main.transform.position;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
