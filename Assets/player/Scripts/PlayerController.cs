using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityTutorial.Manager;
//using UnityTutorial.Logging;

namespace UnityTutorial.PlayerControl
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float AnimBlendSpeed = 8.9f;
        [SerializeField] private Transform CameraRoot;
        [SerializeField] private Transform Camera;
        [SerializeField] private float UpperLimit = -40f;
        [SerializeField] private float BottomLimit = 70f;
        [SerializeField] private float MouseSensitivity = 5.0f;
        [SerializeField, Range(10, 500)] private float JumpFactor = 260f;
        [SerializeField] private float Dis2Ground = 0.08f;
        [SerializeField] private LayerMask GroundCheck;
        [SerializeField] private float AirResistance = 0.8f;

        private Rigidbody _playerRigidbody;
        private InputManager _inputManager;
        private Animator _animator;
        private bool _grounded = false;
        private bool _hasAnimator;
        private int _xVelHash;
        private int _yVelHash;
        private int _jumpHash;
        private int _groundHash;
        private int _fallingHash;
        private int _zVelHash;
        private int _crouchHash;
        private float _xRotation;

        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _runSpeed = 6f;
        private Vector2 _currentVelocity;
        public bool canMove = true;

        // raw-mouse accumulation
        private float _accumMouseX;
        private float _accumMouseY;


       [SerializeField] private AudioSource footstepSource;
[SerializeField] private AudioClip footstepClipA;
[SerializeField] private AudioClip footstepClipB;

private bool playFirstStep = true;

[SerializeField] private float footstepInterval = 0.4f;

private float footstepTimer = 0f;
private bool useFirstFoot = true; // pre striedanie krokov

        private void Start()
        {
            _hasAnimator = TryGetComponent<Animator>(out _animator);
            _playerRigidbody = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();

            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _zVelHash = Animator.StringToHash("Z_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");
            _crouchHash = Animator.StringToHash("Crouch");
        }

        private void FixedUpdate()
        {
            SampleGround();
            Move();
            HandleJump();
            HandleCrouch();
            HandleFootsteps();   // ← PRIDANÉ
        }

        private void LateUpdate()
        {
            CamMovements();
        }


        private void CamMovements()
{
    // Stop camera movement if:
    // - no animator
    // - player cannot move
    // - game is paused via PauseMenuController
    if (!_hasAnimator || !canMove || PauseMenuController.IsPaused) return;

    // Read raw mouse delta from new Input System
    Vector2 raw = Mouse.current.delta.ReadValue();

    // Accumulate raw mouse movement
    _accumMouseX += raw.x * MouseSensitivity;
    _accumMouseY += raw.y * MouseSensitivity;

    // Convert pixels to degrees
    const float pixelsPerDegree = 5.0f;
    float dx = Mathf.Clamp(_accumMouseX, -50f, 50f) / pixelsPerDegree;
    float dy = Mathf.Clamp(_accumMouseY, -50f, 50f) / pixelsPerDegree;

    // Subtract used delta
    _accumMouseX -= dx * pixelsPerDegree;
    _accumMouseY -= dy * pixelsPerDegree;

    // Camera position follows player
    Camera.position = CameraRoot.position;

    // Rotate camera vertically
    _xRotation -= dy;
    _xRotation = Mathf.Clamp(_xRotation, UpperLimit, BottomLimit);
    Camera.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

    // Rotate player horizontally
    _playerRigidbody.MoveRotation(
        _playerRigidbody.rotation * Quaternion.Euler(0f, dx, 0f)
    );
}


