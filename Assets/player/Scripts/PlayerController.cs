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
        [SerializeField] private float JumpCooldown = 0.3f;
        private float _nextJumpTime;

        [Header("Third Person Camera")]
        [SerializeField] private bool StartInThirdPerson = false;
        [SerializeField] private float ThirdPersonDistance = 3f;
        [SerializeField] private float ShoulderOffset = 0.5f;
        [SerializeField] private float ThirdPersonHeight = 0.2f;
        [SerializeField] private float CameraCollisionRadius = 0.2f;
        [SerializeField] private float CameraSwitchSpeed = 8f;

        [Header("Camera Smoothing")]
        [SerializeField, Range(0f, 1f)] private float HeadBobAmount = 0.15f; // 0 = steady camera, 1 = glued to the head bone
        [SerializeField] private float EyeHeightFollowSpeed = 4f;            // how fast the camera follows crouching/leaning
        [SerializeField] private float VerticalSmoothTime = 0.08f;           // softens bumps in the terrain
        [SerializeField] private float GroundSnapDistance = 0.3f;            // stay grounded over small dips while walking

        [Header("First Person Body")]
        [SerializeField] private bool HideHeadInFirstPerson = true; // her head and hair would cover the view when the camera sits inside them
        [SerializeField] private float CastBlendSpeed = 5f;          // how fast the camera locks onto / releases the head while casting

        [Header("Obstacles")]
        [SerializeField] private float StepHeight = 0.4f;       // rocks/edges up to this height are walked over
        [SerializeField] private float FallingDelay = 0.5f;     // short air time (bumps, steps) doesn't play the falling animation

        private CapsuleCollider _capsule;
        private float _airTime;
        private Vector3 _groundNormal = Vector3.up;

        private Vector3 _eyeLocal;
        private bool _eyeInitialized;
        private float _smoothEyeY;
        private float _eyeYVelocity;
        private float _yaw;

        private bool _thirdPerson;
        private float _camBlend; // 0 = first person, 1 = third person
        private float _currentPullback;

        private bool _casting;
        private float _castBlend; // 0 = normal, 1 = camera locked to the head while casting

        private Transform _headBone;
        private Vector3 _headScale;
        private Vector3 _cameraRootPos; // CameraRoot sits on the head bone, so read it while the head has full size

        public bool IsThirdPerson => _thirdPerson;
        // the cast animation leans her forward – the smoothed camera would stay behind, inside her body
        public void SetCasting(bool casting) => _casting = casting;
        // how far the camera currently sits behind the head along its view direction;
        // aim/interaction rays skip this much so reach and self-hits match first person
        public float CameraPullback => _currentPullback;
        // world offset of the camera from its first-person spot
        public Vector3 CameraOffset => Camera.position - _cameraRootPos;

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

            // no friction on the body: with friction she sticks to anything she brushes against;
            // walking speed and stopping are set directly in Move() anyway
            _capsule = GetComponent<CapsuleCollider>();
            if (_capsule != null)
            {
                _capsule.material = new PhysicsMaterial("PlayerNoFriction")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounciness = 0f,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
            }

            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _zVelHash = Animator.StringToHash("Z_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");
            _crouchHash = Animator.StringToHash("Crouch");

            if (_hasAnimator && _animator.isHuman)
                _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
            if (_headBone != null)
                _headScale = _headBone.localScale;

            _thirdPerson = StartInThirdPerson;
            _camBlend = _thirdPerson ? 1f : 0f;
            _yaw = transform.eulerAngles.y;

            // set in the main menu's settings
            MouseSensitivity = PlayerPrefs.GetFloat(MainMenuController.MouseSensitivityKey, MouseSensitivity);
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
            // full-size head first, so CameraRoot (a child of the head bone) is at the eyes
            if (_headBone != null) _headBone.localScale = _headScale;
            _cameraRootPos = CameraRoot.position;

            CamMovements();

            // first person: shrink the head away before the mesh is skinned – the camera sits inside it
            // and the cast animation turns her head, which pulled her face and hair into the view
            if (_headBone != null && HideHeadInFirstPerson && _camBlend < 0.5f)
                _headBone.localScale = _headScale * 0.001f;
        }


        private void CamMovements()
{
    // Stop camera movement if:
    // - no animator
    // - player cannot move
    // - game is paused via PauseMenuController
    if (!_hasAnimator || PauseMenuController.IsPaused) return;
    if (!canMove)
    {
        // something else may turn or teleport the player meanwhile (intro, respawn) – pick that up afterwards
        _yaw = transform.eulerAngles.y;
        _eyeInitialized = false;
        return;
    }

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

    // Toggle first / third person
    if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        _thirdPerson = !_thirdPerson;

    // Rotate camera: vertically, and horizontally right away every frame –
    // the body only turns on physics steps, so following it would make the view stutter
    _xRotation -= dy;
    _xRotation = Mathf.Clamp(_xRotation, UpperLimit, BottomLimit);
    _yaw += dx;
    Camera.rotation = Quaternion.Euler(_xRotation, _yaw, 0f);

    // Camera position follows player (pulled back behind the shoulder in third person)
    _camBlend = Mathf.MoveTowards(_camBlend, _thirdPerson ? 1f : 0f, CameraSwitchSpeed * Time.deltaTime);
    _castBlend = Mathf.MoveTowards(_castBlend, _casting ? 1f : 0f, CastBlendSpeed * Time.deltaTime);
    PositionCamera(GetEyePivot());

    // Rotate player horizontally
    _playerRigidbody.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));
}

