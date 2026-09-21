using System.Collections;
using TMPro;
using UnityEngine;
//Akhona Khoali

// Shows a short message on screen, e.g. "Locked until the investigation is conducted".
//Canvas

public class MessageUI : MonoBehaviour
{
    public static MessageUI Instance { get; private set; }

    public TMP_Text messageText;

    private Coroutine hideRoutine;

    private void Awake()
    {
        Instance = this;

        if (messageText == null)
        {
            Debug.LogError("MessageUI: drag a TextMeshPro text into the Message Text slot.", this);
            return;
        }

        messageText.gameObject.SetActive(false);
    }

    public void Show(string message, float duration = 2.5f)
    {
        if (messageText == null) return;

        messageText.text = message;
        messageText.gameObject.SetActive(true);

        // If a message is already showing it will restart the timer.
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }
        hideRoutine = StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        messageText.gameObject.SetActive(false);
        hideRoutine = null;
    }
}