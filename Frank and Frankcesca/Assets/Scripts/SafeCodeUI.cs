using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
//Akhona Khoali

//  UI manager object
// The player types the code with the number keys (top row or numpad).
// Backspace deletes a digit, E leaves the keypad.
public class SafeCodeUI : MonoBehaviour, IInteractionScreen
{
    public static SafeCodeUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject safePanel;
    public TMP_Text promptText;         // "Enter code"
    public TMP_Text codeText;           // "1 _ _"
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

    private SafeInteractable currentSafe;
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

    public void Open(SafeInteractable safe, PlayerInteractor interactor)
    {
        currentSafe = safe;
        currentInteractor = interactor;
        enteredCode = "";

        safePanel.SetActive(true);
        promptText.text = "Enter code";
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

        if (enteredCode.Length >= currentSafe.code.Length) return;

        enteredCode += character;
        PlaySound(keyClip);
        UpdateCodeDisplay();

        // All digits typed: check the code.
        if (enteredCode.Length == currentSafe.code.Length)
        {
            StartCoroutine(CheckCode());
        }
    }

    private IEnumerator CheckCode()
    {
        acceptingInput = false;

        if (enteredCode == currentSafe.code)
        {
            // Correct code.
            feedbackText.text = "CORRECT";
            feedbackText.color = correctColour;
            PlaySound(correctClip);

            yield return new WaitForSeconds(feedbackTime);

            SafeInteractable safe = currentSafe;
            Close();
            safe.Unlock();
            safe.Open();
        }
        else
        {
            // Wrong code: the opposite happens. The safe stays locked.
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
        int length = currentSafe.code.Length;
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