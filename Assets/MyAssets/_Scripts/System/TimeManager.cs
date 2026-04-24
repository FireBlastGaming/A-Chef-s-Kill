using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Singleton instance
    public static TimeManager Instance { get; private set; }

    private bool _waiting;
    private Coroutine _waitCoroutine;

    private void Awake()
    {
        // Set the static reference when this object is created
        Instance = this;
    }

    public void AdjustGlobalTime(float timeScale, float duration, bool shouldOverrideCurrentWait = false) {
        if (_waiting)
        {
            if (!shouldOverrideCurrentWait)
            {
                return;
            }

            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }

            _waiting = false;
        }

        Time.timeScale = timeScale;

        _waiting = true;
        _waitCoroutine = StartCoroutine(Wait(duration));
    }

    IEnumerator Wait(float duration) {
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        _waiting = false;
        _waitCoroutine = null;
    }

}
