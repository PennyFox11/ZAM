using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
//Akhona khoali

// goes on canvas, and shows a readable text box when the player is holding a NoteInteractable with legible text set.
// While the player is holding a NoteInteractable that has legible text set, holding Space shows
// that clean, readable text on screen - useful when the note's own art (handwriting, a stamp,
// a drawing...) is too stylised to actually read. Let go of Space and it disappears again.
public class NoteReadUI : MonoBehaviour
{
    public static NoteReadUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject readPanel;    // A readable text box. Starts hidden.
    public TMP_Text readText;

    private NoteInteractable currentNote;

    private void Awake()
    {
        Instance = this;

        if (readPanel != null)
        {
            readPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (currentNote == null || Keyboard.current == null)
        {
            if (readPanel != null && readPanel.activeSelf)
            {
                readPanel.SetActive(false);
            }
            return;
        }

        // Held down, not toggled: it shows only while Space is actually pressed.
        bool reading = Keyboard.current.spaceKey.isPressed;

        if (readPanel != null && readPanel.activeSelf != reading)
        {
            readPanel.SetActive(reading);
        }
    }

    // Called by NoteInteractable when it's picked up.
    public void SetCurrentNote(NoteInteractable note)
    {
        currentNote = note;

        if (readText != null)
        {
            readText.text = note != null ? note.legibleText : "";
        }
    }

    // Called by NoteInteractable when it's dropped. Only clears if this really is the note
    // that was being read, so dropping an old note after picking up a new one can't wipe it out.
    public void ClearCurrentNote(NoteInteractable note)
    {
        if (currentNote != note) return;

        currentNote = null;

        if (readPanel != null)
        {
            readPanel.SetActive(false);
        }
    }
}
