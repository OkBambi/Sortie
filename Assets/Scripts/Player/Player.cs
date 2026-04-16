using UnityEngine;

public class Player : MonoBehaviour, ITarget, IDamage, IHealth
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private CombatSystem combatSystem;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [Space]

    private PlayerActionInputs _inputActions;
    private Camera _mainCamera;
    private Ray _currentAimRay;

    public Transform Transform => playerMovement.transform;

    public Vector3 Velocity => throw new System.NotImplementedException();

    public bool IsValid => throw new System.NotImplementedException();

    public float CurrentHealth => throw new System.NotImplementedException();

    public float MaxHealth => throw new System.NotImplementedException();

    void Start()
    {
        _inputActions = new PlayerActionInputs();
        _inputActions.Enable();

        _mainCamera = Camera.main;

        playerCamera = GameObject.FindAnyObjectByType<PlayerCamera>();
        cameraSpring = GameObject.FindAnyObjectByType<CameraSpring>();
        cameraLean = GameObject.FindAnyObjectByType<CameraLean>();

        playerMovement.Initialize();
        playerCamera.Initialize(playerMovement.GetCameraTarget());
        cameraSpring.Initialize();
        cameraLean.Initialize();
    }

    void OnDestroy()
    {
        _inputActions.Dispose();
    }

    void Update()
    {
        var input = _inputActions.Player;
        var deltaTime = Time.deltaTime;

        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Dash = input.Dash.WasPressedThisFrame(),
            Sprint = input.Sprint.IsPressed()
        };
        playerMovement.UpdateInput(characterInput);
        playerMovement.UpdateBody(deltaTime);

        Ray screenRay = _mainCamera != null
            ? _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition)
            : new Ray(transform.position, transform.forward);

        _currentAimRay = screenRay;

        var combatInput = new CombatInput
        {
            ShootPrimary = input.AttackPrimary.IsPressed(),
            ShootSecondary = input.AttackSecondary.IsPressed(),
            ShootLeftShoulder = input.LeftShoulder.IsPressed(),
            ShootRightShoulder = input.RightShoulder.IsPressed(),
            Reload = input.Reload.WasPressedThisFrame(),
            AimRay = screenRay
        };

        combatSystem.ProcessCombat(combatInput);
    }

    void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        var cameraTarget = playerMovement.GetCameraTarget();
        var state = playerMovement.GetState();

        playerCamera.UpdatePosition(cameraTarget, _currentAimRay, deltaTime);
        cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
        cameraLean.UpdateLean(deltaTime, state.Acceleration, cameraTarget.up);
    }

    public void TakeDamage(float damage, Vector3 hitPos)
    {
        //egh
    }

    public void ChangeHealth(float amount)
    {
        throw new System.NotImplementedException();
    }
}