using UnityEngine;

namespace BrunoDanton.Controllers
{
    /// <summary>
    /// Player controller for third-person movement.
    /// Handles:
    /// - Movement (walk, run, roll, jump, fall).
    /// - Character rotation (camera-relative or focus-lock).
    /// - Animations via Animator parameters.
    /// - Audio feedback (footsteps, roll, jump, falling, landing).
    /// 
    /// Requirements:
    /// - CharacterController, Animator, AudioSource on the same GameObject.
    /// - InputManager component for player inputs.
    /// - Animation parameters: "IsGrounded", "Move", "Run", "Jump", "Roll", "MovingSpeed", "Falling", "PosX", "PosY".
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(InputManager))]
    public class PlayerController : MonoBehaviour
    {
        // === COMPONENTS ===
        private Transform _cameraTransform;
        private CharacterController _controller;
        private InputManager _inputManager;
        private AudioSource _audioSource;
        private Animator _animator;

        // === AUDIO ===
        [Header("Audio Clips")]
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private AudioClip[] rollClips;
        [SerializeField] private AudioClip[] jumpClips;
        [SerializeField] private AudioClip fallingClip;
        [SerializeField] private AudioClip groundImpactClip;
        [SerializeField] private AudioClip sweetGroundImpactClip;

        // === MOVEMENT SETTINGS ===
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 7f;             // base walking speed of the character
        [SerializeField] private float jumpHeight = 1f;            // jump height
        [SerializeField] private float jumpMultiplier = -3f;       // jump force multiplier (affects intensity)
        [SerializeField] private float rollDuration = 1.08f;       // duration of the roll action
        [SerializeField] private float rollSpeedMultiplier = 1.5f; // extra speed applied during a roll
        [SerializeField] private float smoothTime = 0.1f;          // smoothing time for acceleration/deceleration
        [Range(0f, 720f)]
        [SerializeField] private float rotationSpeed = 240f;       // rotation speed in degrees per second
        [SerializeField] private float runMultiplier = 1.5f;       // run speed multiplier

        // === PHYSICS SETTINGS ===
        [Header("Physics Settings")]
        [Range(-100f, 0f)]
        [SerializeField] private float gravity = -19.62f;          // gravity value (based on -9.81f * 2)
        [SerializeField] private float fallSoundDelay = 0.6f;      // delay before playing the falling sound
        [SerializeField] private float fallAnimDelay = 0.5f;       // delay before triggering the falling animation

        // === STATES ===
        private bool _isFocused;
        private bool _isRunning;
        private bool _isRolling;
        private bool _hasPlayedRollSound;
        private bool _hasPlayedFootstepSound;

        // === MOVEMENT VARIABLES ===
        private Vector3 _playerVelocity;
        private Vector3 _inputVector;
        private Vector3 _smoothedDirection;
        private Vector3 _currentVelocity;
        private Vector3 _rollDirection;

        // === CONTROL VARIABLES ===
        private float _currentSpeed;
        private float _rollSpeed;
        private float _timeInAir;
        private float _previousAirDuration;
        private float _rollTimer;

        // === UNITY METHODS ===
        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _inputManager = GetComponent<InputManager>();
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
            _cameraTransform = Camera.main != null ? Camera.main.transform : null;

            if (_cameraTransform == null)
            {
                Debug.LogWarning("[PlayerController] No main camera found. Some features may not work as expected.");
            }
        }

        private void Update()
        {
            bool isGrounded = _controller.isGrounded;
            _isFocused = _inputManager.Focus;
            _rollSpeed = walkSpeed * rollSpeedMultiplier;

            _animator.SetBool("IsGrounded", isGrounded);

            UpdateAirTime(isGrounded);      // Tracks how long the player has been in the air
            HandleFallingSound(isGrounded); // Controls when the falling sound should play and its intensity

            HandleMoveAnim();               // Checks if the player is moving the character
            HandleGrounded(isGrounded);     // Checks if the character is grounded and handles jump and run control

            if (HandleRolling(isGrounded)) return; // Handles roll mechanics and animations (returns true if rolling is active)

            HandleMovement();   // Handles character movement when the player has control
            HandleAnimations(); // Handles animations
            HandleRotation();   // Handles character rotation
        }

        private void LateUpdate()
        {
            _previousAirDuration = 0f; // Ensures the sweetGroundImpactClip is not played multiple times in the same landing
        }

        // === CORE HANDLERS ===
        private void HandleMoveAnim()
        {
            _animator.SetBool("Move", _inputManager.Move != Vector2.zero);
        }

        private void HandleGrounded(bool isGrounded)
        {
            HandleRunning();

            if (isGrounded && _playerVelocity.y < 0)
                _playerVelocity.y = 0;

            if (isGrounded)
            {
                _inputVector = (_cameraTransform != null ? _cameraTransform.forward : transform.forward) * _inputManager.Move.y +
                               (_cameraTransform != null ? _cameraTransform.right : transform.right) * _inputManager.Move.x;
                _inputVector.y = 0;
            }

            HandleJump(isGrounded);
        }

        // Handles running mechanics and animations
        private void HandleRunning()
        {
            if (_inputManager.Run)
                _isRunning = !_isRunning;

            if (_inputVector == Vector3.zero && _inputManager.Move == Vector2.zero)
                _isRunning = false;

            _currentSpeed = _isRunning ? runMultiplier * walkSpeed : walkSpeed;
            _animator.SetBool("Run", _isRunning);
        }

        // Handles jumping mechanics and animations
        private void HandleJump(bool isGrounded)
        {
            if (_inputManager.Jump && isGrounded)
            {
                _playerVelocity.y = Mathf.Sqrt(jumpHeight * jumpMultiplier * gravity);
                _timeInAir -= 0.3f;

                _animator.SetTrigger("Jump");
                AudioClip clip = GetRandomClip(jumpClips);
                if (clip != null) _audioSource.PlayOneShot(clip, 0.5f);
            }
        }

        // Handles roll mechanics and animations (returns true if rolling is active)
        private bool HandleRolling(bool isGrounded)
        {
            if (_inputManager.Roll && isGrounded && !_isRolling)
            {
                StartRoll();
            }
            else if (_previousAirDuration >= 1f && isGrounded)
            {
                StartRoll();
                if (groundImpactClip != null)
                    _audioSource.PlayOneShot(groundImpactClip, 0.2f);

                _previousAirDuration = 0f;
            }

            if (_isRolling)
            {
                _playerVelocity.y += gravity * Time.deltaTime;
                Vector3 rollMovement = (_rollDirection * _rollSpeed) + _playerVelocity;
                _controller.Move(rollMovement * Time.deltaTime);

                _rollTimer -= Time.deltaTime;
                if (_rollTimer <= 0f)
                    _isRolling = false;

                return true;
            }

            return false;
        }

        // Handles character movement when the player has control
        private void HandleMovement()
        {
            _smoothedDirection = Vector3.SmoothDamp(_smoothedDirection, _inputVector, ref _currentVelocity, smoothTime);
            _playerVelocity.y += gravity * Time.deltaTime;

            Vector3 finalMove = (_smoothedDirection * _currentSpeed) + _playerVelocity;
            _controller.Move(finalMove * Time.deltaTime);
        }

        // Handles animations
        private void HandleAnimations()
        {
            _animator.SetFloat("MovingSpeed", _smoothedDirection.magnitude);
            _animator.SetBool("Falling", _timeInAir >= fallAnimDelay && _playerVelocity.y < -0.1f);

            Vector3 localMovement = transform.InverseTransformDirection(_smoothedDirection);
            _animator.SetFloat("PosX", localMovement.x);
            _animator.SetFloat("PosY", localMovement.z);
        }

        // Handles character rotation
        private void HandleRotation()
        {
            if (!_isFocused && _smoothedDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(_smoothedDirection),
                    rotationSpeed * Time.deltaTime
                );
            }
            else if (_isFocused && _cameraTransform != null)
            {
                Vector3 cameraForward = new Vector3(_cameraTransform.forward.x, 0, _cameraTransform.forward.z).normalized;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(cameraForward),
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        // === SUPPORT METHODS ===
        // Starts the rolling action (used by HandleRolling)
        private void StartRoll()
        {
            _isRolling = true;
            _rollTimer = rollDuration;
            _animator.SetTrigger("Roll");
            _rollDirection = (_smoothedDirection == Vector3.zero) ? transform.forward : _smoothedDirection.normalized;
        }

        // Tracks how long the player has been in the air
        private void UpdateAirTime(bool isGrounded)
        {
            if (isGrounded)
            {
                _previousAirDuration = _timeInAir;
                _timeInAir = 0;
            }
            else
            {
                _timeInAir += Time.deltaTime;
            }
        }

        // Controls when the falling sound should play and its intensity
        private void HandleFallingSound(bool isGrounded)
        {
            if (isGrounded)
            {
                if (_audioSource.isPlaying && _audioSource.clip == fallingClip)
                {
                    _audioSource.Stop();
                    _audioSource.clip = null;
                }
            }
            else if (!_audioSource.isPlaying && _timeInAir > fallSoundDelay && fallingClip != null)
            {
                _audioSource.clip = fallingClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }

            if (_previousAirDuration > 0.4f && _previousAirDuration < 1f && sweetGroundImpactClip != null)
            {
                _audioSource.PlayOneShot(sweetGroundImpactClip, 0.2f);
            }
        }

        // Returns a random clip from an array of clips
        private AudioClip GetRandomClip(AudioClip[] clips)
        {
            return (clips != null && clips.Length > 0) ? clips[Random.Range(0, clips.Length)] : null;
        }

        // === ANIMATION EVENTS ===
        // Handle when a sound (triggered by an animation event) should start and stop
        public void HandleFootstep()
        {
            if (!_hasPlayedFootstepSound && footstepClip && _controller.isGrounded)
            {
                _audioSource.PlayOneShot(footstepClip);
                _hasPlayedFootstepSound = true;
            }
        }

        public void HandleRollSound()
        {
            if (!_hasPlayedRollSound && _controller.isGrounded && rollClips.Length > 0)
            {
                AudioClip clip = GetRandomClip(rollClips);
                if (clip != null) _audioSource.PlayOneShot(clip, 0.5f);
                _hasPlayedRollSound = true;
            }
        }

        public void StopRolling()
        {
            _isRolling = false;
            _currentVelocity = Vector3.zero;
            _smoothedDirection = Vector3.zero;
            _hasPlayedRollSound = false;
        }

        public void StopFootStep()
        {
            _hasPlayedFootstepSound = false;
        }
    }
}
