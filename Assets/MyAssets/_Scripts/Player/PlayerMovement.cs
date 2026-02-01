using NUnit.Framework.Constraints;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementStats MoveStats;
    [SerializeField] private Collider2D _coll;

    [Header("Camera Stuff")]
    [SerializeField] private GameObject _cameraFollowGo;

    private Rigidbody2D _rb;

    //movement vars
    public bool IsFacingRight { get; private set; }
    public MovementController Controller { get; private set; }
    [HideInInspector]public Vector2 Velocity;

    //input
    private Vector2 _moveInput;
    private bool _sprintHeld;
    private bool _jumpPressed;
    private bool _jumpReleased;
    private bool _dashPressed;

    //jump vars
    private bool _isJumping;
    private bool _isFallingFast;
    private bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfAirJumpsUsed;

    //apex vars
    private float _apexPoint;
    private float _timePastApexThreshold;
    private bool _isPastApexThreshold;

    //jump buffer vars
    private float _jumpBufferTimer;
    private bool _jumpReleasedDuringBuffer;

    //coyote time vars
    private float _coyoteTimer;

    //wall slide wars
    private bool _isWallSliding;
    private bool _isWallSlideFalling;

    //wall jump wars
    private bool _useWallJumpMoveStats;
    private bool _isWallJumping;
    private float _wallJumpTime;
    private bool _isWallJumpFastFalling;
    private bool _isWallJumpFalling;
    private float _wallJumpFastFallTime;
    private float _wallJumpFastFallReleaseSpeed;

    private float _wallJumpPostBufferTimer;

    private float _wallJumpApexPoint;
    private float _timePastWallJumpApexThreshold;
    private bool _isPastWallJumpApexThreshold;

    //dash vars
    public bool IsDashing { get; private set; }
    private bool _isAirDashing;
    private float _dashTimer;
    private float _dashOnGroundTimer;
    private int _numberOfDashesUsed;
    private Vector2 _dashDirection;
    private bool _isDashFastFalling;
    private float _dashFastFallTime;
    private float _dashFastFallReleaseSpeed;
    private float _dashBufferTimer;

    //head bump slide vars
    private float _jumpStartY;
    public bool IsHeadBumpSliding { get; private set; }
    private bool _justFinishedSlide;
    private bool _slideFromDash;
    private float _dashStartY;
    private bool _didHeadBumpSlideThisAirborneState;

    //slope vars
    private bool _isPerformingSlopeDash;
    private float _slopeDashAngle;

    //camera vars
    private CameraFollowObject _cameraFollowObject;
    private float _fallSpeedYDampingChangeThreshold;


    private void Awake()
    {
        IsFacingRight = true;

        _rb = GetComponent<Rigidbody2D>();

        Controller = GetComponent<MovementController>();

        _cameraFollowObject = _cameraFollowGo.GetComponent<CameraFollowObject>();

    }

    private void Start()
    {
        _fallSpeedYDampingChangeThreshold = CameraManager.instance._fallSpeedYDampingChangeThreshold;
    }

    private void Update()
    {
        _moveInput = InputManager.Movement;
        _sprintHeld = InputManager.SprintIsHeld;
        if (InputManager.JumpWasPressed) _jumpPressed = true;
        if (InputManager.JumpWasReleased) _jumpReleased = true;
        if (InputManager.DashWasPressed) _dashPressed = true;

        //if we are falling past a certain speed threshold
        if (Velocity.y < _fallSpeedYDampingChangeThreshold && !CameraManager.instance.IsLerpingYDamping && !CameraManager.instance.LerpedFromPlayerFalling)
        {
            CameraManager.instance.LerpYDamping(true);
        }

        //if we are staying still or moving up
        if (Velocity.y >= 0f && !CameraManager.instance.IsLerpingYDamping && CameraManager.instance.LerpedFromPlayerFalling)
        {
            //reset so it can be called again
            CameraManager.instance.LerpedFromPlayerFalling = false;

            CameraManager.instance.LerpYDamping(false);
        }
    }


    private void FixedUpdate()
    {
        _justFinishedSlide = false;

        CountTimers(Time.fixedDeltaTime);

        JumpChecks();
        LandCheck();
        WallJumpCheck();
        WallSlideCheck();
        DashCheck();
        VelocityReset();

        HandleHorizontalMovement(Time.fixedDeltaTime);
        HandleHeadBumpSlide();
        Jump(Time.fixedDeltaTime);
        WallSlide(Time.fixedDeltaTime);
        WallJump(Time.fixedDeltaTime);
        Dash(Time.fixedDeltaTime);
        Fall(Time.fixedDeltaTime);
        HandleSlide(Time.fixedDeltaTime);

        ClampVelocity();

        Controller.Move(Velocity * Time.fixedDeltaTime);

        //reset input bools
        _jumpPressed = false;
        _jumpReleased = false;
        _dashPressed = false;
    }

    private void ClampVelocity()
    {
        if (Controller.IsSliding)
        {
            Velocity.y = Mathf.Clamp(Velocity.y, -MoveStats.SlideSpeed, 50);
        }
        else if (IsDashing)
        { 
            Velocity.y = Mathf.Clamp(Velocity.y, -50f, 50f);
        }
        else
        {
            Velocity.y = Mathf.Clamp(Velocity.y, -MoveStats.MaxFallSpeed, 50f);
        }
    }


    #region Movement

    private void HandleHorizontalMovement(float timeStep)
    {
        float horizontalInput = _moveInput.x;
        bool hasHorizontalInput = !Mathf.Approximately(horizontalInput, 0f);
        bool hasVerticalInput = !Mathf.Approximately(_moveInput.y, 0f);

        if (IsHeadBumpSliding) return;


        if (!IsDashing)
        {

            float acceleration = Controller.IsGrounded() ? MoveStats.GroundAcceleration : MoveStats.AirAcceleration;
            float deceleration = Controller.IsGrounded() ? MoveStats.GroundDeceleration : MoveStats.AirDeceleration;

            if (_useWallJumpMoveStats)
            {
                acceleration = MoveStats.WallJumpMoveAcceleration;
                deceleration = MoveStats.WallJumpMoveDeceleration;
            }

            if (hasHorizontalInput || (!Controller.IsGrounded() && hasVerticalInput))
            {
                TurnCheck(_moveInput);

                float moveDirection = Mathf.Sign(_moveInput.x);
                float targetVelocityX = _sprintHeld ? moveDirection * MoveStats.MaxSprintSpeed : moveDirection * MoveStats.MaxWalkSpeed;

                float t = Mathf.Clamp01(acceleration * timeStep);

                Velocity.x = Mathf.Lerp(Velocity.x, targetVelocityX, t);

                if (Mathf.Abs(Velocity.x - targetVelocityX) <= 0.01f)
                {
                    Velocity.x = targetVelocityX;
                }
            }

            else
            {
                float t = Mathf.Clamp01(deceleration * timeStep);
                Velocity.x = Mathf.Lerp(Velocity.x, 0, t);

                if (Mathf.Abs(Velocity.x) <= 0.01f)
                {
                    Velocity.x = 0f;
                }
            }
        }
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (IsFacingRight && moveInput.x < 0)
        {
            Turn(false);
        }
        else if (!IsFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }

    }

    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            IsFacingRight = true;
            _cameraFollowObject.CallTurn();
        }
        else
        {
            IsFacingRight = false;
            _cameraFollowObject.CallTurn();
        }

    }

    private void HandleHeadBumpSlide()
    {
        if (!IsHeadBumpSliding && !_didHeadBumpSlideThisAirborneState && (_isJumping || IsDashing || _isWallJumping) && Controller.BumpedHead() && !Controller.IsHittingBothCorners && !Controller.IsHittingCeilingCenter)
        {
            if (_isWallSliding || Controller.IsSliding)
            {
                return;
            }

            if (Controller.CeilingAngle <= MoveStats.MaxSlopeAngleForHeadBump)
            {
                IsHeadBumpSliding = true;
                _didHeadBumpSlideThisAirborneState = true;
            }
        }

        if (IsHeadBumpSliding)
        {
            Velocity.y = 0;

            if (Controller.HeadBumpSlideDirection == 0 || !Controller.BumpedHead() || Controller.IsHittingCeilingCenter || Controller.IsHittingBothCorners)
            {
                IsHeadBumpSliding = false;
                Velocity.x = 0;

                if (!_slideFromDash)
                {
                    float compensationFactor = (1 - MoveStats.JumpHeightCompensationFactor) + 1;
                    float jumpPeakY = _jumpStartY + (MoveStats.JumpHeight * compensationFactor);
                    float remainingHeight = jumpPeakY - _rb.position.y;

                    if (remainingHeight > 0)
                    {
                        float requiredVelocity = Mathf.Sqrt(2 * Mathf.Abs(MoveStats.Gravity) * remainingHeight);
                        Velocity.y = requiredVelocity;
                    }
                }

                else if (_slideFromDash)
                {
                    float targetApexY = _dashStartY + MoveStats.DashTargetApexHeight;
                    float remainingHeight = targetApexY - _rb.position.y;

                    if (remainingHeight > 0)
                    {
                        float requiredVelocity = Mathf.Sqrt(2 * Mathf.Abs(MoveStats.Gravity) * remainingHeight);
                        Velocity.y = requiredVelocity;
                    }
                }

                _slideFromDash = false;
                _justFinishedSlide = true;
            }
            else
            {
                Velocity.x = Controller.HeadBumpSlideDirection * MoveStats.HeadBumpSlideSpeed;
            }
        }
    }


    #endregion

    #region Land/Fall

    private void LandCheck()
    {
        if(Controller.IsGrounded())
        {
            bool isGroundAWall = Controller.SlopeAngle >= MoveStats.MinAngleForWallSlide && Controller.SlopeAngle <= MoveStats.MaxAngleForWallSlide;

            if (isGroundAWall)
            {
                return;    
            }

            //LANDED
            if ((_isJumping || _isFalling || _isWallJumpFalling || _isWallJumping || _isWallSlideFalling || _isWallSliding || _isDashFastFalling ||
                IsHeadBumpSliding) && Velocity.y <= 0f)
            {
                IsHeadBumpSliding = false;
                _didHeadBumpSlideThisAirborneState = false;

                ResetJumpValues();
                StopWallSlide();
                ResetWallJumpValues();
                ResetDashes();
                ResetDashValues();

            }

            if (MoveStats.ResetAirJumpsOnMaxSlopeLand || (!MoveStats.ResetAirJumpsOnMaxSlopeLand && Controller.SlopeAngle <= MoveStats.MaxSlopeAngle))
            {
                _numberOfAirJumpsUsed = 0;
            }
        }
    }

    private void Fall(float timeStep)
    {
        //NORMAL GRAVITY WHILE FALLING
        if (!Controller.IsGrounded() && !_isJumping && !_isWallSliding && !_isWallJumping && !IsDashing && !_isDashFastFalling)
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }
            Velocity.y += MoveStats.Gravity * timeStep;
        }
    }

    private void VelocityReset()
    {
        if (Controller.IsSliding) return;

        if (Controller.IsGrounded())
        {
            if (!IsSlideableSlope(Controller.SlopeAngle) && !Controller.IsOnSlideableSlope)
            {
                if (Velocity.y <= 0f)
                {
                    Velocity.y = -2f;
                }
            }
        }        
    }


    #endregion

    #region Jump

    private void ResetJumpValues()
    {
        _isJumping = false;
        _isFalling = false;
        _isFallingFast = false;
        _fastFallTime = 0f;
        _isPastApexThreshold = false;
    }

    private void JumpChecks()
    {
        //WHEN WE PRESS THE JUMP BUTTON
        if (_jumpPressed)
        {
            if (_isWallSlideFalling && _wallJumpPostBufferTimer >= 0f)
            {
                return;
            }

            else if (_isWallSliding || (Controller.IsTouchingWall(IsFacingRight) && !Controller.IsGrounded()))
            {
                return;
            }

                _jumpBufferTimer = MoveStats.JumpBufferTime;
            _jumpReleasedDuringBuffer = false;
        }

        //WHEN WE RELEASE THE JUMP BUTTON
        if (_jumpReleased)
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpReleasedDuringBuffer = true;
            }

            if (_isJumping && Velocity.y > 0f)
            {
                if (_isPastApexThreshold)
                {
                    _isPastApexThreshold = false;
                    _isFallingFast = true;
                    _fastFallTime = MoveStats.TimeForUpwardsCancel;
                    Velocity.y = 0f;
                }
                else
                {
                    _isFallingFast = true;
                    _fastFallReleaseSpeed = Velocity.y;
                }

            }


        }

        //INITIATE JUMP WITH JUMP BUFFERING AND COYOTE TIME

        if (_jumpBufferTimer > 0f && !_isJumping && (Controller.IsGrounded() || _coyoteTimer > 0f) && (MoveStats.CanJumpOnMaxSlopes || Controller.SlopeAngle <= MoveStats.MaxSlopeAngle))
        {
            InitiateJump(0);

            if (_jumpReleasedDuringBuffer)
            {
                _isFallingFast = true;
                _fastFallReleaseSpeed = Velocity.y;
            }
        }

        //DOUBLE JUMP

        else if (_jumpBufferTimer > 0f && (_isJumping || _isWallJumping || _isWallSlideFalling || _isAirDashing ||
            _isDashFastFalling || Controller.IsSliding) && !Controller.IsTouchingWall(IsFacingRight) && _numberOfAirJumpsUsed < MoveStats.NumberOfAirJumpsAllowed)
        {
            _isFallingFast = false;
            InitiateJump(1);

            if (_isDashFastFalling)
            {
                _isDashFastFalling = false;
            }
        }

        //AIR JUMP AFTER COYOTE TIME LAPSED
        else if (_jumpBufferTimer > 0f && _isFalling && !_isWallSlideFalling && _numberOfAirJumpsUsed < MoveStats.NumberOfAirJumpsAllowed)
        {
            InitiateJump(1);
            _isFallingFast = false;
        }
    }

    private void InitiateJump(int numberOfAirJumpsUsed)
    {
        if (!_isJumping)
        {
            _isJumping = true;
        }

        ResetWallJumpValues();

        _jumpBufferTimer = 0f;
        _numberOfAirJumpsUsed += numberOfAirJumpsUsed;
        Velocity.y = MoveStats.InitialJumpVelocity;
        _didHeadBumpSlideThisAirborneState = false;

        _jumpStartY = _rb.position.y;
    }

    private void Jump(float timeStep)
    {
        //APPLY GRAVITY WHILE JUMPING

        if(_isJumping){

            //CHECK FOR HEAD BUMP

            if (Controller.BumpedHead() && !IsHeadBumpSliding)
            {
                if (Controller.HeadBumpSlideDirection != 0 && !Controller.IsHittingCeilingCenter && !Controller.IsHittingBothCorners)
                {
                    _slideFromDash = false;
                }
                else if (MoveStats.JumpFollowSlopesWhenHeadTouching && Controller.CeilingAngle > 0f)
                {
                    Vector2 ceilingNormal = Controller.CeilingNormal;

                    Velocity = Velocity - (Vector2.Dot(Velocity, ceilingNormal) * ceilingNormal);
                }
                else
                {
                    Velocity.y = 0f;
                    _isFallingFast = true;
                }
            }

            if (IsHeadBumpSliding)
            {
                Velocity.y = 0f;
                return;
            } 

            if (!_justFinishedSlide)
            {
                //GRAVITY ON ASCENDING

                if (Velocity.y >= 0f)
                {

                    //APEX CONTROLS
                    _apexPoint = Mathf.InverseLerp(MoveStats.InitialJumpVelocity, 0f, Velocity.y);

                    if (_apexPoint > MoveStats.ApexThreshold)
                    {
                        if (!_isPastApexThreshold)
                        {
                            _isPastApexThreshold = true;
                            _timePastApexThreshold = 0f;
                        }

                        if (_isPastApexThreshold)
                        {
                            _timePastApexThreshold += timeStep;
                            if (_timePastApexThreshold < MoveStats.ApexHangTime)
                            {
                                Velocity.y = 0f;
                            }
                            else
                            {
                                Velocity.y = -0.01f;
                            }
                        }
                    }

                    //GRAVITY ON ASCENDING BUT NOT PAST APEX THRESHOLD

                    else if (!_isFallingFast)
                    {
                        Velocity.y += MoveStats.Gravity * timeStep;
                        if (_isPastApexThreshold)
                        {
                            _isPastApexThreshold = false;
                        }
                    }

                }

                //GRAVITY ON DESCENDING

                else if (!_isFallingFast)
                {
                    Velocity.y += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * timeStep;
                }

                else if (Velocity.y < 0f)
                {
                    if (!_isFalling)
                    {
                        _isFalling = true;
                    }
                }

            }

        }

        //JUMP CUT
        if (_isFallingFast)
        {
            if (_fastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * timeStep;
            }

            else if (_fastFallTime < MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y = Mathf.Lerp(_fastFallReleaseSpeed, 0f, (_fastFallTime / MoveStats.TimeForUpwardsCancel));
            }

            _fastFallTime += timeStep; 
        }
    }
    #endregion

    #region Wall Slide

    private void WallSlideCheck()
    {
        bool isTouchingSideWall = Controller.IsTouchingWall(IsFacingRight);
        bool isSideWallAngle = Controller.WallAngle >= MoveStats.MinAngleForWallSlide && Controller.WallAngle <= MoveStats.MaxAngleForWallSlide;

        if (!IsDashing && isTouchingSideWall && isSideWallAngle && !Controller.IsGrounded())
        {
            if (Velocity.y < 0f && !_isWallSliding)
            {  
                ResetJumpValues();
                ResetWallJumpValues();
                ResetDashValues();

                if (MoveStats.ResetDashOnWallSlide)
                {
                    ResetDashes();
                }

                _isWallSlideFalling = false;
                _isWallSliding = true;

                if (MoveStats.ResetJumpsOnWallSlide)
                {
                    _numberOfAirJumpsUsed = 0;
                }
            }
        }

        else if (_isWallSliding && !isTouchingSideWall)
        {
            _isWallSlideFalling = true;
            StopWallSlide();
        }

        else 
        {
            StopWallSlide();
        }
    }

    private void StopWallSlide()
    {
        if (_isWallSliding)
        {
            _isWallSliding = false;
        }
    }

    private void WallSlide(float timeStep)
    {
        if (_isWallSliding)
        {
            Velocity.y = Mathf.Lerp(Velocity.y, -MoveStats.WallSlideSpeed, MoveStats.WallSlideDecelerationSpeed * timeStep);
        }
    }

    #endregion

    #region Wall Jump

    private void WallJumpCheck()
    {
        if (ShouldApplyPostWallJumpBuffer())
        {
            _wallJumpPostBufferTimer = MoveStats.WallJumpPostBufferTime;
        }

        //wall jump fast falling
        if (_jumpReleased && !_isWallSliding && !Controller.IsTouchingWall(IsFacingRight) && _isWallJumping)
        {
            if (Velocity.y > 0f)
            {
                if (_isPastWallJumpApexThreshold)
                {
                    _isPastWallJumpApexThreshold = false;
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallTime = MoveStats.TimeForUpwardsCancel;

                    Velocity.y = 0f;
                }
                else
                {
                    _isWallJumpFastFalling = true;
                    _wallJumpFastFallReleaseSpeed = Velocity.y;
                }
            }
        }

        //actual jump with post wall jump buffer time
        if (_jumpPressed && _wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();
        }
    }

    private void InitiateWallJump()
    {
        if (!_isWallJumping) {
            _isWallJumping = true;
            _useWallJumpMoveStats = true;
        }

        StopWallSlide();
        ResetJumpValues();
        _wallJumpTime = 0f;

        Velocity.y = MoveStats.InitialWallJumpVelocity;
        Velocity.x = Mathf.Abs(MoveStats.WallJumpDirection.x) * -Controller.GetWallDirection();
        _didHeadBumpSlideThisAirborneState = false;

        _jumpStartY = _rb.position.y;
    }

    private void WallJump(float timeStep)
    {
        //APPLY WALL JUMP GRAVITY
        if (_isWallJumping)
        {
            //TIME TO TAKE OVER MOVEMENT CONTROLS WHILE WALL JUMPING
            _wallJumpTime += timeStep;
            if (_wallJumpTime >= MoveStats.TimeTillJumpApex)
            {
                _useWallJumpMoveStats = false;
            }

            //HIT HEAD

            if (Controller.BumpedHead() && !IsHeadBumpSliding)
            {
                if (Controller.HeadBumpSlideDirection != 0 && !Controller.IsHittingCeilingCenter && !Controller.IsHittingBothCorners)
                {
                    _slideFromDash = false;
                }
                else if (MoveStats.JumpFollowSlopesWhenHeadTouching && Controller.CeilingAngle > 0f)
                {
                    Vector2 ceilingNormal = Controller.CeilingNormal;

                    Velocity = Velocity - (Vector2.Dot(Velocity, ceilingNormal) * ceilingNormal);
                }
                else
                {
                    Velocity.y = 0f;
                    _isWallJumpFastFalling = true;
                }
            }

            if (IsHeadBumpSliding)
            {
                Velocity.y = 0f;
                return;
            }

            if (!_justFinishedSlide)
            {
                //GRAVITY IN ASCENDING
                if (Velocity.y >= 0f)
                {
                    //APEX CONTROLS
                    _wallJumpApexPoint = Mathf.InverseLerp(MoveStats.WallJumpDirection.y, 0f, Velocity.y);

                    if (_wallJumpApexPoint > MoveStats.ApexThreshold)
                    {
                        if (!_isPastWallJumpApexThreshold)
                        {
                            _isPastApexThreshold = true;
                            _timePastWallJumpApexThreshold = 0f;
                        }

                        if (_isPastWallJumpApexThreshold)
                        {
                            _timePastWallJumpApexThreshold += timeStep;
                            if (_timePastWallJumpApexThreshold < MoveStats.ApexHangTime)
                            {
                                Velocity.y = 0f;
                            }
                            else
                            {
                                Velocity.y = -0.01f;
                            }
                        }
                    }

                    //GRAVITY IN ASCENDING BUT NOT PAST APEX THRESHOLD
                    else if (!_isWallJumpFastFalling)
                    {
                        Velocity.y += MoveStats.WallJumpGravity * timeStep;

                        if (_isPastWallJumpApexThreshold)
                        {
                            _isPastWallJumpApexThreshold = false;
                        }
                    }
                }

                //GRAVITY ON DESCENDING
                else if (!_isWallJumpFastFalling)
                {
                    Velocity.y += MoveStats.WallJumpGravity * timeStep;
                }

                else if (Velocity.y < 0f)
                {
                    if (!_isWallJumpFalling)
                        _isWallJumpFalling = true;
                }
            }
        }

        //HANDLE WALL JUMP CUT TIME

        if (_isWallJumpFastFalling)
        {
            if (_wallJumpFastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y += MoveStats.WallJumpGravity * MoveStats.WallJumpGravityOnReleaseMultiplier * timeStep;
            }
            else if (_wallJumpFastFallTime < MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y = Mathf.Lerp(_wallJumpFastFallReleaseSpeed, 0f, (_wallJumpFastFallTime / MoveStats.TimeForUpwardsCancel));
            }

            _wallJumpFastFallTime += timeStep;
        }
    }

    private bool ShouldApplyPostWallJumpBuffer()
    {
        bool isWallAngleValid = Controller.WallAngle >= MoveStats.MinAngleForWallSlide && Controller.WallAngle <= MoveStats.MaxAngleForWallSlide;

        if (Controller.IsTouchingWall(IsFacingRight) && isWallAngleValid || _isWallSliding)
        {
            return true;
        }
        else { return false; }
    }

    private void ResetWallJumpValues()
    {
        _isWallSlideFalling = false;
        _useWallJumpMoveStats = false;
        _isWallJumping = false;
        _isWallJumpFastFalling = false;
        _isWallJumpFalling = false;
        _isPastWallJumpApexThreshold = false;

        _wallJumpFastFallTime = 0f;
        _wallJumpTime = 0f;
    }

    #endregion

    #region Dash

    private void DashCheck()
    {
        if (_dashPressed)
        {
            _dashBufferTimer = MoveStats.DashBufferTime;
        }

        if(_dashBufferTimer > 0f)
        {
            //ground dash
            if (Controller.IsGrounded() && _dashOnGroundTimer < 0 && !IsDashing)
            {
                InitiateDash();
                _dashBufferTimer = 0f;
            }

            //air dash
            else if (!Controller.IsGrounded() && !IsDashing && _numberOfDashesUsed < MoveStats.NumberOfDashes)
            {
                _isAirDashing = true;
                InitiateDash();
                _dashBufferTimer = 0f;
            }
        }

    }

    private void InitiateDash()
    {
        _dashStartY = _rb.position.y;

        _dashDirection = _moveInput;
        TurnCheck(_dashDirection);

        Vector2 closestDirection = Vector2.zero;
        float minDistance = Vector2.Distance(_dashDirection, MoveStats.DashDirections[0]);

        for (int i = 0; i < MoveStats.DashDirections.Length; i++)
        {
            //skip if we hit it bang on
            if (_dashDirection == MoveStats.DashDirections[i])
            {
                closestDirection = _dashDirection;
                break;
            }

            float distance = Vector2.Distance(_dashDirection, MoveStats.DashDirections[i]);

            //check if this is a diagonal direction and apply bias
            bool isDiagonal = (Mathf.Abs(MoveStats.DashDirections[i].x) == 1 && Mathf.Abs(MoveStats.DashDirections[i].y) == 1);
            if (isDiagonal)
            {
                distance -= MoveStats.DashDiagonallyBias;
            }

            else if (distance < minDistance)
            {
                minDistance = distance;
                closestDirection = MoveStats.DashDirections[i];
            }
        }

        //handle direction with NO input
        if (closestDirection == Vector2.zero)
        {
            if (IsFacingRight)
            {
                closestDirection = Vector2.right;
            }
            else { closestDirection = Vector2.left; }
        }

        if (Controller.IsGrounded() && closestDirection.y < 0 && closestDirection.x !=0)
        {
            closestDirection = new Vector2(Mathf.Sign(closestDirection.x), 0);
        }

        _dashDirection = closestDirection;
        _numberOfDashesUsed++;
        IsDashing = true;
        _dashTimer = 0f;
        _dashOnGroundTimer = MoveStats.TimeBtwDashesOnGround;

        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSlide();

        if (_dashDirection.y > 0f)
        {
            _didHeadBumpSlideThisAirborneState = false;
        }

        _isPerformingSlopeDash = Controller.IsGrounded() && Controller.SlopeAngle > 0 && _dashDirection.y == 0 && !_isJumping && Mathf.Sign(_dashDirection.x) != Mathf.Sign(Controller.SlopeNormal.x);
        if (_isPerformingSlopeDash)
        {
            _slopeDashAngle = Controller.SlopeAngle;
        }


    }

    private void Dash(float timeStep)
    {
        if (_justFinishedSlide) return;

        if (IsDashing)
        {
            if (Controller.BumpedHead() && !IsHeadBumpSliding)
            {
                if (Controller.HeadBumpSlideDirection != 0 && !Controller.IsHittingCeilingCenter && !Controller.IsHittingBothCorners)
                {
                    _slideFromDash = true;
                    _dashTimer = 0f;
                }
                else if (MoveStats.DashFollowSlopesWhenHeadTouching && Controller.CeilingAngle > 0f)
                {
                    Vector2 ceilingNormal = Controller.CeilingNormal;

                    Velocity = Velocity - (Vector2.Dot(Velocity, ceilingNormal) * ceilingNormal);
                }
                else
                {
                    Velocity.y = 0;
                    IsDashing = false;
                    _isAirDashing = false;
                    _dashTimer = 0f;
                }
            }

            if (IsHeadBumpSliding)
            {
                Velocity.y = 0f;
                return;
            }

            //stop the dash after the timer
            _dashTimer += timeStep;
            if (_dashTimer >= MoveStats.DashTime)
            {
                if (Controller.IsGrounded())
                {
                    ResetDashes();
                }

                _isAirDashing = false;
                IsDashing = false;

                if (!_isJumping && !_isWallJumping)
                {
                    _dashFastFallTime = 0f;
                    _dashFastFallReleaseSpeed = Velocity.y;

                    if (!Controller.IsGrounded())
                    {
                        _isDashFastFalling = true;
                    }
                    else
                    {
                        Velocity.y = 0f;
                    }
                }

                return;
            }

            if (MoveStats.DashDirectionMatchesSlopeDirection && _isPerformingSlopeDash)
            {
                Velocity.x = Mathf.Cos(_slopeDashAngle * Mathf.Deg2Rad) * MoveStats.DashSpeed * _dashDirection.x;
                Velocity.y = Mathf.Sin(_slopeDashAngle * Mathf.Deg2Rad) * MoveStats.DashSpeed;
            }
            else 
            { 
                Velocity.x = MoveStats.DashSpeed * _dashDirection.x;

                if (_dashDirection.y != 0f || _isAirDashing)
                {
                    Velocity.y = MoveStats.DashSpeed * _dashDirection.y;
                }
                else if (!_isJumping && _dashDirection.y == 0f)
                {
                    Velocity.y = -0.001f;
                }
            }
        }

        //HANDLE DASH CUT TIME
        else if (_isDashFastFalling)
        {
            if (Velocity.y > 0f)
            {
                if (_dashFastFallTime < MoveStats.DashTimeForUpwardsCancel)
                {
                    Velocity.y = Mathf.Lerp(_dashFastFallReleaseSpeed, 0f, (_dashFastFallTime / MoveStats.DashTimeForUpwardsCancel));
                }
                else if (_dashFastFallTime >= MoveStats.DashTimeForUpwardsCancel)
                {
                    Velocity.y += MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * timeStep;
                }

                _dashFastFallTime += timeStep;
            }
            else
            {
                Velocity.y += MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * timeStep;
            }
        }
    }

    private void ResetDashValues()
    {
        _isDashFastFalling = false;
        _dashOnGroundTimer = -0.01f;

        _dashFastFallReleaseSpeed = 0f;
        _dashFastFallTime = 0f;
        _dashDirection = Vector2.zero;
        _isPerformingSlopeDash = false;
    }

    private void ResetDashes()
    {
        _numberOfDashesUsed = 0;
    }

    #endregion

    #region Slide

    private void HandleSlide(float timeStep)
    {
        if (Controller.IsSliding)
        {
            if (_isJumping) return;
            if (_isWallJumping) return;
        
            Velocity.y += MoveStats.Gravity * timeStep;
        }
    }

    #endregion

    #region Timers

    private void CountTimers(float timeStep)
    {
        //jump buffer
        _jumpBufferTimer -= timeStep;

        //jump coyote time
        HandleCoyoteTimer(timeStep);

        //wall jump buffer timer
        _wallJumpPostBufferTimer -= timeStep;

        //dash timer
        HandleDashOnGroundTimer(timeStep);

        //dash buffer timer
        _dashBufferTimer -= timeStep;
    }

    private void HandleCoyoteTimer(float timeStep)
    {
        if (Controller.IsGrounded() && !Controller.IsSliding && !IsSlideableSlope(Controller.SlopeAngle))
        {
            _coyoteTimer = MoveStats.JumpCoyoteTime;
        }
        else
        {
            _coyoteTimer -= timeStep;
        }

    }

    private void HandleDashOnGroundTimer(float timeStep)
    {

        if (Controller.IsGrounded() && !Controller.IsSliding && !IsSlideableSlope(Controller.SlopeAngle))
        {
            _dashOnGroundTimer -= timeStep;
        }

    }




    #endregion

    #region Helper Methods

    private bool IsSlideableSlope(float slopeAngle)
    {
        if (slopeAngle >= MoveStats.MaxSlopeAngle && slopeAngle < MoveStats.MinAngleForWallSlide)
        {
            return true;
        }

        return false;
    }

    #endregion
}
