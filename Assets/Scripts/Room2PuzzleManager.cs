using System.Collections;
using UnityEngine;

public class Room2PuzzleManager : MonoBehaviour
{
    public DoorController finalDoor;
    public int totalObjects = 3;

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

        if (placedObjects >= totalObjects)
        {
            solved = true;

            // Victory sound
            if (victoryClip != null)
            {
                victorySource.clip = victoryClip;
                victorySource.Play();
            }

            // Ανοίγει η τελική πόρτα
            if (finalDoor != null)
                finalDoor.OpenDoor();

            // Ξεκινάει η θάλασσα
            if (seaClip != null)
            {
                seaSource.clip = seaClip;
                seaSource.Play();
            }

            StartCoroutine(ShowEndAfterDelay());
        }
    }

    private IEnumerator ShowEndAfterDelay()
    {
        yield return new WaitForSeconds(15f);

        if (endCanvas != null)
            endCanvas.SetActive(true);

        Time.timeScale = 0f;
    }
}