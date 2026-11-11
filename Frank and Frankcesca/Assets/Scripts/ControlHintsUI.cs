using System;
using System.Collections;
using UnityEngine;

// One line of the control list: a small icon (the mouse, the E key...) and what it does.
[Serializable]
public class ControlHint
{
    public Sprite icon;                         // Your Krita PNG, imported as a Sprite (2D and UI).
    public string label = "open/close";
}

// Put this on your UI manager object (an active object, NOT the panel), like DialogueUI.
// One shared "what can I do" list. Any item can show it:
//     ControlHintsUI.Instance.Show(hints);   // when the player picks something up
//     ControlHintsUI.Instance.Hide();        // when they put it down
// It builds one row per hint from the Row Prefab, so the same panel serves the book and any
// other interactable - each just hands over its own list.
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

    public void Show(ControlHint[] hints)
    {
        if (panel == null || rowContainer == null || rowPrefab == null)
        {
            Debug.LogWarning("ControlHintsUI: Panel, Row Container and Row Prefab must all be filled in.", this);
            return;
        }

        if (hints == null || hints.Length == 0)
        {
            Hide();
            return;
        }

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

    public void Hide()
    {
        if (panel == null || !panel.activeSelf) return;
        StartFade(0f, true);
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