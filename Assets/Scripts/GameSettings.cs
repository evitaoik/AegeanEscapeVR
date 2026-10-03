using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Keeps the player settings (volume, visuals, comfort) and saves them with PlayerPrefs.
public class GameSettings : MonoBehaviour
{
    public static readonly float[] SnapAngles = { 30f, 45f, 90f };

    [Header("References")]
    public VisualModeToggle visualModeToggle;
    public ControllerInputActionManager leftControllerInput;
    public ControllerInputActionManager rightControllerInput;
    public SnapTurnProvider snapTurnProvider;

    public event Action Changed;

    public float MasterVolume { get; private set; } = 0.8f;
    public bool ShowHands { get; private set; } = true;
    public bool SmoothMovement { get; private set; } = true;
    public bool SmoothTurning { get; private set; } = false;
    public int SnapAngleIndex { get; private set; } = 1;
    public bool ShowTimer { get; private set; } = true;

    public float SnapAngle => SnapAngles[SnapAngleIndex];

    private void Awake()
    {
        Load();
    }

    private void Start()
    {
        // Applied in Start so the XR components have finished their own setup
        Apply();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(Mathf.Round(value * 10f) / 10f);
        SaveAndApply();
    }

    public void ChangeVolume(int steps)
    {
        SetMasterVolume(MasterVolume + steps * 0.1f);
    }

    public void SetShowHands(bool value)
    {
        ShowHands = value;
        SaveAndApply();
    }

    public void SetSmoothMovement(bool value)
    {
        SmoothMovement = value;
        SaveAndApply();
    }

    public void SetSmoothTurning(bool value)
    {
        SmoothTurning = value;
        SaveAndApply();
    }

    public void CycleSnapAngle(int direction)
    {
        SnapAngleIndex = (SnapAngleIndex + direction + SnapAngles.Length) % SnapAngles.Length;
        SaveAndApply();
    }

    public void SetShowTimer(bool value)
    {
        ShowTimer = value;
        SaveAndApply();
    }

    // Called when the player presses V, so the menu and the keyboard stay in sync
    public void SyncShowHands(bool value)
    {
        ShowHands = value;
        PlayerPrefs.SetInt("settings.showHands", value ? 1 : 0);
        Changed?.Invoke();
    }

    private void SaveAndApply()
    {
        Save();
        Apply();
    }

    private void Apply()
    {
        AudioListener.volume = MasterVolume;

        if (visualModeToggle != null)
            visualModeToggle.SetShowHands(ShowHands);

        // Left stick: smooth movement or teleport
        if (leftControllerInput != null)
        {
            leftControllerInput.smoothMotionEnabled = SmoothMovement;
            leftControllerInput.smoothTurnEnabled = SmoothTurning;
        }

        // Right stick: always teleport + turn, only the turn style changes
        if (rightControllerInput != null)
            rightControllerInput.smoothTurnEnabled = SmoothTurning;

        if (snapTurnProvider != null)
            snapTurnProvider.turnAmount = SnapAngle;

        Changed?.Invoke();
    }

    private void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat("settings.volume", MasterVolume);
        ShowHands = PlayerPrefs.GetInt("settings.showHands", ShowHands ? 1 : 0) == 1;
        SmoothMovement = PlayerPrefs.GetInt("settings.smoothMove", SmoothMovement ? 1 : 0) == 1;
        SmoothTurning = PlayerPrefs.GetInt("settings.smoothTurn", SmoothTurning ? 1 : 0) == 1;
        SnapAngleIndex = Mathf.Clamp(PlayerPrefs.GetInt("settings.snapAngle", SnapAngleIndex), 0, SnapAngles.Length - 1);
        ShowTimer = PlayerPrefs.GetInt("settings.showTimer", ShowTimer ? 1 : 0) == 1;
    }

    private void Save()
    {
        PlayerPrefs.SetFloat("settings.volume", MasterVolume);
        PlayerPrefs.SetInt("settings.showHands", ShowHands ? 1 : 0);
        PlayerPrefs.SetInt("settings.smoothMove", SmoothMovement ? 1 : 0);
        PlayerPrefs.SetInt("settings.smoothTurn", SmoothTurning ? 1 : 0);
        PlayerPrefs.SetInt("settings.snapAngle", SnapAngleIndex);
        PlayerPrefs.SetInt("settings.showTimer", ShowTimer ? 1 : 0);
        PlayerPrefs.Save();
    }
}