// Steady eye point: the head bone bobs and sways with every step of the walk animation,
// so follow its averaged position and keep only a little of the bob.
private Vector3 GetEyePivot()
{
    Vector3 headLocal = transform.InverseTransformPoint(CameraRoot.position);
    if (!_eyeInitialized)
    {
        _eyeLocal = headLocal;
        _smoothEyeY = CameraRoot.position.y;
        _eyeYVelocity = 0f;
        _eyeInitialized = true;
    }

    _eyeLocal = Vector3.Lerp(_eyeLocal, headLocal, EyeHeightFollowSpeed * Time.deltaTime);
    // while casting, follow the head fully so the camera moves with her when she leans in
    float follow = Mathf.Lerp(HeadBobAmount, 1f, _castBlend);
    Vector3 pivot = Vector3.Lerp(transform.TransformPoint(_eyeLocal), CameraRoot.position, follow);

    // soften small steps of uneven ground; snap on big jumps (teleports)
    if (Mathf.Abs(pivot.y - _smoothEyeY) > 1f)
    {
        _smoothEyeY = pivot.y;
        _eyeYVelocity = 0f;
    }
    _smoothEyeY = Mathf.SmoothDamp(_smoothEyeY, pivot.y, ref _eyeYVelocity, VerticalSmoothTime);
    pivot.y = _smoothEyeY;
    return pivot;
}

