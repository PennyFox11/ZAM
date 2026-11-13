using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One line of the control list: a small icon (the mouse, the E key...) and what it does.
[Serializable]
public class ControlHint
{
    public Sprite icon;                         // Your Krita PNG, imported as a Sprite (2D and UI).
    public string label = "open/close";
}

// Put this on your UI manager object (an active object, NOT the panel), like DialogueUI.
// One shared "what can I do" list. Anything can ask for it, and says who is asking:
//     ControlHintsUI.Instance.Show(this, hints);   // the player picks something up / opens a screen
//     ControlHintsUI.Instance.Hide(this);          // they put it down / close it
// If two things want the list at once (holding a note, then switching the scanner on), the
// newest one is shown, and the older one comes back when the newer one is hidden.
public class ControlHintsUI : MonoBehaviour
{
    public static ControlHintsUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;                    // The whole list. Starts hidden.
    public Transform rowContainer;              // Where the rows go (give it a Vertical Layout Group).
    public ControlHintRow rowPrefab;

    [Header("Fade")]
    [Tooltip("Optional: add a Canvas Group to the panel for a soft fade.")]
    public float fadeSpeed = 8f;

    private class Entry
    {
        public UnityEngine.Object owner;
        public ControlHint[] hints;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private CanvasGroup group;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;

        // If the row was left sitting in the scene instead of being a prefab file, hide that copy
        // so it doesn't show up as an extra row. (Copies made from it are switched on below.)
        if (rowPrefab != null && rowPrefab.gameObject.scene.IsValid())
        {
            rowPrefab.gameObject.SetActive(false);
        }

        if (panel != null)
        {
            group = panel.GetComponent<CanvasGroup>();
            panel.SetActive(false);
        }
    }

    // "owner" is whoever is asking (usually "this"). An empty list just removes that owner.
    public void Show(UnityEngine.Object owner, ControlHint[] hints)
    {
        RemoveOwner(owner);

        if (hints != null && hints.Length > 0)
        {
            entries.Add(new Entry { owner = owner, hints = hints });
        }

        Refresh();
    }

    public void Hide(UnityEngine.Object owner)
    {
        RemoveOwner(owner);
        Refresh();
    }

    private void RemoveOwner(UnityEngine.Object owner)
    {
        entries.RemoveAll(e => e.owner == owner);
    }

    // Shows the newest entry, or hides the panel when nobody wants it.
    private void Refresh()
    {
        if (panel == null || rowContainer == null || rowPrefab == null)
        {
            Debug.LogWarning("ControlHintsUI: Panel, Row Container and Row Prefab must all be filled in.", this);
            return;
        }

        // Forget owners that have been destroyed (for example a pickup that was removed).
        entries.RemoveAll(e => e.owner == null);

        if (entries.Count == 0)
        {
            if (panel.activeSelf)
            {
                StartFade(0f, true);
            }
            return;
        }

        ControlHint[] hints = entries[entries.Count - 1].hints;

        foreach (Transform child in rowContainer)
        {
            // Never delete the row template itself.
            if (child.gameObject == rowPrefab.gameObject) continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (ControlHint hint in hints)
        {
            ControlHintRow row = Instantiate(rowPrefab, rowContainer);
            row.gameObject.SetActive(true);
            row.Setup(hint.icon, hint.label);
        }

        panel.SetActive(true);
        StartFade(1f, false);
    }

    private void StartFade(float target, bool hideWhenDone)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        if (group == null)
        {
            if (hideWhenDone) panel.SetActive(false);
            return;
        }

        fadeRoutine = StartCoroutine(Fade(target, hideWhenDone));
    }

    // Unscaled time, so it still fades while the game is paused behind a screen.
    private IEnumerator Fade(float target, bool hideWhenDone)
    {
        while (!Mathf.Approximately(group.alpha, target))
        {
            group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        if (hideWhenDone)
        {
            panel.SetActive(false);
        }

        fadeRoutine = null;
    }
}