using System.Collections;
using UnityEngine;

public class Room2PuzzleManager : MonoBehaviour
{
    [Header("Puzzle")]
    public DoorController finalDoor;
    public int totalObjects = 3;

    [Header("Timer")]
    public GameTimer gameTimer;

    [Header("Sounds")]
    public AudioClip victoryClip;
    public AudioClip seaClip;

    [Header("End")]
    public GameObject endCanvas;

    private int placedObjects = 0;
    private bool solved = false;

    private AudioSource victorySource;
    private AudioSource seaSource;

    private void Start()
    {
        victorySource = gameObject.AddComponent<AudioSource>();
        seaSource = gameObject.AddComponent<AudioSource>();

        victorySource.playOnAwake = false;

        seaSource.playOnAwake = false;
        seaSource.loop = true;
        seaSource.volume = 0.25f;

        if (endCanvas != null)
            endCanvas.SetActive(false);
    }

    public void ObjectPlaced()
    {
        if (solved)
            return;

        placedObjects++;

        Debug.Log("Objects placed: " + placedObjects + "/" + totalObjects);

        if (placedObjects >= totalObjects)
        {
            solved = true;

            // STOP TIMER
            if (gameTimer != null)
                gameTimer.StopTimer();

            // VICTORY SOUND
            if (victoryClip != null)
            {
                victorySource.clip = victoryClip;
                victorySource.Play();
            }

            // OPEN FINAL DOOR
            if (finalDoor != null)
                finalDoor.OpenDoor();

            // SEA SOUND
            if (seaClip != null)
            {
                seaSource.clip = seaClip;
                seaSource.Play();
            }

            // SHOW END SCREEN AFTER DELAY
            StartCoroutine(ShowEndAfterDelay());
        }
    }

    private IEnumerator ShowEndAfterDelay()
    {
        yield return new WaitForSeconds(20f);

        if (endCanvas != null)
            endCanvas.SetActive(true);

        Time.timeScale = 0f;
    }
}