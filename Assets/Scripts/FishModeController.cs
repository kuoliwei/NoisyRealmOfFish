using UnityEngine;
using System;

public class FishModeController : MonoBehaviour
{
    private float timer;
    private bool running;
    private Action onTimeout;

    public void StartTimer(float duration, Action onTimeoutCallback)
    {
        timer = duration;
        running = true;
        onTimeout = onTimeoutCallback;
    }

    public void StopTimer()
    {
        running = false;
    }

    public void ResetTimer()
    {
        timer = 0f;
        running = false;
    }

    private void Update()
    {
        if (!running) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            running = false;
            onTimeout?.Invoke();
        }
    }
}
