using UnityEngine;
using UnityEngine.InputSystem;

public class VRHandsFollow : MonoBehaviour
{
    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("Hand Bones")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Position Offset")]
    public Vector3 leftPositionOffset;
    public Vector3 rightPositionOffset;

    [Header("Rotation Offset")]
    public Vector3 leftRotationOffset;
    public Vector3 rightRotationOffset;

    private Quaternion leftBindRotation;
    private Quaternion rightBindRotation;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (leftController != null && leftHand != null)
        {
            leftBindRotation =
                Quaternion.Inverse(leftController.rotation) *
                leftHand.rotation;
        }

        if (rightController != null && rightHand != null)
        {
            rightBindRotation =
                Quaternion.Inverse(rightController.rotation) *
                rightHand.rotation;
        }
    }

    void Update()
    {
        if (Keyboard.current == null || animator == null)
            return;

        bool grabbing = Keyboard.current.gKey.isPressed;
        bool pointing = Keyboard.current.pKey.isPressed;

        // Grab έχει προτεραιότητα από Point
        animator.SetBool("IsGrab", grabbing);
        animator.SetBool("IsPoint", pointing && !grabbing);
    }

    void LateUpdate()
    {
        if (leftController != null && leftHand != null)
        {
            leftHand.position =
                leftController.TransformPoint(leftPositionOffset);

            leftHand.rotation =
                leftController.rotation *
                leftBindRotation *
                Quaternion.Euler(leftRotationOffset);
        }

        if (rightController != null && rightHand != null)
        {
            rightHand.position =
                rightController.TransformPoint(rightPositionOffset);

            rightHand.rotation =
                rightController.rotation *
                rightBindRotation *
                Quaternion.Euler(rightRotationOffset);
        }
    }
}