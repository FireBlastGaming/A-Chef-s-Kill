using UnityEngine;

public class EnemyAi : MonoBehaviour
{
    [Header("References")]
    public MovementStats MoveStats;
    [SerializeField] private Collider2D _coll;
    [SerializeField] private Transform _target;
    [SerializeField] private float _targetDetectionRange = 6f;

    [Header("Melee Attack")]
    [SerializeField] private bool _useMeleeAttack = true;
    [SerializeField] private float _meleeAttackRange = 1.25f;
    [SerializeField] private float _meleeVerticalRange = 1.1f;
    [SerializeField] private float _meleeAttackCooldown = 1.1f;
    [SerializeField] private float _meleeAttackWindup = 0.15f;
    [SerializeField] private float _meleeAttackRecovery = 0.2f;
    [SerializeField] private Vector2 _meleeAttackGateOffset = Vector2.zero;
    [SerializeField] private int _meleeDamage = 15;
    [SerializeField] private Vector2 _meleeHitboxOffset = new Vector2(0.8f, 0f);
    [SerializeField] private float _meleeHitboxRadius = 0.55f;
    [SerializeField] private LayerMask _meleeTargetLayers;
    [SerializeField] private string _meleeAttackTrigger = "MeleeAttack";

    private Rigidbody2D _rb;
    private Animator _anim;


    //movement vars
    public bool IsFacingRight { get; private set; }
    public EnemyMovementController Controller { get; private set; }
    [HideInInspector] public Vector2 Velocity;

    private Vector2 _moveDirection;

    private bool _isFalling;
    private bool _isPerformingMeleeAttack;
    private bool _hasAppliedMeleeDamage;
    private float _meleeCooldownTimer;
    private float _meleeWindupTimer;

    private void Awake()
    {
        IsFacingRight = true;
        _moveDirection = Vector2.right;

        _rb = GetComponent<Rigidbody2D>();
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _anim = GetComponent<Animator>();

        Controller = GetComponent<EnemyMovementController>();
    }
    private void FixedUpdate()
    {

        LandCheck();

        UpdateAIDirection();
        HandleMeleeAttack(Time.fixedDeltaTime);

        HandleHorizontalMovement(Time.fixedDeltaTime);
        Fall(Time.fixedDeltaTime);

        ClampVelocity();

        Controller.Move(Velocity * Time.fixedDeltaTime);
    }

    private void ClampVelocity()
    {
        //CLAMP FALL SPEED
        Velocity.y = Mathf.Clamp(Velocity.y, -MoveStats.MaxFallSpeed, 50f);
    }

