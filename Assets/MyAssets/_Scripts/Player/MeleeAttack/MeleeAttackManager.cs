using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttackManager : MonoBehaviour
{
    public float DefaultForce = 10f;
    private bool _meleeAttack;
    private PlayerMovement _player;
    private Animator _meleeAnimator;
    private Animator _anim;

    private void Start()
    {
        _player = GetComponent<PlayerMovement>();
        _anim = GetComponent<Animator>();
        _meleeAnimator = GetComponentInChildren<MeleeWeapon>().gameObject.GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        CheckInput();
    }
    
    private void CheckInput()
    {
        if (InputManager.PrimaryAttackWasClicked) { _meleeAttack = true; }
        else { _meleeAttack = false; }

        float verticalInput = InputManager.Movement.y;

        // Upward melee
        if (_meleeAttack && verticalInput > 0)
        {
            _anim.SetTrigger("UpwardMelee");
            _meleeAnimator.SetTrigger("UpwardMeleeSwipe");
        }
        // Downward melee (airborne only)
        else if (_meleeAttack && verticalInput < 0 && !_player.Controller.IsGrounded())
        {
            _anim.SetTrigger("DownwardMelee");
            _meleeAnimator.SetTrigger("DownwardMeleeSwipe");
        }
        // Backward melee (attack opposite to facing direction)
        else if (_meleeAttack && !_player.IsFacingRight)
        {
            _anim.SetTrigger("BackwardMelee");
            _meleeAnimator.SetTrigger("BackwardMeleeSwipe");
        }
        // Forward melee (default)
        else if (_meleeAttack)
        {
            _anim.SetTrigger("ForwardMelee");
            _meleeAnimator.SetTrigger("ForwardMeleeSwipe");
        }
    }
}
