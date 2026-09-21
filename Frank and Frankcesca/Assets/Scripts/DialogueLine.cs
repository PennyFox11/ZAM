using UnityEngine;
//Akhona Khoali

// Who is talking on a given line.
public enum DialogueSpeaker
{
    Player,
    NPC
}

// One line of the conversation. Fill these in on the NPC in the Inspector.
// We have both Player and  NPC to make it feel like a real back and forth betwwen them
[System.Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;
    [TextArea(2, 5)]
    public string text;

    public AudioClip voiceClip;     // Minnah asked to have voice recordings for each line, so we can play them when the line is shown.
}