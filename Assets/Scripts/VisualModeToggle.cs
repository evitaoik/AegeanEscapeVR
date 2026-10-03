using UnityEngine;
using UnityEngine.InputSystem;

public class VisualModeToggle : MonoBehaviour
{
    public GameObject handsVisual;

    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;

    // Optional: keeps the Settings menu in sync when V is pressed
    public GameSettings settings;

    private bool showHands = true;

    public bool ShowHands => showHands;

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

            if (settings != null)
                settings.SyncShowHands(showHands);
        }
    }

    public void SetShowHands(bool value)
    {
        showHands = value;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (handsVisual != null)
            handsVisual.SetActive(showHands);

        if (leftControllerVisual != null)
            leftControllerVisual.SetActive(!showHands);

        if (rightControllerVisual != null)
            rightControllerVisual.SetActive(!showHands);
    }
}
