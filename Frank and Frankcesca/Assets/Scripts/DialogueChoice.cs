using System;
using UnityEngine;
//Akhona Khoali
//This is what ends the game.
//It is used by the DialogueInteractable to offer the player a choice of two endings,
//and leads back to the main menu regardless of what the player chooses.

// One of the two options (A or B) offered at the end of a conversation. I will Fill it in the inspector
// DialogueInteractable in the Inspector, under "Ending Choice".
[Serializable]
public class DialogueChoice
{
    [TextArea(1, 3)]
    public string label = "Option";             // What the button says, e.g. "A: Turn him in."

    [Tooltip("A short name for this ending, e.g. \"turned_in\". The credits scene can read it from EndingResult.Chosen to show a different message per choice.")]
    public string endingId = "";

    [Tooltip("The exact scene name (it must be in Build Settings) to load when this is picked, e.g. \"Credits\". Both options can use the same scene.")]
    public string sceneName = "Credits";
}

// Remembers which ending the player picked, so the main menu scene (or anything else) can read it
// after the scene change. It resets when the game is closed.
public static class EndingResult
{
    public static string Chosen = "";
}
