using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Singleton instance
    public static TimeManager Instance { get; private set; }
    
    private bool _waiting;

    private void Awake()
    {
        // Set the static reference when this object is created
        Instance = this;
    }

    public void AdjustGlobalTime(float timeScale, float duration) {
        if (_waiting) {
            return;
        }
        Time.timeScale = timeScale;
        StartCoroutine(Wait(duration));
    }

    IEnumerator Wait(float duration) {
        _waiting = true;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        _waiting = false;
    }

}
