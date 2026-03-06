using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [SerializeField]
    private int _damage = 20;
    private PlayerMovement _player;
    private MeleeAttackManager _meleeAttackManager;

    private void Start()
    {
        _player = GetComponentInParent<PlayerMovement>();
        _meleeAttackManager = GetComponentInParent<MeleeAttackManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            HandleCollision(enemyHealth);
        }
    }

    private void HandleCollision(EnemyHealth objHealth)
    {
        float verticalInput = InputManager.Movement.y;

        //downward strike while airborne - bounce jump
        if (objHealth.GiveUpwardForce && verticalInput < 0 && !_player.Controller.IsGrounded())
        {
            _player.TriggerBounceJump(1f);
        }
        //upward strike while airborne - push down
        else if (verticalInput > 0 && !_player.Controller.IsGrounded())
        {
            _player.Velocity.y = -_meleeAttackManager.DefaultForce;
        }
        //standard horizontal strike - push backward
        else
        {
            float direction = _player.IsFacingRight ? -1f : 1f;
            _player.Velocity.x = direction * _meleeAttackManager.DefaultForce;
        }

        objHealth.Damage(_damage);
    }
}
