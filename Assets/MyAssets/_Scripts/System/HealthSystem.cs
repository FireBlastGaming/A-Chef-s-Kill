using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    // Initialize all variables
    [SerializeField]
    private bool _isDamageable = true;
    public int HealthPoints = 100;
    [SerializeField]
    private float _invulnerabilityTime = .1f;
    [SerializeField]
    private float _hitScale = 0.05f;
    public bool GiveUpwardForce = true;
    private bool _isHit;
    [HideInInspector]public int CurrentHealth;

    // Shader vars
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private Material _originalMaterial;
    private Material _flashMaterial;
    [SerializeField] private float _playerDarkenLerpTime = 0.2f;
    private Coroutine _playerDarkenRoutine;

    private void Start()
    {
        //sets the enemy to the max amount of health when the scene loads
        CurrentHealth = HealthPoints; 
        
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColor = _spriteRenderer.color;
        _originalMaterial = _spriteRenderer.material;
        
        // Create a material instance with white flash shader
        Shader flashShader = Shader.Find("Sprites/WhiteFlash");
        if (flashShader != null)
        {
            _flashMaterial = new Material(flashShader);
            // Copy texture from original material
            if (_originalMaterial.HasProperty("_MainTex"))
            {
                _flashMaterial.SetTexture("_MainTex", _originalMaterial.GetTexture("_MainTex"));
            }
            _flashMaterial.SetColor("_Color", _originalColor);
            _flashMaterial.SetFloat("_FlashAmount", 0f);
        }
    }

    public void Damage(int amount)
    {
        // checks to see if the player is currently in an invulnerable state; if not it runs the following logic.
        if (_isDamageable && !_isHit && CurrentHealth > 0)
        {
            _isHit = true;
            CurrentHealth -= amount;

            //flashes white when hit
            if (_flashMaterial != null)
            {
                _spriteRenderer.material = _flashMaterial;
                _flashMaterial.SetFloat("_FlashAmount", 1f);
            }


            //if CurrentHealth is below zero, player is dead, and then we handle all the logic to manage the dead state
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;

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
        float elapsed = 0f;
        while (elapsed < _invulnerabilityTime)
        {
            elapsed += Time.deltaTime;
            float flashAmount = Mathf.Lerp(1f, 0f, elapsed / _invulnerabilityTime);
            if (_flashMaterial != null)
            {
                _flashMaterial.SetFloat("_FlashAmount", flashAmount);
            }
            yield return null;
        }
        
        // Restore original material
        _spriteRenderer.material = _originalMaterial;

        if (CompareTag("Player"))
        {
            ApplyPlayerDamageDarkening();
        }
        
        _isHit = false;
    }

    private void ApplyPlayerDamageDarkening()
    {
        float missingHealthPercent = 1f - ((float)CurrentHealth / HealthPoints);
        missingHealthPercent = Mathf.Clamp01(missingHealthPercent);

        Color targetColor = Color.Lerp(_originalColor, Color.black, missingHealthPercent);

        if (_playerDarkenRoutine != null)
        {
            StopCoroutine(_playerDarkenRoutine);
        }

        _playerDarkenRoutine = StartCoroutine(LerpSpriteColor(targetColor, _playerDarkenLerpTime));
    }

    private IEnumerator LerpSpriteColor(Color targetColor, float duration)
    {
        Color startColor = _spriteRenderer.color;

        if (duration <= 0f)
        {
            _spriteRenderer.color = targetColor;
            _playerDarkenRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        _spriteRenderer.color = targetColor;
        _playerDarkenRoutine = null;
    }

    // Coroutine that disables the enemy after the white flash
    private IEnumerator DisableOnDeath()
    {
        yield return new WaitForSeconds(_invulnerabilityTime);
        gameObject.SetActive(false);
    }
}
