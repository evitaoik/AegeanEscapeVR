using UnityEngine;

// Keeps a World Space canvas in front of the player's head.
// It only moves when the panel drifts out of view, so it stays still while you read or point at it.
// Uses unscaled time so it also works while the game is paused.
public class VRPanelFollow : MonoBehaviour
{
    public Transform head;

    [Header("Placement")]
    public float distance = 1.25f;
    public float heightOffset = -0.1f;
    [Tooltip("Follow the head pitch too (for small HUDs). If off, the panel stays upright.")]
    public bool followPitch = false;
    [Tooltip("Pull the panel closer if a wall is between the player and the panel.")]
    public bool avoidWalls = true;
    public float minDistance = 0.6f;

    [Header("Lazy follow")]
    public float angleThreshold = 35f;
    public float followSpeed = 4f;

    private bool moving;

    private void OnEnable()
    {
        if (head == null && Camera.main != null)
            head = Camera.main.transform;

        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (head == null)
            return;

        GetTarget(out Vector3 targetPosition, out Quaternion targetRotation);

        Vector3 toPanel = transform.position - head.position;
        Vector3 forward = targetRotation * Vector3.forward;
        float angle = Vector3.Angle(followPitch ? toPanel : Vector3.ProjectOnPlane(toPanel, Vector3.up), forward);
        float distanceError = Mathf.Abs(toPanel.magnitude - Vector3.Distance(head.position, targetPosition));

        if (angle > angleThreshold || distanceError > 0.5f)
            moving = true;

        if (!moving)
            return;

        float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            moving = false;
    }

    // Puts the panel straight in front of the player (used when a menu opens)
    public void SnapToTarget()
    {
        if (head == null)
            return;

        GetTarget(out Vector3 targetPosition, out Quaternion targetRotation);
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        moving = false;
    }

    private void GetTarget(out Vector3 position, out Quaternion rotation)
    {
        Vector3 forward = head.forward;

        if (!followPitch)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
        }

        forward.Normalize();

        float d = distance;

        if (avoidWalls && Physics.Raycast(head.position, forward, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            d = Mathf.Max(minDistance, hit.distance - 0.1f);

        Vector3 up = followPitch ? head.up : Vector3.up;
        position = head.position + forward * d + up * heightOffset;
        rotation = Quaternion.LookRotation(forward, up);
    }
}
