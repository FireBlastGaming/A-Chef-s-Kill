using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // Initialize all variables
    [SerializeField]
    private bool _isDamageable = true;
    [SerializeField]
    private int _healthPoints = 100;
    [SerializeField]
    private float _invulnerabilityTime = .1f;
    [SerializeField]
    private float _hitScale = 0.05f;
    public bool GiveUpwardForce = true;
    private bool _isHit;
    private int _currentHealth;

    // Shader vars
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;

    private void Start()
    {
        //sets the enemy to the max amount of health when the scene loads
        _currentHealth = _healthPoints; 
        
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Store the original color
        _originalColor = _spriteRenderer.color;
    }

    public void Damage(int amount)
    {
        // checks to see if the player is currently in an invulnerable state; if not it runs the following logic.
        if (_isDamageable && !_isHit && _currentHealth > 0)
        {
            _isHit = true;
            _currentHealth -= amount;

            //flashes white when hit
            _spriteRenderer.color = Color.white;


            //if _currentHealth is below zero, player is dead, and then we handle all the logic to manage the dead state
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;

                //hit stop when dead
                TimeManager.Instance.AdjustGlobalTime(_hitScale/2, _invulnerabilityTime/2);

                //removes GameObject from the scene; this should probably play a dying animation in a method that would handle all the other death logic, but for the test it just disables it from the scene
                StartCoroutine(DisableOnDeath());
            }
            else
            {
                //hit stop
                TimeManager.Instance.AdjustGlobalTime(_hitScale, _invulnerabilityTime);

                StartCoroutine(TurnOffHit());
            }
        }
    }

    // Coroutine that runs to allow the enemy to receive damage again
    private IEnumerator TurnOffHit()
    {
        yield return new WaitForSeconds(_invulnerabilityTime);
        
        // Reset color back to original
        _spriteRenderer.color = _originalColor;
        
        _isHit = false;
    }

    // Coroutine that disables the enemy after the white flash
    private IEnumerator DisableOnDeath()
    {
        yield return new WaitForSeconds(_invulnerabilityTime);
        gameObject.SetActive(false);
    }
}
