using UnityEngine;

public class PuzzleSlot : MonoBehaviour
{
    public string requiredTag;
    public Room2PuzzleManager puzzleManager;

    [Header("Snap")]
    public Transform snapPoint;

    private bool completed = false;
    private AudioSource correctSound;

    private void Start()
    {
        correctSound = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed)
            return;

        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        if (!rb.CompareTag(requiredTag))
            return;

        completed = true;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab =
            rb.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab != null)
        {
            grab.enabled = false;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (snapPoint != null)
        {
            rb.transform.position = snapPoint.position;
            rb.transform.rotation = snapPoint.rotation;
        }

        // Παίζει ο ήχος όταν το σωστό αντικείμενο μπει στο slot
        if (correctSound != null)
        {
            correctSound.Play();
        }

        Debug.Log(requiredTag + " placed correctly!");

        if (puzzleManager != null)
        {
            puzzleManager.ObjectPlaced();
        }
    }
}