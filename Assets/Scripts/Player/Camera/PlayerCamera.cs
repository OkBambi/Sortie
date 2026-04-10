using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [Header("Dynamic Aim Offset")]
    [Tooltip("How much the camera moves towards the cursor (0 = stays on player, 1 = moves completely to cursor)")]
    [SerializeField] private float lookAheadAmount = 0.25f;
    [Tooltip("Maximum distance the camera can drift from the player")]
    [SerializeField] private float maxLookAheadDistance = 5f;
    [Tooltip("How fast the camera smooths to the new position")]
    [SerializeField] private float smoothSpeed = 10f;

    //[SerializeField] private float sensitivity = 0.1f;
    private Vector3 _eulerAngles;
    private Vector3 _currentOffset;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }

    public void UpdateRotation(CameraInput input)
    {
        //_eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        //transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target, Ray aimRay, float deltaTime)
    {
        Vector3 targetAimPosition = target.position;

        Plane groundPlane = new Plane(Vector3.up, target.position);
        if (groundPlane.Raycast(aimRay, out float distance))
        {
            targetAimPosition = aimRay.GetPoint(distance);
        }

        Vector3 desiredOffset = (targetAimPosition - target.position) * lookAheadAmount;

        if (desiredOffset.magnitude > maxLookAheadDistance)
        {
            desiredOffset = desiredOffset.normalized * maxLookAheadDistance;
        }

        _currentOffset = Vector3.Lerp(_currentOffset, desiredOffset, deltaTime * smoothSpeed);

        transform.position = target.position + _currentOffset;
    }
}