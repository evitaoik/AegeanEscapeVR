using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsMenu : MonoBehaviour
{
    public GameObject controlsPanel;

    private void Start()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // Με C ανοίγει / κλείνει το panel
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleControls();
        }
    }

    public void ToggleControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(!controlsPanel.activeSelf);
    }

    public void CloseControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }
}