using System.Collections;
using UnityEngine;

public class CameraFollowObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerTransform;

    [Header("Flip Rotation Stats")]
    [SerializeField] private float _flipYRotationTime = 0.5f;


    private Coroutine _turnCoroutine;

    private PlayerMovement _player;

    private MovementController Controller;

    private bool _isFacingRight;

    private void Awake()
    {
        _player = _playerTransform.gameObject.GetComponent<PlayerMovement>();

        _isFacingRight = _player.IsFacingRight;
    }

    private void Update()
    {
        //make the cameraFollowObject follow the player's position
        transform.position = _playerTransform.position;
    }

    public void CallTurn()
    {
        Invoke("rotateSpriteUsingXScale", _flipYRotationTime-0.4f);
        _turnCoroutine = StartCoroutine(FlipYLerp());
    }

    private void rotateSpriteUsingXScale()
    {
        Vector3 scale = new Vector3(-_playerTransform.localScale.x, _playerTransform.localScale.y, _playerTransform.rotation.z);
        _playerTransform.localScale = scale;
    }

    private IEnumerator FlipYLerp()
    {
        float startRotation = transform.localEulerAngles.y;
        float endRotationAmount = DetermineEndRotation();
        float yRotation = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < _flipYRotationTime)
        {
            elapsedTime += Time.deltaTime;

            //lerp the y rotation
            yRotation = Mathf.Lerp(startRotation, endRotationAmount, (elapsedTime / _flipYRotationTime));
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            yield return null;
        }
    }

    private float DetermineEndRotation()
    {
        _isFacingRight = !_isFacingRight;

        if (_isFacingRight)
        {
            return 0f;
        }
        else
        {
            return 180f;
        }
    }
}
