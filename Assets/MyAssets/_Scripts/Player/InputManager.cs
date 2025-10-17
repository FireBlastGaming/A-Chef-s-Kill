using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Reference to the PlayerInput component. We avoid naming this the same as the
    // type (PlayerInput) to prevent hiding the type and confusing editors.
    public static PlayerInput playerInput;

    public static Vector2 Movement;
    public static bool JumpWasPressed;
    public static bool JumpIsHeld;
    public static bool JumpWasReleased;
    public static bool SprintIsHeld;
    public static bool DashWasPressed;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _dashAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        _moveAction  = playerInput.actions["Move"];
        _jumpAction  = playerInput.actions["Jump"];
        _sprintAction = playerInput.actions["Sprint"];

        _dashAction = playerInput.actions["Dash"];
    }

    private void Update()
    {
        Movement = _moveAction.ReadValue<Vector2>();

        JumpWasPressed = _jumpAction.WasPressedThisFrame();
        JumpIsHeld = _jumpAction.IsPressed();
        JumpWasReleased = _jumpAction.WasReleasedThisFrame();

        SprintIsHeld = _sprintAction.IsPressed();

        DashWasPressed = _dashAction.WasPressedThisFrame();
    }
}
