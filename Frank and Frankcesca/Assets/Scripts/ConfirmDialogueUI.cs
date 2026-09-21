using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//Akhona Khoali

// For canvas
// A Yes / No question that freezes the player and shows the mouse cursor.
//this script is used to show a confirmation dialog when the player interacts with certain objects in the game. 
//It displays a message and provides Yes and No buttons for the player to respond.
// The player's movement is locked while the dialog is active, and the mouse cursor is shown for button interaction.

public class ConfirmDialogUI : MonoBehaviour, IInteractionScreen
{
    public static ConfirmDialogUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialogPanel;      // Starts hidden.
    public TMP_Text messageText;
    public Button yesButton;
    public Button noButton;

    [Header("Sounds (all optional)")]
    public AudioClip openClip;
    public AudioClip clickClip;

    private PlayerInteractor currentInteractor;
    private Action onYes;
    private Action onNo;
    private AudioSource sfxAudioSource;

    private void Awake()
    {
        Instance = this;
        dialogPanel.SetActive(false);

        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.pitch = 1f;
    }

    // Shows the question. yesAction runs if the player clicks Yes, noAction (optional) if they click No.
    public void Ask(string message, PlayerInteractor interactor, Action yesAction, Action noAction = null)
    {
        currentInteractor = interactor;
        onYes = yesAction;
        onNo = noAction;

        messageText.text = message;
        dialogPanel.SetActive(true);

        // Freeze the player and show the mouse cursor so the buttons can be clicked.
        currentInteractor.LockPlayer(this, true);

        if (openClip != null)
        {
            sfxAudioSource.PlayOneShot(openClip);
        }
    }

    // E does nothing here: the player has to click Yes or No.
    public void OnInteractPressed()
    {
    }

    private void OnYesClicked()
    {
        PlayClick();
        Close();

        if (onYes != null)
        {
            onYes();
        }
    }

    private void OnNoClicked()
    {
        PlayClick();
        Close();

        if (onNo != null)
        {
            onNo();
        }
    }

    private void Close()
    {
        dialogPanel.SetActive(false);
        currentInteractor.UnlockPlayer();
    }

    private void PlayClick()
    {
        if (clickClip != null)
        {
            sfxAudioSource.PlayOneShot(clickClip);
        }
    }
}