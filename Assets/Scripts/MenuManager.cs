using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Runs every menu of the game on one World Space canvas:
// main menu, controls, settings, pause and the end screen.
public class MenuManager : MonoBehaviour
{
    public enum Page { None, Main, Controls, Settings, Pause, End }

    // Set by Restart so the reloaded scene skips the main menu
    private static bool skipMainMenu;

    [Header("Canvases")]
    public GameObject menuCanvas;
    public GameObject hudCanvas;

    [Header("Pages")]
    public GameObject mainPage;
    public GameObject controlsPage;
    public GameObject settingsPage;
    public GameObject pausePage;
    public GameObject endPage;

    [Header("Game")]
    public GameTimer gameTimer;
    public GameSettings settings;
    [Tooltip("Locomotion components that are switched off while a menu is open.")]
    public Behaviour[] lockedWhileInMenu;

    [Header("Texts")]
    public TMP_Text pauseTimeText;
    public TMP_Text endTimeText;

    [Header("Settings Values")]
    public TMP_Text volumeValue;
    public RectTransform volumeFill;
    public TMP_Text handsValue;
    public TMP_Text movementValue;
    public TMP_Text turningValue;
    public TMP_Text snapAngleValue;
    public CanvasGroup snapAngleRow;
    public TMP_Text timerValue;

    [Header("Audio")]
    public AudioClip clickClip;

    private Page currentPage = Page.None;
    private Page returnPage = Page.Main;
    private bool gameStarted;
    private bool gameEnded;
    private bool loading;

    private AudioSource uiAudio;
    private InputAction pauseAction;

    public Page CurrentPage => currentPage;

    private void Awake()
    {
        uiAudio = gameObject.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.spatialBlend = 0f;
        uiAudio.ignoreListenerPause = true;

        // Left controller Menu button, or Escape on the keyboard / simulator
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
        pauseAction.AddBinding("<Keyboard>/escape");
    }

    private void OnEnable()
    {
        pauseAction.Enable();

        if (settings != null)
            settings.Changed += RefreshSettings;
    }

    private void OnDisable()
    {
        pauseAction.Disable();

        if (settings != null)
            settings.Changed -= RefreshSettings;
    }

    private void OnDestroy()
    {
        pauseAction.Dispose();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        RefreshSettings();

        if (skipMainMenu)
        {
            skipMainMenu = false;
            BeginGame();
        }
        else
        {
            SetLocomotion(false);
            ShowPage(Page.Main);
        }
    }

    private void Update()
    {
        if (loading || !pauseAction.WasPressedThisFrame())
            return;

        switch (currentPage)
        {
            case Page.None:
                if (gameStarted && !gameEnded)
                    Pause();
                break;

            case Page.Pause:
                Resume();
                break;

            case Page.Controls:
            case Page.Settings:
                Back();
                break;
        }
    }

    // ---------- Main menu ----------

    public void Play()
    {
        PlayClick();
        BeginGame();
    }

    public void OpenControls()
    {
        PlayClick();
        returnPage = currentPage;
        ShowPage(Page.Controls);
    }

    public void OpenSettings()
    {
        PlayClick();
        returnPage = currentPage;
        ShowPage(Page.Settings);
    }

    public void Back()
    {
        PlayClick();
        ShowPage(returnPage);
    }

    public void Quit()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Pause menu ----------

    public void Pause()
    {
        PlayClick();
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetLocomotion(false);

        if (pauseTimeText != null && gameTimer != null)
            pauseTimeText.text = "TIME  " + gameTimer.FormattedTime;

        ShowPage(Page.Pause);
    }

    public void Resume()
    {
        PlayClick();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetLocomotion(true);
        ShowPage(Page.None);
    }

    public void Restart()
    {
        skipMainMenu = true;
        ReloadScene();
    }

    public void GoToMainMenu()
    {
        skipMainMenu = false;
        ReloadScene();
    }

    // ---------- End screen ----------

