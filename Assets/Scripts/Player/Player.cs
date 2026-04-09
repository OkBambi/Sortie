using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private CombatSystem combatSystem;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [Space]

    private PlayerActionInputs _inputActions;
    private Camera _mainCamera; // Cached reference for aiming

    void Start()
    {
        _inputActions = new PlayerActionInputs();
        _inputActions.Enable();

        _mainCamera = Camera.main;

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

        // Camera Update
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);

        // Movement Update
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

        //ill make proper inputs SOON :TM:
        int requestedSlot = -1;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) requestedSlot = 0;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) requestedSlot = 1;

        Ray screenRay = _mainCamera != null
            ? _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition)
            : new Ray(transform.position, transform.forward);

        var combatInput = new CombatInput
        {
            Shoot = input.Attack.IsPressed(),
            Reload = input.Reload.WasPressedThisFrame(),
            NumberKeyMap = requestedSlot,
            AimRay = screenRay
        };

        combatSystem.ProcessCombat(combatInput);
    }

    void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        var cameraTarget = playerMovement.GetCameraTarget();
        var state = playerMovement.GetState();

        playerCamera.UpdatePosition(cameraTarget);
        cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
        cameraLean.UpdateLean(deltaTime, state.Acceleration, cameraTarget.up);
    }
}