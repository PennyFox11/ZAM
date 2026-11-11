using UnityEngine;
using UnityEngine.SceneManagement;
//Akhona Khoali
//Canvas
// Switches between the four panels - only one is ever visible at a time - and loads the
// game scene for New Game / Continue.
public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;        // Continue / New Game / Settings / Credits / Quit buttons.
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject quitConfirmPanel;

    [Header("New Game")]
    public string gameSceneName = "Game";   // Must match the scene's exact name in Build Settings.

    private void Awake()
    {
        ShowMain();
    }

    //  Buttons on the main panel 

    public void OnNewGamePressed()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnContinuePressed()
    {
        // Hook this up to your save system once it exists. For now it just starts the game scene,
        // same as New Game.
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnSettingsPressed()
    {
        ShowOnly(settingsPanel);
    }

    public void OnCreditsPressed()
    {
        ShowOnly(creditsPanel);
    }

    public void OnQuitPressed()
    {
        ShowOnly(quitConfirmPanel);
    }

    //  Back button on Settings / Credits, wire both to this 

    public void ShowMain()
    {
        ShowOnly(mainPanel);
    }

    // The two buttons on the Quit confirm panel 

    public void OnQuitConfirmed()
    {
        Application.Quit();

        // Application.Quit() does nothing in the Editor, so this stops Play Mode instead,
        // letting you actually test the button without needing a built .exe.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnQuitCancelled()
    {
        ShowMain();
    }

    private void ShowOnly(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        creditsPanel.SetActive(panel == creditsPanel);
        quitConfirmPanel.SetActive(panel == quitConfirmPanel);
    }
}