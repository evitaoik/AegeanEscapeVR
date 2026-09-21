using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Vector3 openOffset = new Vector3(0f, 2.2f, 0f);
    public float openSpeed = 2f;
    public bool isOpen = false;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private AudioSource doorAudio;

    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;

        doorAudio = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isOpen)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                openPosition,
                openSpeed * Time.deltaTime
            );
        }
    }

    public void OpenDoor()
    {
        if (isOpen)
            return;

        isOpen = true;

        if (doorAudio != null)
            doorAudio.Play();
    }
}