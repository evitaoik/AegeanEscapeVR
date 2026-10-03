using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public TMP_Text timerText;

    private float elapsedTime = 0f;
    private bool timerRunning = false;

    public float ElapsedTime => elapsedTime;
    public bool IsRunning => timerRunning;
    public string FormattedTime => Format(elapsedTime);

    void Update()
    {
        // Time.deltaTime is 0 while the game is paused, so the timer stops too
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

    public static string Format(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        // Fixed-width digits so the text does not jump every second
        timerText.text = "<mspace=0.56em>" + FormattedTime + "</mspace>";
    }
}
