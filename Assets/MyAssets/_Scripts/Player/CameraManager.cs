using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField] private CinemachineCamera[] _allCameras;

    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallYPanTime = 0.35f;
    public float _fallSpeedYDampingChangeThreshold = -15f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private Coroutine _lerpYPanCoroutine;

    private CinemachineCamera _currentCamera;
    private CinemachinePositionComposer _positionComposer;

    private float _normYPanAmount;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i = 0; i < _allCameras.Length; i++)
        {
            if (_allCameras[i].enabled)
            {
                //set the current active camera
                _currentCamera = _allCameras[i];

                var body = _currentCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);

                // Safe cast: null if it's not a PositionComposer
                _positionComposer = body as CinemachinePositionComposer;



            }
        }

        //set the YDamping amount so it's based on the inspector value
        _normYPanAmount = _positionComposer.Damping.y;
    }

    #region Lerp the Y Damping

    public void LerpYDamping(bool isPlayerFalling)
    {

        _lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }

    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;

        //grab the starting damping amount
        float startDampAmount = _positionComposer.Damping.y;
        float endDampAmount = 0f;

        //determine the end damping amount
        if (isPlayerFalling)
        {
            endDampAmount = _fallPanAmount;
            LerpedFromPlayerFalling = true;
        }
        else
        {
            endDampAmount = _normYPanAmount;
        }

        //lerp the pan amount
        float elapsedTime = 0f;
        while (elapsedTime < _fallYPanTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, ( elapsedTime / _fallYPanTime));
            _positionComposer.Damping.y = lerpedPanAmount;

            yield return null;
        }
            
        IsLerpingYDamping = false;

    }

    #endregion
}
