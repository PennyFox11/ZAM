using System;
using UnityEngine;
using UnityEngine.Video;
//Akhona Khoali
//This is what ends the game.
//It is used by the DialogueInteractable to offer the player a choice of two endings.
//Each choice plays its own video cutscene, then goes to the main menu.

// One of the two options (A or B) offered at the end of a conversation. Fill it in the inspector
// DialogueInteractable in the Inspector, under "Ending Choice".
[Serializable]
public class DialogueChoice
{
    [TextArea(1, 3)]
    public string label = "Option";             // What the button says, e.g. "A: Turn him in."

    [Tooltip("A short name for this ending, e.g. \"turned_in\". Can be read from EndingResult.Chosen.")]
    public string endingId = "";

    [Tooltip("The exact scene name (it must be in Build Settings) to load AFTER the video, e.g. \"MainMenu\".")]
    public string sceneName = "MainMenu";

    [Tooltip("The cutscene video that plays when this option is picked.")]
    public VideoClip endingVideo;
}

// Remembers which ending the player picked, so the main menu scene (or anything else) can read it
// after the scene change. It resets when the game is closed.
public static class EndingResult
{
    public static string Chosen = "";
}