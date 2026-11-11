using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
//Akhona Khoali

// belongs on cannvas with the pause menu buttons. It opens and closes the pause menu, and handles the buttons on it.
// Esc opens and closes the pause menu. While open, the game is actually frozen
// (Time.timeScale = 0), not just visually paused, so enemies/animations/physics stop too.


// Panels (only one is ever shown at a time, inside the one pausePanel):
//   Main              >> Resume / Settings / Main Menu / Quit to Desktop buttons
//   Settings          >> reuse SettingsPanelUI (same Volume/Brightness sliders as the main menu)
//   Main Menu Confirm >> "Return to the main menu? Unsaved progress will be lost." Yes/No
//   Quit Confirm      >> "Quit to desktop?" Yes/No
public class PauseMenuUI : MonoBehaviour, IInteractionScreen
{
    public static PauseMenuUI Instance { get; private set; }

    [Header("Panels")]
    public GameObject pausePanel;          // The whole pause screen (everything below lives inside it).
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject mainMenuConfirmPanel;
    public GameObject quitConfirmPanel;

    [Header("Main Menu Scene")]
    public string mainMenuSceneName = "MainMenu";   // Must match the scene's exact name in Build Settings.

    private PlayerInteractor interactor;
    private bool isPaused;

    private void Awake()
    {
        Instance = this;
        pausePanel.SetActive(false);
        interactor = FindFirstObjectByType<PlayerInteractor>();
    }

    // Hook this up in the Player Input component: Pause action -> PauseMenuUI.OnPause.
    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isPaused)
        {
            Resume();
        }
        else
        {
            // Don't open over a dialogue, safe, computer or notebook screen.
            if (interactor != null && interactor.InputLocked) return;

            Open();
        }
    }

    // E does nothing here: the mouse drives this screen's buttons.
    public void OnInteractPressed()
    {
    }

    private void Open()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        ShowMain();

        Time.timeScale = 0f;

        if (interactor != null)
        {
            interactor.LockPlayer(this, true);
        }
    }

    private void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);

        Time.timeScale = 1f;

        if (interactor != null)
        {
            interactor.UnlockPlayer();
        }
    }

    // Buttons on the main pause panel 

    public void OnResumePressed()
    {
        Resume();
    }

    public void OnSettingsPressed()
    {
        ShowOnly(settingsPanel);
    }

    public void OnMainMenuPressed()
    {
        ShowOnly(mainMenuConfirmPanel);
    }

    public void OnQuitPressed()
    {
        ShowOnly(quitConfirmPanel);
    }

    //  Back button, wire it on both the Settings panel and either confirm panel's "No" 

    public void ShowMain()
    {
        ShowOnly(mainPanel);
    }

    // The two confirm panels' "Yes" buttons 

    public void OnMainMenuConfirmed()
    {
        // Time scale must be back to normal before leaving, or the main menu loads frozen too.
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuitConfirmed()
    {
        Application.Quit();

        // Application.Quit() does nothing in the Editor, so this stops Play Mode instead.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ShowOnly(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        mainMenuConfirmPanel.SetActive(panel == mainMenuConfirmPanel);
        quitConfirmPanel.SetActive(panel == quitConfirmPanel);
    }
}