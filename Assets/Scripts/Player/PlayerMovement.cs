using UnityEngine;
using KinematicCharacterController;
using System;

[Serializable]
public class MovementStats
{
    [Header("Mecha Ground Movement")]
    public float BaseSpeed = 18f;
    public float SprintSpeed = 35f;
    [Tooltip("How fast thrusters get you to top speed/change direction")]
    public float GroundAcceleration = 8f;
    [Tooltip("How much you slide when letting go of the controls (Lower = more ice-skating)")]
    public float GroundFriction = 3f;

    [Header("Mecha Aerial Movement")]
    public float AirSpeed = 25f;
    public float AirAcceleration = 5f;
    public float JumpSpeed = 20f;
    public float CoyoteTime = 0.2f;
    public float JumpSustainGravity = 0.4f;
    public float Gravity = -90f;

    [Header("Quick Boost (Dash)")]
    public float DashSpeed = 50f;
    [Tooltip("How long the pure dash momentum lasts before normal physics take over")]
    public float DashDuration = 0.15f;
    public float DashCooldown = 0.25f;
    public float DashRecoveryTime = 1.5f;
    public int MaxDashes = 2;

    [Header("Energy/Boost Capacity")]
    public float MaxBoost = 100f;
    public float BoostGain = 15f;
    public float SprintBoostLoss = 10f;
    public float JumpBoostLoss = 20f;
    public float DashBoostLoss = 25f;
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

public class PlayerMovement : MonoBehaviour, ICharacterController, IResourceProvider
{
    [Header("Core")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform playerLegs;
    [SerializeField] private float legRotationSpeed = 12f;

    [Header("Data")]
    public MovementStats Stats = new MovementStats();

    // State accessible by other systems (UI, Visuals)
    public float CurrentBoost { get; private set; }
    public int CurrentDashes { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsDashing { get { return _dashTimer > 0f; } }

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
    private float _dashTimer;
    private Vector3 _dashDirection;

    private Vector3 startPos;

    public void Initialize()
    {
        startPos = this.transform.position;
        _lastState = _state;
        CurrentDashes = Stats.MaxDashes;
        CurrentBoost = Stats.MaxBoost;
        _currentDashRecovery = Stats.DashRecoveryTime;
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
        _requestedJump = _requestedJump || (input.Jump && CurrentBoost >= Stats.JumpBoostLoss);
        if (_requestedJump && !wasRequestingJump) _timeSinceJumpRequest = 0f;

        _requestedSustainedJump = input.JumpSustain;
        _requestedDash = _requestedDash || input.Dash;

        // Only allow sprint if we have energy and are actually trying to move
        IsSprinting = input.Sprint && CurrentBoost > 0 && _requestedMovement.sqrMagnitude > 0.1f;
    }

    public void UpdateBody(float deltaTime)
    {
        // Dash Recovery
        if (CurrentDashes < Stats.MaxDashes)
        {
            _currentDashRecovery -= deltaTime;
            if (_currentDashRecovery <= 0f)
            {
                CurrentDashes++;
                _currentDashRecovery = Stats.DashRecoveryTime;
            }
        }
        else
        {
            _currentDashRecovery = Stats.DashRecoveryTime;
        }

        if (_currentDashCooldown > 0f) _currentDashCooldown -= deltaTime;
        if (_dashTimer > 0f) _dashTimer -= deltaTime;

        // Boost Drain/Gain
        if (IsSprinting || IsDashing)
            CurrentBoost = Mathf.Clamp(CurrentBoost - (Stats.SprintBoostLoss * deltaTime), 0, Stats.MaxBoost);
        else
            CurrentBoost = Mathf.Clamp(CurrentBoost + (Stats.BoostGain * deltaTime), 0, Stats.MaxBoost);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;

        if (IsDashing)
        {
            Vector3 dashVel = _dashDirection * Stats.DashSpeed;

            if (!motor.GroundingStatus.IsStableOnGround)
                currentVelocity += motor.CharacterUp * Stats.Gravity * deltaTime;

            currentVelocity = new Vector3(dashVel.x, currentVelocity.y, dashVel.z);
        }
        else
        {
            // muv luv ground
            if (motor.GroundingStatus.IsStableOnGround)
            {
                _state.Grounded = true;
                _timeSinceUngrounded = 0f;
                _ungroundedDueToJump = false;

                var groundedMovement = motor.GetDirectionTangentToSurface(
                    direction: _requestedMovement,
                    surfaceNormal: motor.GroundingStatus.GroundNormal
                ) * _requestedMovement.magnitude;

                var targetVelocity = groundedMovement * (IsSprinting ? Stats.SprintSpeed : Stats.BaseSpeed);

                float responseSpeed = _requestedMovement.sqrMagnitude > 0f ? Stats.GroundAcceleration : Stats.GroundFriction;

                var moveVelocity = Vector3.Lerp(
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-responseSpeed * deltaTime)
                );

                _state.Acceleration = (moveVelocity - currentVelocity) / deltaTime;
                currentVelocity = moveVelocity;
            }
            // gundam aerial
            else
            {
                _timeSinceUngrounded += deltaTime;

                if (_requestedMovement.sqrMagnitude > 0f)
                {
                    var planarMovement = Vector3.ProjectOnPlane(vector: _requestedMovement, planeNormal: motor.CharacterUp) * _requestedMovement.magnitude;
                    var currentPlanarVelocity = Vector3.ProjectOnPlane(vector: currentVelocity, planeNormal: motor.CharacterUp);

                    var movementForce = planarMovement * Stats.AirAcceleration * deltaTime;
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, IsSprinting ? Stats.SprintSpeed : Stats.AirSpeed);
                    currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
                }

                // Gravity logic
                var effectGravity = Stats.Gravity;
                if (_requestedSustainedJump && Vector3.Dot(currentVelocity, motor.CharacterUp) > 0f)
                    effectGravity *= Stats.JumpSustainGravity;

                currentVelocity += motor.CharacterUp * effectGravity * deltaTime;
            }
        }

        // jump
        if (_requestedJump)
        {
            var canCoyoteJump = _timeSinceUngrounded < Stats.CoyoteTime && !_ungroundedDueToJump;

            if ((motor.GroundingStatus.IsStableOnGround || canCoyoteJump) && CurrentBoost >= Stats.JumpBoostLoss)
            {
                CurrentBoost -= Stats.JumpBoostLoss;
                _requestedJump = false;
                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;
                _state.Grounded = false;

                _dashTimer = 0f;

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

        // dash
        if (_requestedDash)
        {
            var canDash = (_currentDashCooldown <= 0f) && (CurrentDashes > 0) && CurrentBoost >= Stats.DashBoostLoss;
            if (canDash)
            {
                _currentDashCooldown = Stats.DashCooldown;
                CurrentDashes--;
                CurrentBoost -= Stats.DashBoostLoss;
                _requestedDash = false;

                _dashTimer = Stats.DashDuration;

                // Dash in input direction. If no input, dash in the direction the torso is facing
                _dashDirection = _requestedMovement.sqrMagnitude == 0f ?
                    Vector3.ProjectOnPlane(root.forward, motor.CharacterUp).normalized :
                    _requestedMovement.normalized;
            }
            else
            {
                _requestedDash = false;
            }
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // Torso aims towards mouse (requested rotation)
        var forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward, motor.CharacterUp);
        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);

        // Legs aim towards actual momentum/velocity to emphasize drifting and sliding
        Vector3 flatVelocity = new Vector3(motor.BaseVelocity.x, 0f, motor.BaseVelocity.z);
        if (flatVelocity.sqrMagnitude > 1f) // Only rotate legs if we are actually moving fast enough
        {
            Quaternion targetLegRotation = Quaternion.LookRotation(flatVelocity.normalized, motor.CharacterUp);
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

    public float GetResourcePercentage(ResourceType type)
    {
        if (type == ResourceType.Boost) return CurrentBoost / Stats.MaxBoost;
        else return 0.0f;
    }

    int IResourceProvider.GetResourceCurrent(ResourceType type)
    {
        throw new NotImplementedException();
    }

    int IResourceProvider.GetResourceMax(ResourceType type)
    {
        throw new NotImplementedException();
    }
}