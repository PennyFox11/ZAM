using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
//Akhona Khoali

// Belongs to canvases that show a keypad for entering a code. The code is set in the Inspector on the SafeInteractable
// The player types the code with the number keys (top row or numpad).
// Backspace deletes a digit, E leaves the keypad.
// The safe uses Open(safe, interactor). A code-locked computer (or anything else) can use
// OpenForCode(...) to show this same keypad and run something when the code is right.
public class SafeCodeUI : MonoBehaviour, IInteractionScreen
{
    public static SafeCodeUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject safePanel;
    public TMP_Text promptText;         // "Enter code"
    public TMP_Text codeText;           // "1 2 _ _"
    public TMP_Text feedbackText;       // "CORRECT" or "WRONG"

    [Header("Colours")]
    public Color correctColour = Color.green;
    public Color wrongColour = Color.red;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip keyClip;           // Typing sound for each digit.
    public AudioClip correctClip;
    public AudioClip wrongClip;

    [Header("Timing")]
    public float feedbackTime = 1.2f;   // How long CORRECT / WRONG stays on screen.

    private string currentCode = "";
    private System.Action onCorrect;
    private PlayerInteractor currentInteractor;
    private string enteredCode = "";
    private bool acceptingInput;

    private void Awake()
    {
        Instance = this;
        safePanel.SetActive(false);
    }

    private void OnDisable()
    {
        StopListening();
    }

    // The safe: the right code unlocks it and opens it.
    public void Open(SafeInteractable safe, PlayerInteractor interactor)
    {
        OpenForCode(safe.code, interactor, () =>
        {
            safe.Unlock();
            safe.Open();
        });
    }

    // Shows the keypad for any code. onCorrectCode runs after the keypad has closed.
    public void OpenForCode(string code, PlayerInteractor interactor, System.Action onCorrectCode, string prompt = "Enter code")
    {
        currentCode = code;
        onCorrect = onCorrectCode;
        currentInteractor = interactor;
        enteredCode = "";

        safePanel.SetActive(true);
        promptText.text = prompt;
        feedbackText.text = "";
        UpdateCodeDisplay();

        // Freeze the player. The prompt "[E] Enter code" disappears automatically.
        currentInteractor.LockPlayer(this, false);

        // Ask the Input System to tell us about every typed character.
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += HandleTextInput;
        }

        acceptingInput = true;
    }

    // E leaves the keypad (unless the code is being checked).
    public void OnInteractPressed()
    {
        if (!acceptingInput) return;
        Close();
    }

    private void Update()
    {
        if (!acceptingInput) return;

        if (Keyboard.current != null &&
            Keyboard.current.backspaceKey.wasPressedThisFrame &&
            enteredCode.Length > 0)
        {
            enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
            UpdateCodeDisplay();
        }
    }

    // Called by the Input System for every character the player types.
    private void HandleTextInput(char character)
    {
        if (!acceptingInput) return;

        // Ignore letters and symbols (this also ignores the E used to leave).
        if (!char.IsDigit(character)) return;

        if (enteredCode.Length >= currentCode.Length) return;

        enteredCode += character;
        PlaySound(keyClip);
        UpdateCodeDisplay();

        // All digits typed: check the code.
        if (enteredCode.Length == currentCode.Length)
        {
            StartCoroutine(CheckCode());
        }
    }

    private IEnumerator CheckCode()
    {
        acceptingInput = false;

        if (enteredCode == currentCode)
        {
            // Correct code.
            feedbackText.text = "CORRECT";
            feedbackText.color = correctColour;
            PlaySound(correctClip);

            yield return new WaitForSeconds(feedbackTime);

            System.Action whenCorrect = onCorrect;
            Close();

            if (whenCorrect != null)
            {
                whenCorrect();
            }
        }
        else
        {
            // Wrong code: the opposite happens. It stays locked.
            feedbackText.text = "WRONG";
            feedbackText.color = wrongColour;
            PlaySound(wrongClip);

            yield return new WaitForSeconds(feedbackTime);

            enteredCode = "";
            feedbackText.text = "";
            UpdateCodeDisplay();
            acceptingInput = true;
        }
    }

    private void Close()
    {
        acceptingInput = false;
        StopListening();

        safePanel.SetActive(false);
        currentInteractor.UnlockPlayer();
    }

    private void StopListening()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= HandleTextInput;
        }
    }

    // Builds text like "1 2 _ _" from what has been typed so far.
    private void UpdateCodeDisplay()
    {
        int length = currentCode.Length;
        string display = "";

        for (int i = 0; i < length; i++)
        {
            display += (i < enteredCode.Length) ? enteredCode[i].ToString() : "_";

            if (i < length - 1)
            {
                display += " ";
            }
        }

        codeText.text = display;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}