using UnityEngine;
//Akhona Khoali

// Who is talking on a given line.
public enum DialogueSpeaker
{
    Player,
    NPC
}

// One line of the conversation. Fill these in on the NPC in the Inspector.
// Alternate Player / NPC to make it feel like a real back-and-forth.
[System.Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;
    [TextArea(2, 5)]
    public string text;

    public AudioClip voiceClip;     // Optional. The recorded voice for this line. Plays when the line appears.

    [Tooltip("Optional. Matches a Clue Id in the EvidenceNotebook - reported to the notebook the moment this line is shown.")]
    public string clueId = "";
}