    #region Movement
    private void HandleHorizontalMovement(float timeStep)
    {
        if (_isPerformingMeleeAttack)
        {
            float attackDeceleration = Controller.IsGrounded() ? MoveStats.GroundDeceleration : MoveStats.AirDeceleration;
            Velocity.x = Mathf.Lerp(Velocity.x, 0f, attackDeceleration * timeStep);
            return;
        }

        float horizontalInput = _moveDirection.x;
        bool hasHorizontalInput = !Mathf.Approximately(horizontalInput, 0f);

        TurnCheck(_moveDirection);
        float targetVelocityX = 0f;

        if (hasHorizontalInput)
        {
            float moveDirection = Mathf.Sign(_moveDirection.x);
            targetVelocityX = moveDirection * MoveStats.MaxWalkSpeed;
        }

        float acceleration = Controller.IsGrounded() ? MoveStats.GroundAcceleration : MoveStats.AirAcceleration;
        float deceleration = Controller.IsGrounded() ? MoveStats.GroundDeceleration : MoveStats.AirDeceleration;


        if (hasHorizontalInput)
        {
            Velocity.x = Mathf.Lerp(Velocity.x, targetVelocityX, acceleration * timeStep);
        }

        else
        {
            Velocity.x = Mathf.Lerp(Velocity.x, 0, deceleration * timeStep);
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
        IsFacingRight = turnRight;

        Vector3 localScale = transform.localScale;
        float xScaleMagnitude = Mathf.Abs(localScale.x);
        localScale.x = IsFacingRight ? xScaleMagnitude : -xScaleMagnitude;
        transform.localScale = localScale;
    }

    private void UpdateAIDirection()
    {
        Vector2 desiredDirection = IsFacingRight ? Vector2.right : Vector2.left;
        bool isChasingTarget = false;

        if (_target != null)
        {
            float deltaX = _target.position.x - transform.position.x;
            float distanceToTarget = Vector2.Distance(transform.position, _target.position);
            bool targetInRange = distanceToTarget <= _targetDetectionRange;

            if (targetInRange && !Mathf.Approximately(deltaX, 0f))
            {
                desiredDirection = new Vector2(Mathf.Sign(deltaX), 0f);
                isChasingTarget = true;
            }
        }

        bool movingRight = desiredDirection.x > 0f;
        bool blockedByWall = Controller.IsTouchingWall(movingRight);
        bool blockedByLedge = Controller.IsGrounded() && !Controller.HasGroundAhead(movingRight);

        if (isChasingTarget && (blockedByWall || blockedByLedge))
        {
            // Stay bounded to current platform while target is across a ledge.
            desiredDirection = Vector2.zero;
        }
        else if (!isChasingTarget && (blockedByWall || blockedByLedge))
        {
            desiredDirection *= -1f;
        }

        _moveDirection = desiredDirection;
    }

    private void HandleMeleeAttack(float timeStep)
    {
        if (!_useMeleeAttack)
        {
            return;
        }

        if (_meleeCooldownTimer > 0f)
        {
            _meleeCooldownTimer -= timeStep;
        }

        if (_isPerformingMeleeAttack)
        {
            _meleeWindupTimer -= timeStep;

            if (!_hasAppliedMeleeDamage && _meleeWindupTimer <= 0f)
            {
                PerformMeleeHit();
                _hasAppliedMeleeDamage = true;
            }

            if (_meleeWindupTimer <= -_meleeAttackRecovery)
            {
                _isPerformingMeleeAttack = false;
                _hasAppliedMeleeDamage = false;
            }

            return;
        }

        if (CanStartMeleeAttack())
        {
            StartMeleeAttack();
        }
    }

    private bool CanStartMeleeAttack()
    {
        if (_target == null || _meleeCooldownTimer > 0f)
        {
            return false;
        }

        Vector2 meleeGateCenter = transform.TransformPoint(_meleeAttackGateOffset);
        float horizontalDistance = Mathf.Abs(_target.position.x - meleeGateCenter.x);
        float verticalDistance = Mathf.Abs(_target.position.y - meleeGateCenter.y);

        return horizontalDistance <= _meleeAttackRange && verticalDistance <= _meleeVerticalRange;
    }

    private void StartMeleeAttack()
    {
        _isPerformingMeleeAttack = true;
        _hasAppliedMeleeDamage = false;
        _meleeWindupTimer = _meleeAttackWindup;
        _meleeCooldownTimer = _meleeAttackCooldown;

        if (_anim != null && !string.IsNullOrEmpty(_meleeAttackTrigger))
        {
            _anim.SetTrigger(_meleeAttackTrigger);
        }
    }

    private void PerformMeleeHit()
    {
        Vector2 hitboxCenter = transform.TransformPoint(_meleeHitboxOffset);

        Collider2D[] hits = _meleeTargetLayers.value == 0
            ? Physics2D.OverlapCircleAll(hitboxCenter, _meleeHitboxRadius)
            : Physics2D.OverlapCircleAll(hitboxCenter, _meleeHitboxRadius, _meleeTargetLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            HealthSystem targetHealth = hits[i].GetComponent<HealthSystem>();
            if (targetHealth == null)
            {
                targetHealth = hits[i].GetComponentInParent<HealthSystem>();
            }

            if (targetHealth == null || targetHealth.gameObject == gameObject)
            {
                continue;
            }

            targetHealth.Damage(_meleeDamage);
            return;
        }
    }

    #endregion

    #region Land/Fall
    private void LandCheck()
    {
        if (Controller.IsGrounded())
        {
            //LANDED
            if (_isFalling && Velocity.y <= 0f)
            {
                _isFalling = false;
            }

            if (Velocity.y <= 0f)
            {
                Velocity.y = -2f;
            }
        }
    }
    private void Fall(float timeStep)
    {
        //NORMAL GRAVITY WHILE FALLING
        if (!Controller.IsGrounded())
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }
            Velocity.y += MoveStats.Gravity * timeStep;
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _targetDetectionRange);

        if (_useMeleeAttack)
        {
            // Melee start window (uses horizontal + vertical distance checks).
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Vector3 meleeGateCenter = transform.TransformPoint(_meleeAttackGateOffset);
            Vector3 meleeGateSize = new Vector3(_meleeAttackRange * 2f, _meleeVerticalRange * 2f, 0f);
            Gizmos.DrawWireCube(meleeGateCenter, meleeGateSize);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(_meleeHitboxOffset), _meleeHitboxRadius);
        }
    }
}
