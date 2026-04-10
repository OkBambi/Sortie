using UnityEngine;

public class FaceCursor : MonoBehaviour
{
    [SerializeField] float offsetAngle = 90f;

    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 lookDir = transform.position - Camera.main.transform.position;

            Quaternion targetRotation = Quaternion.LookRotation(lookDir);

            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(mouseRay, out float hitDistance))
            {
                Vector3 mouseWorldPos = mouseRay.GetPoint(hitDistance);
                Vector3 dirToMouse = mouseWorldPos - transform.position;

                if (dirToMouse != Vector3.zero)
                {
                    targetRotation = Quaternion.LookRotation(lookDir, dirToMouse);
                }
            }
            transform.rotation = targetRotation * Quaternion.Euler(0, 0, offsetAngle);
        }
    }
}