using System;
using UnityEngine;
//akhona khoali

// One clue the player can bring up in an interrogation. will be filled in on the
// InterrogationDoorInteractable in the Inspector - one per clue we want this suspect to be
// asked about. Only clues the player has already found in the EvidenceNotebook can be picked;
// everything else shows up locked in the carousel.
[Serializable]
public class InterrogationClue
{
    [Tooltip("Must match a Clue Id in the EvidenceNotebook, e.g. \"usb_stick\".")]
    public string clueId;

    [Header("Question (asked out loud when the player picks this clue)")]
    [TextArea(2, 4)]
    public string question;
    public AudioClip questionVoiceClip;

    [Header("Suspect's Answer")]
    [TextArea(2, 6)]
    public string answer;
    public AudioClip answerVoiceClip;

    [Header("Confession")]
    [Tooltip("Tick this if asking about this clue catches the suspect out - it fills the confession bar. Leave it off for a clue that doesn't matter.")]
    public bool isIncriminating = false;
}