using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Reference to the PlayerInput component. We avoid naming this the same as the
    // type (PlayerInput) to prevent hiding the type and confusing editors.
    public static PlayerInput PlayerInput;

    public static Vector2 Movement;
    public static bool JumpWasPressed;
    public static bool JumpIsHeld;
    public static bool JumpWasReleased;
    public static bool SprintIsHeld;
    public static bool DashWasPressed;
    public static bool PrimaryAttackWasClicked;
    public static bool PauseWasPressed;
    public static bool InventoryWasPressed;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _dashAction;
    private InputAction _primaryAttackAction;
    private InputAction _playerPauseAction;
    private InputAction _uiPauseAction;
    private InputAction _playerInventoryAction;
    private InputAction _uiInventoryAction;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();

        _moveAction  = PlayerInput.actions["Move"];
        _jumpAction  = PlayerInput.actions["Jump"];
        _sprintAction = PlayerInput.actions["Sprint"];

        _dashAction = PlayerInput.actions["Dash"];

        _primaryAttackAction = PlayerInput.actions["Primary Attack"];

        _playerPauseAction = PlayerInput.actions.FindActionMap("Player", true).FindAction("Pause", true);
        _uiPauseAction = PlayerInput.actions.FindActionMap("UI", true).FindAction("Pause", true);
        _playerInventoryAction = PlayerInput.actions.FindActionMap("Player", true).FindAction("Inventory", true);
        _uiInventoryAction = PlayerInput.actions.FindActionMap("UI", true).FindAction("Inventory", true);
    }

    private void Update()
    {
        Movement = _moveAction.ReadValue<Vector2>();

        JumpWasPressed = _jumpAction.WasPressedThisFrame();
        JumpIsHeld = _jumpAction.IsPressed();
        JumpWasReleased = _jumpAction.WasReleasedThisFrame();

        SprintIsHeld = _sprintAction.IsPressed();

        DashWasPressed = _dashAction.WasPressedThisFrame();

        PrimaryAttackWasClicked = _primaryAttackAction.WasPressedThisFrame();

        PauseWasPressed = PlayerInput.currentActionMap != null && PlayerInput.currentActionMap.name == "UI"
            ? _uiPauseAction.WasPressedThisFrame()
            : _playerPauseAction.WasPressedThisFrame();

        InventoryWasPressed = PlayerInput.currentActionMap != null && PlayerInput.currentActionMap.name == "UI"
            ? _uiInventoryAction.WasPressedThisFrame()
            : _playerInventoryAction.WasPressedThisFrame();
    }
}
