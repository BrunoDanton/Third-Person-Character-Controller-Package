using UnityEngine;
using UnityEngine.InputSystem;

namespace BrunoDanton.Controllers
{
    /// <summary>
    /// Centralizes all input actions for the player.
    /// Requires a PlayerInput component configured with the following actions:
    /// - Move (Vector2)
    /// - Jump (Button)
    /// - Focus (Button)
    /// - Run (Button / Trigger)
    /// - Roll (Button)
    /// </summary>
    
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;

        public Vector2 Move { get; private set; }
        public bool Jump { get; private set; }
        public bool Focus { get; private set; }
        public bool Run { get; private set; }
        public bool Roll { get; private set; }

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _focusAction;
        private InputAction _runAction;
        private InputAction _rollAction;

        private void Awake()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    Debug.LogError("[InputManager] Missing PlayerInput component. Please add one to the GameObject.");
                    enabled = false;
                    return;
                }
            }

            // Bind input actions
            _moveAction = playerInput.actions["Move"];
            _jumpAction = playerInput.actions["Jump"];
            _focusAction = playerInput.actions["Focus"];
            _runAction = playerInput.actions["Run"];
            _rollAction = playerInput.actions["Roll"];

            // Subscribe to input events
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;

            _jumpAction.performed += OnJump;
            _jumpAction.canceled += OnJump;

            _focusAction.performed += OnFocus;
            _focusAction.canceled += OnFocus;

            _runAction.performed += OnRun;

            // Roll is a one-frame action (no canceled event)
            _rollAction.performed += OnRoll;
        }

        private void OnDestroy()
        {
            // Always unsubscribe to avoid memory leaks
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJump;
                _jumpAction.canceled -= OnJump;
            }

            if (_focusAction != null)
            {
                _focusAction.performed -= OnFocus;
                _focusAction.canceled -= OnFocus;
            }

            if (_runAction != null)
            {
                _runAction.performed -= OnRun;
            }

            if (_rollAction != null)
            {
                _rollAction.performed -= OnRoll;
            }
        }

        // === Input Callbacks ===
        private void OnMove(InputAction.CallbackContext ctx) => Move = ctx.ReadValue<Vector2>();
        private void OnJump(InputAction.CallbackContext ctx) => Jump = ctx.ReadValueAsButton();
        private void OnFocus(InputAction.CallbackContext ctx) => Focus = ctx.ReadValueAsButton();
        private void OnRun(InputAction.CallbackContext ctx) => Run = true;
        private void OnRoll(InputAction.CallbackContext ctx) => Roll = true;

        private void LateUpdate()
        {
            // Reset one-frame actions
            Run = false;
            Roll = false;
        }
    }
}
