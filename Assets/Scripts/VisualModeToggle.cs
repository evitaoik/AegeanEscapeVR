using UnityEngine;
using UnityEngine.InputSystem;

public class VisualModeToggle : MonoBehaviour
{
    public GameObject handsVisual;

    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;

    private bool showHands = true;

    void Start()
    {
        UpdateVisuals();
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.vKey.wasPressedThisFrame)
        {
            showHands = !showHands;
            UpdateVisuals();
        }
    }

    void UpdateVisuals()
    {
        handsVisual.SetActive(showHands);

        leftControllerVisual.SetActive(!showHands);
        rightControllerVisual.SetActive(!showHands);
    }
}