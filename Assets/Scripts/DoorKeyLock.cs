using UnityEngine;

public class DoorKeyLock : MonoBehaviour
{
    public DoorController door;

    private AudioSource wrongKeySound;

    private void Start()
    {
        wrongKeySound = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        // Σωστό κλειδί
        if (rb.CompareTag("Key"))
        {
            if (door != null)
                door.OpenDoor();

            return;
        }

        // Λάθος κλειδί
        if (rb.CompareTag("FakeKey"))
        {
            if (wrongKeySound != null)
                wrongKeySound.Play();
        }
    }
}