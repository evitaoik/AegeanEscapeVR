using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public TMP_Text timerText;

    private float elapsedTime = 0f;
    private bool timerRunning = false;

    void Update()
    {
        if (!timerRunning)
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    public void StartTimer()
    {
        elapsedTime = 0f;
        timerRunning = true;
        UpdateTimerText();
    }

    public void StopTimer()
    {
        timerRunning = false;
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        timerText.text = "TIME " + minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}