private void Move()
{
    // Stop movement if no animator or player cannot move
    if (!_hasAnimator) return;
    if (!canMove)
    {
        // stand still (e.g. during dialogue) instead of sliding with the last velocity
        _currentVelocity = Vector2.zero;
        _playerRigidbody.linearVelocity = new Vector3(0f, _playerRigidbody.linearVelocity.y, 0f);
        _animator.SetFloat(_xVelHash, 0f);
        _animator.SetFloat(_yVelHash, 0f);
        return;
    }

    // Determine target speed
    float targetSpeed = _inputManager.Run ? _runSpeed : _walkSpeed;
    if (_inputManager.Crouch) targetSpeed = 2.5f;
    if (_inputManager.Move == Vector2.zero) targetSpeed = 0;

    // Grounded movement
    if (_grounded)
    {
        _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, _inputManager.Move.x * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
        _currentVelocity.y = Mathf.Lerp(_currentVelocity.y, _inputManager.Move.y * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);

        var xVelDifference = _currentVelocity.x - _playerRigidbody.linearVelocity.x;
        var zVelDifference = _currentVelocity.y - _playerRigidbody.linearVelocity.z;

        _playerRigidbody.AddForce(transform.TransformVector(new Vector3(xVelDifference, 0, zVelDifference)), ForceMode.VelocityChange);
    }
    else // Air movement
    {
        _playerRigidbody.AddForce(transform.TransformVector(new Vector3(_currentVelocity.x * AirResistance, 0, _currentVelocity.y * AirResistance)), ForceMode.VelocityChange);
    }

    // Update animator
    _animator.SetFloat(_xVelHash, _currentVelocity.x);
    _animator.SetFloat(_yVelHash, _currentVelocity.y);
}


        private void HandleCrouch() => _animator.SetBool(_crouchHash, _inputManager.Crouch);

        private void HandleJump()
        {
            if (!_hasAnimator || !canMove) return;
            if (!_inputManager.Jump) return;
            if (!_grounded) return;
            _animator.SetTrigger(_jumpHash);
           
           // Firebase log
            if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
            {
                FirebaseGameAnalytics.Instance.LogGameplayEvent("jump", transform.position);
            }
        }

        public void JumpAddForce()
        {
            _playerRigidbody.AddForce(-_playerRigidbody.linearVelocity.y * Vector3.up, ForceMode.VelocityChange);
            _playerRigidbody.AddForce(Vector3.up * JumpFactor, ForceMode.Impulse);
            _animator.ResetTrigger(_jumpHash);
        }

        private void SampleGround()
        {
            if (!_hasAnimator) return;

            // cast from just above the feet; worldCenterOfMass sits ~0.7m up (capsule centre), too high for a short ray
            Vector3 groundRayOrigin = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(groundRayOrigin, Vector3.down, out RaycastHit hitInfo, Dis2Ground + 0.1f, GroundCheck))
            {
                _grounded = true;
                SetAnimationGrounding();
                return;
            }

            _grounded = false;
            _animator.SetFloat(_zVelHash, _playerRigidbody.linearVelocity.y);
            SetAnimationGrounding();
        }

        private void SetAnimationGrounding()
        {
            _animator.SetBool(_fallingHash, !_grounded);
            _animator.SetBool(_groundHash, _grounded);
        }

private void HandleFootsteps()
{
    if (!_grounded || !canMove) return;
    if (_inputManager.Move == Vector2.zero) return;

    // --- ŽIADNY PITCH, zvuk ostáva rovnaký ---
    footstepSource.pitch = 1f;

    // --- Interval podľa pohybu ---
    float interval = footstepInterval;

    if (_inputManager.Run)
    {
        interval *= 0.55f;   // rýchlejšie kroky
    }
    else if (_inputManager.Crouch)
    {
        interval *= 1.4f;    // pomalšie kroky
    }

    footstepTimer -= Time.deltaTime;

    if (footstepTimer <= 0f)
    {
        AudioClip clipToPlay = playFirstStep ? footstepClipA : footstepClipB;

        if (clipToPlay != null)
            footstepSource.PlayOneShot(clipToPlay);

        playFirstStep = !playFirstStep;  // striedanie A ↔ B

        footstepTimer = interval;
    }
}







    }
}