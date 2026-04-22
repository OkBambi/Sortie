using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class CameraState
{
    public string StateName;
    public Vector3 LocalPositionOffset;
    public Vector3 LocalRotationOffset;
    [Tooltip("If assigned, the camera will ignore local offsets and move directly to this Transform's world position/rotation.")]
    public Transform TargetTransform;
    public float FieldOfView = 30f;
    public float TransitionSpeed = 5f;
}

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [Header("Hierarchy References")]
    [Tooltip("Drag the actual 'Main Camera' child object here")]
    [SerializeField] private Camera mainCameraLeaf;

    [Header("Dynamic Aim Offset")]
    [Tooltip("How much the camera moves towards the cursor (0 = stays on player, 1 = moves completely to cursor)")]
    [SerializeField] private float lookAheadAmount = 0.25f;
    [Tooltip("Maximum distance the camera can drift from the player")]
    [SerializeField] private float maxLookAheadDistance = 5f;
    [Tooltip("How fast the camera smooths to the new position")]
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Camera States")]
    [SerializeField] private List<CameraState> cameraStates = new List<CameraState>();

    private CameraState _currentState;
    private Vector3 _eulerAngles;
    private Vector3 _currentOffset;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;

        if (cameraStates.Count > 0)
        {
            _currentState = cameraStates[0];
            ApplyStateInstantly(_currentState);
        }
        else
        {
            Debug.LogWarning("PlayerCamera has no Camera States defined! Please add a Gameplay state in the inspector.");
        }
    }

    /// <summary>
    /// Call this to swing the camera to a menu view, cinematic, or back to gameplay.
    /// </summary>
    public void ChangeState(string stateName)
    {
        var foundState = cameraStates.Find(s => s.StateName == stateName);
        if (foundState != null)
        {
            _currentState = foundState;
        }
        else
        {
            Debug.LogError($"Camera State '{stateName}' not found!");
        }
    }

    /// <summary>
    /// Provides a stable rotation reference for PlayerMovement, ignoring current camera transitions.
    /// </summary>
    public Quaternion GetGameplayCameraWorldRotation()
    {
        if (cameraStates != null && cameraStates.Count > 0)
        {
            // We multiply the Rig Root's world rotation by the state's local offset
            // to get the true world direction the camera is facing (the "Green Arrows").
            return transform.rotation * Quaternion.Euler(cameraStates[0].LocalRotationOffset);
        }

        // Bulletproof fallbacks if the Inspector hasn't been fully set up yet
        if (mainCameraLeaf != null) return mainCameraLeaf.transform.rotation;
        if (Camera.main != null) return Camera.main.transform.rotation;

        return transform.rotation;
    }

    public void UpdateRotation(CameraInput input)
    {
        // Reserved for manual camera panning if needed later
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

        if (_currentState != null && mainCameraLeaf != null)
        {
            Transform camTransform = mainCameraLeaf.transform;

            float t = Time.unscaledDeltaTime * _currentState.TransitionSpeed;

            if (_currentState.TargetTransform != null)
            {
                camTransform.position = Vector3.Lerp(camTransform.position, _currentState.TargetTransform.position, t);
                camTransform.rotation = Quaternion.Slerp(camTransform.rotation, _currentState.TargetTransform.rotation, t);
            }
            else
            {
                camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, _currentState.LocalPositionOffset, t);
                camTransform.localRotation = Quaternion.Slerp(camTransform.localRotation, Quaternion.Euler(_currentState.LocalRotationOffset), t);
            }

            mainCameraLeaf.fieldOfView = Mathf.Lerp(mainCameraLeaf.fieldOfView, _currentState.FieldOfView, t);
        }
    }

    private void ApplyStateInstantly(CameraState state)
    {
        if (mainCameraLeaf != null)
        {
            if (state.TargetTransform != null)
            {
                mainCameraLeaf.transform.position = state.TargetTransform.position;
                mainCameraLeaf.transform.rotation = state.TargetTransform.rotation;
            }
            else
            {
                mainCameraLeaf.transform.localPosition = state.LocalPositionOffset;
                mainCameraLeaf.transform.localRotation = Quaternion.Euler(state.LocalRotationOffset);
            }
            mainCameraLeaf.fieldOfView = state.FieldOfView;
        }
    }
}