    // Called by Room2PuzzleManager when the player escapes
    public void ShowEndScreen()
    {
        gameEnded = true;

        if (gameTimer != null)
        {
            gameTimer.StopTimer();

            if (endTimeText != null)
                endTimeText.text = gameTimer.FormattedTime;
        }

        SetLocomotion(false);
        ShowPage(Page.End);
        Time.timeScale = 0f;
    }

    // ---------- Settings ----------

    public void VolumeDown() { PlayClick(); settings.ChangeVolume(-1); }
    public void VolumeUp() { PlayClick(); settings.ChangeVolume(1); }
    public void ToggleHands() { PlayClick(); settings.SetShowHands(!settings.ShowHands); }
    public void ToggleMovement() { PlayClick(); settings.SetSmoothMovement(!settings.SmoothMovement); }
    public void ToggleTurning() { PlayClick(); settings.SetSmoothTurning(!settings.SmoothTurning); }
    public void SnapAnglePrevious() { PlayClick(); settings.CycleSnapAngle(-1); }
    public void SnapAngleNext() { PlayClick(); settings.CycleSnapAngle(1); }
    public void ToggleTimer() { PlayClick(); settings.SetShowTimer(!settings.ShowTimer); }

    private void RefreshSettings()
    {
        if (settings == null)
            return;

        if (volumeValue != null)
            volumeValue.text = Mathf.RoundToInt(settings.MasterVolume * 100f) + "%";

        if (volumeFill != null)
            volumeFill.anchorMax = new Vector2(settings.MasterVolume, 1f);

        if (handsValue != null)
            handsValue.text = settings.ShowHands ? "HANDS" : "CONTROLLERS";

        if (movementValue != null)
            movementValue.text = settings.SmoothMovement ? "SMOOTH" : "TELEPORT";

        if (turningValue != null)
            turningValue.text = settings.SmoothTurning ? "SMOOTH" : "SNAP";

        if (snapAngleValue != null)
            snapAngleValue.text = settings.SnapAngle.ToString("0") + "°";

        // Snap angle only matters with snap turning
        if (snapAngleRow != null)
        {
            snapAngleRow.alpha = settings.SmoothTurning ? 0.35f : 1f;
            snapAngleRow.interactable = !settings.SmoothTurning;
        }

        if (timerValue != null)
            timerValue.text = settings.ShowTimer ? "ON" : "OFF";

        UpdateHud();
    }

    // ---------- Helpers ----------

    private void BeginGame()
    {
        gameStarted = true;
        gameEnded = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetLocomotion(true);

        if (gameTimer != null)
            gameTimer.StartTimer();

        ShowPage(Page.None);
    }

    private void ShowPage(Page page)
    {
        bool wasOpen = currentPage != Page.None;
        currentPage = page;

        if (mainPage != null) mainPage.SetActive(page == Page.Main);
        if (controlsPage != null) controlsPage.SetActive(page == Page.Controls);
        if (settingsPage != null) settingsPage.SetActive(page == Page.Settings);
        if (pausePage != null) pausePage.SetActive(page == Page.Pause);
        if (endPage != null) endPage.SetActive(page == Page.End);

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(page != Page.None);

            // Bring the menu in front of the player when it opens
            VRPanelFollow follow = menuCanvas.GetComponent<VRPanelFollow>();

            if (follow != null && page != Page.None && !wasOpen)
                follow.SnapToTarget();
        }

        UpdateHud();
    }

    private void UpdateHud()
    {
        if (hudCanvas == null)
            return;

        bool show = gameStarted && !gameEnded && currentPage == Page.None &&
                    (settings == null || settings.ShowTimer);

        hudCanvas.SetActive(show);
    }

    private void SetLocomotion(bool enabled)
    {
        if (lockedWhileInMenu == null)
            return;

        foreach (Behaviour behaviour in lockedWhileInMenu)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    private void PlayClick()
    {
        if (clickClip != null)
            uiAudio.PlayOneShot(clickClip);
    }

    private void ReloadScene()
    {
        if (loading)
            return;

        loading = true;
        PlayClick();
        StartCoroutine(ReloadAfterClick());
    }

    private IEnumerator ReloadAfterClick()
    {
        // Small delay so the click sound can be heard
        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