private void PositionCamera(Vector3 pivot)
{
    if (_camBlend <= 0f)
    {
        Camera.position = pivot;
        _currentPullback = 0f;
        return;
    }

    float t = Mathf.SmoothStep(0f, 1f, _camBlend);
    Vector3 offset = Camera.rotation * new Vector3(ShoulderOffset, ThirdPersonHeight, -ThirdPersonDistance) * t;
    float distance = offset.magnitude;
    Vector3 dir = offset / distance;

    // keep the camera out of walls/terrain, ignoring the player's own colliders
    foreach (RaycastHit h in Physics.SphereCastAll(pivot, CameraCollisionRadius, dir, distance, ~0, QueryTriggerInteraction.Ignore))
    {
        if (h.transform.IsChildOf(transform) || h.distance >= distance) continue;
        distance = h.distance;
    }

    Camera.position = pivot + dir * distance;
    _currentPullback = Mathf.Max(0f, Vector3.Dot(pivot - Camera.position, Camera.forward));
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
        _playerRigidbody.useGravity = !_grounded;
        _animator.SetFloat(_xVelHash, 0f);
        _animator.SetFloat(_yVelHash, 0f);
        return;
    }

    // Determine target speed
    float targetSpeed = _inputManager.Run ? _runSpeed : _walkSpeed;
    if (_inputManager.Crouch) targetSpeed = 2.5f;
    if (_inputManager.Move == Vector2.zero) targetSpeed = 0;

    // compare in the player's own space – _currentVelocity is relative to where she faces,
    // the rigidbody velocity is in world space (mixing them made the speed overshoot back and forth)
    Quaternion facing = _playerRigidbody.rotation;
    Vector3 worldVelocity = _playerRigidbody.linearVelocity;
    Vector3 localVelocity = Quaternion.Inverse(facing) * worldVelocity;

    // Grounded movement
    if (_grounded)
    {
        _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, _inputManager.Move.x * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
        _currentVelocity.y = Mathf.Lerp(_currentVelocity.y, _inputManager.Move.y * targetSpeed, AnimBlendSpeed * Time.fixedDeltaTime);

        var xVelDifference = _currentVelocity.x - localVelocity.x;
        var zVelDifference = _currentVelocity.y - localVelocity.z;

        _playerRigidbody.AddForce(facing * new Vector3(xVelDifference, 0, zVelDifference), ForceMode.VelocityChange);

        Vector3 moveDir = facing * new Vector3(_currentVelocity.x, 0, _currentVelocity.y);

        // downhill: follow the slope instead of walking straight out into the air
        // (gravity alone pulled her down too slowly, so she lost the ground and played the falling animation)
        if (worldVelocity.y < 0.5f && Time.time >= _nextJumpTime)
        {
            Vector3 alongGround = Vector3.ProjectOnPlane(moveDir, _groundNormal);
            if (alongGround.sqrMagnitude > 0.0001f)
            {
                float downhillY = alongGround.normalized.y * moveDir.magnitude;
                if (downhillY < 0f && worldVelocity.y > downhillY)
                    _playerRigidbody.AddForce(Vector3.up * (downhillY - worldVelocity.y), ForceMode.VelocityChange);
            }
        }
        if (_inputManager.Move != Vector2.zero && moveDir.sqrMagnitude > 0.01f)
            StepUp(moveDir.normalized);
    }
    else // Air movement: keep momentum, steer gently toward the input direction
    {
        Vector3 target = facing * new Vector3(_currentVelocity.x, 0, _currentVelocity.y);
        Vector3 horizontal = new Vector3(worldVelocity.x, 0, worldVelocity.z);
        Vector3 steer = Vector3.ClampMagnitude(target - horizontal, AirResistance * 10f * Time.fixedDeltaTime);
        _playerRigidbody.AddForce(steer, ForceMode.VelocityChange);
    }

    // without friction she would slowly slide down slopes while standing, so stand still without gravity
    _playerRigidbody.useGravity = !(_grounded && _inputManager.Move == Vector2.zero && _playerRigidbody.linearVelocity.y < 0.5f);

    // Update animator
    _animator.SetFloat(_xVelHash, _currentVelocity.x);
    _animator.SetFloat(_yVelHash, _currentVelocity.y);
}


        private void HandleCrouch() => _animator.SetBool(_crouchHash, _inputManager.Crouch);

        private void HandleJump()
        {
            if (!_hasAnimator || !canMove) return;
            if (!_inputManager.Jump) return;
            if (!_grounded || Time.time < _nextJumpTime) return;
            _animator.SetTrigger(_jumpHash);

            // push off right away – waiting for the animation event made the jump feel late
            _playerRigidbody.AddForce(-_playerRigidbody.linearVelocity.y * Vector3.up, ForceMode.VelocityChange);
            _playerRigidbody.AddForce(Vector3.up * JumpFactor, ForceMode.Impulse);
            _playerRigidbody.useGravity = true;
            // she is still near the ground for a few physics steps – don't jump again from there
            _nextJumpTime = Time.time + JumpCooldown;

           // Firebase log
            if (FirebaseGameAnalytics.Instance != null && FirebaseGameAnalytics.Instance.IsReady)
            {
                FirebaseGameAnalytics.Instance.LogGameplayEvent("jump", transform.position);
            }
        }

        // animation event in the Jump clip; the force is already applied in HandleJump
        public void JumpAddForce()
        {
            _animator.ResetTrigger(_jumpHash);
        }

        private void SampleGround()
        {
            if (!_hasAnimator) return;

            // sphere instead of a thin ray from the centre: standing on the edge of a rock still counts as ground.
            // cast from just above the feet (the sphere's bottom starts 0.1m up, like the old ray)
            float radius = _capsule != null ? _capsule.radius * 0.8f : 0.2f;
            Vector3 origin = _playerRigidbody.position + Vector3.up * (0.1f + radius);
            // while already walking on the ground, look a bit further down so small dips and bumps
            // in the terrain don't flicker into the falling state (not while moving up, i.e. jumping)
            float castLength = Dis2Ground + 0.1f;
            if (_grounded && _playerRigidbody.linearVelocity.y < 0.5f) castLength += GroundSnapDistance;

            bool found = false;
            RaycastHit hitInfo = default;
            foreach (RaycastHit h in Physics.SphereCastAll(origin, radius, Vector3.down, castLength, GroundCheck, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(transform) || h.normal.y < 0.4f) continue; // own colliders, walls
                if (found && h.distance >= hitInfo.distance) continue;
                hitInfo = h;
                found = true;
            }

            if (found)
            {
                // snapped over a dip: pull down onto the ground instead of floating off the edge
                if (hitInfo.distance > Dis2Ground + 0.1f && _playerRigidbody.linearVelocity.y > -2f)
                    _playerRigidbody.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);

                _grounded = true;
                _groundNormal = hitInfo.normal;
                _airTime = 0f;
                SetAnimationGrounding();
                return;
            }

            _grounded = false;
            _groundNormal = Vector3.up;
            _airTime += Time.fixedDeltaTime;
            _animator.SetFloat(_zVelHash, _playerRigidbody.linearVelocity.y);
            SetAnimationGrounding();
        }

        // Movement is done with the rigidbody in Move(). Having this method tells the Animator that
        // root motion is handled by script, so the animations no longer overwrite her velocity
        // (with "Apply Root Motion" on, every walk/fall clip kept resetting the speed and lifting her off the ground).
        private void OnAnimatorMove() { }

        private void SetAnimationGrounding()
        {
            // a moment in the air (bump, stepping off a rock) keeps the walk animation
            bool animGrounded = _grounded || _airTime < FallingDelay;
            _animator.SetBool(_fallingHash, !animGrounded);
            _animator.SetBool(_groundHash, animGrounded);
        }

        // Walk up onto small rocks and edges instead of stopping against them.
        private void StepUp(Vector3 moveDir)
        {
            if (_capsule == null) return;

            Vector3 feet = _playerRigidbody.position;
            Vector3 side = Vector3.Cross(Vector3.up, moveDir) * (_capsule.radius * 0.7f);
            float reach = _capsule.radius + 0.15f;
            float highest = 0f;

            // centre and both sides of the body
            for (int i = -1; i <= 1; i++)
            {
                Vector3 low = feet + side * i + Vector3.up * 0.05f;
                if (!Physics.Raycast(low, moveDir, out RaycastHit wall, reach, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (wall.transform.IsChildOf(transform)) continue;
                if (wall.collider is TerrainCollider || wall.normal.y > 0.7f) continue; // slopes are walked normally

                // something at knee height in front -> too tall to step on
                Vector3 high = feet + side * i + Vector3.up * (StepHeight + 0.05f);
                if (Physics.Raycast(high, moveDir, reach + 0.1f, ~0, QueryTriggerInteraction.Ignore)) continue;

                // find the top of the obstacle
                Vector3 above = wall.point + moveDir * 0.05f + Vector3.up * (StepHeight + 0.05f);
                if (!Physics.Raycast(above, Vector3.down, out RaycastHit top, StepHeight + 0.1f, ~0, QueryTriggerInteraction.Ignore)) continue;

                float rise = top.point.y - feet.y;
                if (rise > 0.02f && rise <= StepHeight && top.normal.y > 0.5f)
                    highest = Mathf.Max(highest, rise);
            }

            if (highest <= 0f) return;

            // small hop just high enough to land on top (smooth with interpolation, unlike teleporting up)
            Vector3 v = _playerRigidbody.linearVelocity;
            v.y = Mathf.Max(v.y, Mathf.Sqrt(2f * -Physics.gravity.y * (highest + 0.05f)));
            _playerRigidbody.linearVelocity = v;
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