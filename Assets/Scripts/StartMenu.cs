using UnityEngine;

public class StartMenu : MonoBehaviour
{
    public GameObject startCanvas;
    public AudioClip clickClip;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void StartGame()
    {
        if (clickClip != null)
        {
            audioSource.PlayOneShot(clickClip);
        }

        Invoke(nameof(CloseMenu), 0.15f);
    }

    private void CloseMenu()
    {
        startCanvas.SetActive(false);
    }
}