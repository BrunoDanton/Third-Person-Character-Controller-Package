using UnityEngine;
using UnityEngine.InputSystem;

namespace BrunoDanton.Controllers
{
    /// <summary>
    /// Third-person orbital camera controller.
    /// - Rotates around the player based on mouse/controller input.
    /// - Prevents clipping into walls using a linecast.
    /// Requires:
    /// - A PlayerInput with an action named "Look" (Vector2).
    /// - A target Transform (player).
    /// </summary>

    public class CameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private PlayerInput playerInput;

        [Header("Settings")]
        [SerializeField] private LayerMask collisionLayers;
        [Range(0f, 500f)] [SerializeField] private float sensibility = 200f;
        [Range(0f, 300f)] [SerializeField] private float yOffset = 1.5f;
        [SerializeField] private float distance = 5f;
        [SerializeField] private float verticalRotationLimit = 60f;

        private InputAction _lookAction;
        private float _rotX;
        private float _rotY;
        private Vector3 _offset;

        private void Start()
        {
            if (player == null)
            {
                Debug.LogError("[CameraController] Missing player reference. Please assign a target Transform.");
                enabled = false;
                return;
            }

            if (playerInput == null)
            {
                Debug.LogError("[CameraController] Missing PlayerInput reference. Please assign a PlayerInput component.");
                enabled = false;
                return;
            }

            _lookAction = playerInput.actions["Look"];
            if (_lookAction == null)
            {
                Debug.LogError("[CameraController] 'Look' action not found in PlayerInput. Please configure it.");
                enabled = false;
                return;
            }

            _offset = new Vector3(0, 0, -distance);
        }

        private void Update()
        {
            Vector2 lookInput = _lookAction.ReadValue<Vector2>();

            // Horizontal (Y rotation)
            _rotY += lookInput.x * sensibility / 500f;

            // Vertical (X rotation)
            _rotX -= lookInput.y * sensibility / 500f;
            _rotX = Mathf.Clamp(_rotX, -verticalRotationLimit, verticalRotationLimit);

            // Apply rotation
            transform.rotation = Quaternion.Euler(_rotX, _rotY, 0);
        }

        private void LateUpdate()
        {
            // Target position (with vertical offset)
            Vector3 targetPosition = player.position + new Vector3(0, yOffset, 0);

            // Camera position based on rotation and offset
            Vector3 rotatedOffset = transform.rotation * _offset;
            Vector3 idealPosition = targetPosition + rotatedOffset;

            // Prevent clipping through walls
            if (Physics.Linecast(targetPosition, idealPosition, out RaycastHit hitInfo, collisionLayers))
            {
                transform.position = hitInfo.point;
            }
            else
            {
                transform.position = idealPosition;
            }
        }

        /// <summary>
        /// Dynamically updates the camera distance (e.g. for zoom).
        /// </summary>
        public void SetDistance(float newDistance)
        {
            distance = Mathf.Max(0.1f, newDistance);
            _offset = new Vector3(0, 0, -distance);
        }
    }
}
