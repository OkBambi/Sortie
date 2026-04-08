using UnityEngine;
using KinematicCharacterController;
using System;

[Serializable]
public struct MovementStats
{
    public float WalkSpeed;
    public float WalkResponse;
    public float SprintMultiplier;

    public float AirSpeed;
    public float AirAcceleration;
    public float JumpSpeed;
    public float CoyoteTime;
    public float JumpSustainGravity;
    public float Gravity;

    public float DashSpeed;
    public float DashCooldown;
    public float DashRecoveryTime;
    public int MaxDashes;

    public float MaxBoost;
    public float BoostGain;
    public float SprintBoostLoss;
    public float JumpBoostLoss;
    public float DashBoostLoss;
}

public struct CharacterState
{
    public bool Grounded;
    public Vector3 Velocity;
    public Vector3 Acceleration;
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public bool Dash;
    public bool Sprint;
}

public class PlayerMovement : MonoBehaviour, ICharacterController
{
    [Header("Core")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform playerLegs;
    [SerializeField] private float legRotationSpeed = 8f;

    [Header("Data")]
    public MovementStats Stats; // Clean, centralized data container

    // State accessible by other systems (UI, Visuals)
    public float CurrentBoost { get; private set; }
    public int CurrentDashes { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsDashing { get; private set; }

    private CharacterState _state;
    private CharacterState _lastState;

    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedDash;

    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;
    private float _currentDashRecovery;
    private float _currentDashCooldown;

    private Vector3 startPos;

    public void Initialize()
    {
        startPos = this.transform.position;
        _lastState = _state;
        CurrentDashes = Stats.MaxDashes;
        CurrentBoost = Stats.MaxBoost;
        motor.CharacterController = this;
    }

    public void ResetPos()
    {
        motor.SetPosition(startPos);
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;

        Transform camTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 cameraForward;
        Vector3 cameraRight;

        if (camTransform != null)
        {
            cameraForward = camTransform.forward;
            cameraRight = camTransform.right;
        }
        else
        {
            cameraForward = input.Rotation * Vector3.forward;
            cameraRight = input.Rotation * Vector3.right;
        }

        cameraForward.y = 0f;
        cameraForward.Normalize();
        cameraRight.y = 0f;
        cameraRight.Normalize();

        _requestedMovement = (cameraRight * input.Move.x) + (cameraForward * input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);

        var wasRequestingJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump && CurrentBoost > Stats.JumpBoostLoss;
        if (_requestedJump && !wasRequestingJump) _timeSinceJumpRequest = 0f;

        _requestedSustainedJump = input.JumpSustain;
        _requestedDash = _requestedDash || input.Dash;
        IsSprinting = input.Sprint && CurrentBoost > 0;
    }

    public void UpdateBody(float deltaTime)
    {
        // Dash Recovery
        if (_currentDashRecovery > 0f && CurrentDashes < Stats.MaxDashes)
            _currentDashRecovery -= deltaTime;
        else if (_currentDashRecovery <= 0f && CurrentDashes < Stats.MaxDashes)
        {
            CurrentDashes++;
            if (CurrentDashes < Stats.MaxDashes) _currentDashRecovery = Stats.DashRecoveryTime;
        }

        // Dash Between Cooldown
        if (_currentDashCooldown > 0f) _currentDashCooldown -= deltaTime;
        if (_currentDashCooldown < 0f) _currentDashCooldown = 0f;

        // Boost Drain/Gain
        if (IsSprinting)
            CurrentBoost = Mathf.Clamp(CurrentBoost - (Stats.SprintBoostLoss * deltaTime), 0, Stats.MaxBoost);
        else
            CurrentBoost = Mathf.Clamp(CurrentBoost + (Stats.BoostGain * deltaTime), 0, Stats.MaxBoost);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;
        IsDashing = false;

        // Ground Movement
        if (motor.GroundingStatus.IsStableOnGround)
        {
            _state.Grounded = true;
            _timeSinceUngrounded = 0f;
            _ungroundedDueToJump = false;

            var groundedMovement = motor.GetDirectionTangentToSurface(
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            var targetVelocity = groundedMovement * Stats.WalkSpeed;
            targetVelocity *= IsSprinting ? Stats.SprintMultiplier : 1f;

            var moveVelocity = Vector3.Lerp(
                a: currentVelocity,
                b: targetVelocity,
                t: 1f - Mathf.Exp(-Stats.WalkResponse * deltaTime)
            );

            _state.Acceleration = (moveVelocity - currentVelocity) / deltaTime;
            currentVelocity = moveVelocity;
        }
        // Aerial Movement
        else
        {
            _timeSinceUngrounded += deltaTime;

            if (_requestedMovement.sqrMagnitude > 0f)
            {
                var planarMovement = Vector3.ProjectOnPlane(vector: _requestedMovement, planeNormal: motor.CharacterUp) * _requestedMovement.magnitude;
                var currentPlanarVelocity = Vector3.ProjectOnPlane(vector: currentVelocity, planeNormal: motor.CharacterUp);

                var movementForce = planarMovement * Stats.AirAcceleration * deltaTime;
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                targetPlanarVelocity *= IsSprinting ? Stats.SprintMultiplier : 1f;
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, Stats.AirSpeed);

                currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
            }

            var effectGravity = Stats.Gravity;
            if (_requestedSustainedJump && Vector3.Dot(currentVelocity, motor.CharacterUp) > 0f)
                effectGravity *= Stats.JumpSustainGravity;

            currentVelocity += motor.CharacterUp * effectGravity * deltaTime;
        }

        // Jumping
        if (_requestedJump)
        {
            var canCoyoteJump = _timeSinceUngrounded < Stats.CoyoteTime && !_ungroundedDueToJump && CurrentBoost >= Stats.JumpBoostLoss;

            if (motor.GroundingStatus.IsStableOnGround || canCoyoteJump)
            {
                CurrentBoost -= Stats.JumpBoostLoss;
                _requestedJump = false;
                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;
                _state.Grounded = false;

                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, Stats.JumpSpeed);
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
            else
            {
                _timeSinceJumpRequest += deltaTime;
                _requestedJump = _timeSinceJumpRequest < Stats.CoyoteTime;
            }
        }

        // Dashing
        if (_requestedDash)
        {
            var canDash = (_currentDashCooldown == 0f) && (CurrentDashes > 0) && CurrentBoost >= Stats.DashBoostLoss;
            if (canDash)
            {
                _currentDashCooldown = Stats.DashCooldown;
                CurrentDashes--;
                CurrentBoost -= Stats.DashBoostLoss;
                _requestedDash = false;
                IsDashing = true;

                var dashDirection = _requestedMovement.sqrMagnitude == 0f ?
                    Vector3.ProjectOnPlane(root.forward, motor.CharacterUp).normalized :
                    _requestedMovement.normalized;

                var dashVelocity = dashDirection * Stats.DashSpeed;
                currentVelocity = new Vector3(dashVelocity.x, currentVelocity.y, dashVelocity.z);
            }
            else
            {
                _requestedDash = false;
            }
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward, motor.CharacterUp);
        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);

        if (_requestedMovement.sqrMagnitude > 0.01f)
        {
            Quaternion targetLegRotation = Quaternion.LookRotation(_requestedMovement, motor.CharacterUp);
            playerLegs.rotation = Quaternion.Lerp(playerLegs.rotation, targetLegRotation, legRotationSpeed * deltaTime);
        }
    }

    // Required Interface Methods
    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

    public Transform GetCameraTarget() => cameraTarget;
    public CharacterState GetState() => _state;
}