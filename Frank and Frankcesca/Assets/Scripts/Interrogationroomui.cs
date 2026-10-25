using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//Akhona Khoali

// belongs to canvas
// One shared interrogation screen, the same way ComputerDatabaseUI is shared by every computer:
// InterrogationDoorInteractable.Interact() calls Begin(), handing over that suspect's clues,
// confession settings, timer and teleport points.
//
// Flow:
//   E on the door -> fade out, player is teleported inside to Room Point, frozen (cursor shown)
//   The Interrogation Panel is mostly SEE-THROUGH in the Editor (set its background Image's
//   alpha low) so the suspect is visible moving around behind the UI.
//   The bottom carousel shows every clue on this suspect's list, built from the
//   EvidenceNotebook: not found yet -> locked/dark/unclickable, found -> clickable.
//   Clicking a clue asks its question (subtitle + voice), then shows the suspect's answer
//   (top-left box + voice). An incriminating clue also fills the confession bar a bit.
//   Full bar -> the suspect confesses (voice line), then the interrogation ends.
//   The countdown ends it too, whatever the bar is at. E also ends it early.
//   Ending it (any way) fades the player back out to Return Point, unfreezes them, and rings
//   the suspect's phone - and the door won't open again.
public class InterrogationRoomUI : MonoBehaviour, IInteractionScreen
{
    public static InterrogationRoomUI Instance { get; private set; }

    [Header("Room Panel")]
    public GameObject interrogationPanel;       // The whole screen. Keep its background mostly transparent.

    [Header("Clue Carousel (bottom)")]
    public Transform clueRowContainer;          // The horizontal Scroll View's "Content" object.
    public InterrogationClueRow rowPrefab;
    public ScrollRect clueScrollRect;           // Optional. Lets picking a clue smoothly scroll it to the middle.
    public RectTransform centreMarker;          // An empty RectTransform sitting in the middle of the carousel viewport.
    public float scrollToCentreTime = 0.3f;

    [Header("Question Preview (hover)")]
    public GameObject questionPreviewPanel;     // Optional. Small box shown while the mouse is over a clue. Starts hidden.
    public TMP_Text questionPreviewText;

    [Header("Subtitles / Voiceover")]
    public GameObject subtitlePanel;            // The player's question. Starts hidden.
    public TMP_Text subtitleText;
    public GameObject answerPanel;              // The suspect's answer, top-left box. Starts hidden.
    public TMP_Text answerText;
    public float typingSpeed = 0.03f;
    public float pauseBetweenQuestionAndAnswer = 0.4f;
    public AudioSource voiceAudioSource;        // Plays question/answer/confession clips. Created for you if empty.

    [Header("Confession Bar")]
    public Slider confessionBar;                // 0 - 1. Fills as incriminating clues are asked about.

    [Header("Timer")]
    public TMP_Text timerText;

    [Header("Fade")]
    public CanvasGroup fadePanel;               // Optional: a full-screen black Image with a Canvas Group.
    public float fadeTime = 0.5f;

    [Header("Sounds (all optional)")]
    public AudioClip enterClip;
    public AudioClip selectClip;

    private InterrogationDoorInteractable currentDoor;
    private PlayerInteractor currentInteractor;
    private InterrogationClueRow currentRow;
    private List<InterrogationClueRow> rows = new List<InterrogationClueRow>();

    private AudioSource sfxAudioSource;
    private Coroutine askRoutine;
    private Coroutine timerRoutine;
    private bool isSessionActive;
    private int incriminatingAskedCount;

    private void Awake()
    {
        Instance = this;
        interrogationPanel.SetActive(false);

        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
            fadePanel.interactable = false;
            fadePanel.gameObject.SetActive(false);
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
        }
        voiceAudioSource.playOnAwake = false;

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;

        ResetPanels();
    }

    // Starting 

    public void Begin(InterrogationDoorInteractable door, PlayerInteractor interactor)
    {
        if (isSessionActive) return;

        currentDoor = door;
        currentInteractor = interactor;
        currentRow = null;
        incriminatingAskedCount = 0;
        isSessionActive = true;

        // The one shot is spent now, win or lose - the door won't open again after this.
        door.MarkUsed();

        StartCoroutine(BeginRoutine());
    }

    private IEnumerator BeginRoutine()
    {
        yield return Fade(0f, 1f);

        interrogationPanel.SetActive(true);

        TeleportPlayer(currentDoor.roomPoint);

        // true = show the cursor so the carousel can be clicked.
        currentInteractor.LockPlayer(this, true);

        BuildRows();
        ResetPanels();
        UpdateConfessionBar();

        yield return Fade(1f, 0f);

        timerRoutine = StartCoroutine(TimerRoutine(currentDoor.timeLimitSeconds));

        PlaySound(enterClip);
    }

    // Builds one carousel row per clue on this suspect's list, locked or available depending
    // on whether the player has found that clue in the EvidenceNotebook.
    private void BuildRows()
    {
        foreach (Transform child in clueRowContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        rows.Clear();

        if (currentDoor.clues == null) return;

        foreach (InterrogationClue clue in currentDoor.clues)
        {
            InterrogationClueRow row = Instantiate(rowPrefab, clueRowContainer);
            row.Setup(clue, centreMarker);

            bool found = EvidenceNotebook.Instance != null && EvidenceNotebook.Instance.IsClueDiscovered(clue.clueId);

            if (found)
            {
                ClueEntry entry = EvidenceNotebook.Instance.GetClue(clue.clueId);
                Sprite image = entry != null ? entry.image : null;
                string title = entry != null ? entry.title : clue.clueId;
                row.SetAvailable(image, title);
            }
            else
            {
                row.SetLocked();
            }

            rows.Add(row);
        }
    }

    //Picking a clue (called by InterrogationClueRow when clicked) 

    public void SelectClue(InterrogationClueRow row)
    {
        if (!isSessionActive || !row.IsAvailable) return;

        if (currentRow != null)
        {
            currentRow.SetSelected(false);
        }

        currentRow = row;
        row.SetSelected(true);

        HideQuestionPreview();
        PlaySound(selectClip);
        ScrollToRow(row);

        if (askRoutine != null)
        {
            StopCoroutine(askRoutine);
        }
        askRoutine = StartCoroutine(AskRoutine(row));
    }

    // Hover preview (called by InterrogationClueRow) 

    public void ShowQuestionPreview(InterrogationClueRow row)
    {
        if (!isSessionActive || questionPreviewPanel == null || row == null || row.Clue == null) return;

        if (questionPreviewText != null)
        {
            questionPreviewText.text = row.Clue.question;
        }

        questionPreviewPanel.SetActive(true);
    }

    public void HideQuestionPreview()
    {
        if (questionPreviewPanel != null)
        {
            questionPreviewPanel.SetActive(false);
        }
    }

    private IEnumerator AskRoutine(InterrogationClueRow row)
    {
        InterrogationClue clue = row.Clue;

        // The player's question.
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        PlayVoice(clue.questionVoiceClip);
        yield return TypeText(subtitleText, clue.question);

        yield return new WaitForSeconds(pauseBetweenQuestionAndAnswer);

        // The suspect's answer.
        if (answerPanel != null) answerPanel.SetActive(true);
        PlayVoice(clue.answerVoiceClip);
        yield return TypeText(answerText, clue.answer);

        row.MarkAsked();
        row.SetSelected(false);   // Falls back to the red "already asked" outline now.

        if (clue.isIncriminating)
        {
            incriminatingAskedCount++;
            UpdateConfessionBar();

            if (incriminatingAskedCount >= Mathf.Max(1, currentDoor.incriminatingCluesToConfess))
            {
                yield return new WaitForSeconds(0.6f);
                yield return ConfessRoutine();
            }
        }

        askRoutine = null;
    }

    private IEnumerator ConfessRoutine()
    {
        if (answerPanel != null) answerPanel.SetActive(true);
        PlayVoice(currentDoor.confessionVoiceClip);
        yield return TypeText(answerText, currentDoor.confessionLine);

        yield return new WaitForSeconds(1.5f);

        EndSession();
    }

    //  Timer 

    private IEnumerator TimerRoutine(float seconds)
    {
        float remaining = seconds;

        while (remaining > 0f)
        {
            UpdateTimerText(remaining);
            yield return null;
            remaining -= Time.deltaTime;
        }

        UpdateTimerText(0f);
        timerRoutine = null;
        EndSession();
    }

    private void UpdateTimerText(float remaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text = minutes + ":" + seconds.ToString("00");
    }

    // Leaving 

    // E leaves early - the door is already used up (Begin() marked it), so there's nothing
    // more to decide here.
    public void OnInteractPressed()
    {
        EndSession();
    }

    private void EndSession()
    {
        if (!isSessionActive) return;
        isSessionActive = false;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (askRoutine != null)
        {
            StopCoroutine(askRoutine);
            askRoutine = null;
        }

        voiceAudioSource.Stop();

        StartCoroutine(EndRoutine());
    }

    private IEnumerator EndRoutine()
    {
        yield return Fade(0f, 1f);

        interrogationPanel.SetActive(false);
        ResetPanels();

        TeleportPlayer(currentDoor.returnPoint);
        currentInteractor.UnlockPlayer();

        yield return Fade(1f, 0f);

        if (currentDoor.phone != null)
        {
            currentDoor.phone.StartRinging();
        }

        if (!string.IsNullOrEmpty(currentDoor.checklistId))
        {
            ClueTrigger.DiscoverClue(currentDoor.checklistId);
        }

        currentDoor = null;
        currentInteractor = null;
        currentRow = null;
    }

    //  Helpers 

    private void TeleportPlayer(Transform point)
    {
        Transform player = currentInteractor.transform;

        // A CharacterController must be switched off, or it pulls the player back to the old spot.
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        player.position = point.position;
        player.rotation = Quaternion.Euler(0f, point.eulerAngles.y, 0f);

        if (controller != null) controller.enabled = true;
    }

    private void ResetPanels()
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (answerPanel != null) answerPanel.SetActive(false);
        if (questionPreviewPanel != null) questionPreviewPanel.SetActive(false);
        if (subtitleText != null) subtitleText.text = "";
        if (answerText != null) answerText.text = "";
    }

    private void UpdateConfessionBar()
    {
        if (confessionBar == null || currentDoor == null) return;

        int needed = Mathf.Max(1, currentDoor.incriminatingCluesToConfess);
        confessionBar.value = Mathf.Clamp01((float)incriminatingAskedCount / needed);
    }

    private void ScrollToRow(InterrogationClueRow row)
    {
        if (clueScrollRect == null) return;

        int index = rows.IndexOf(row);
        if (index < 0 || rows.Count <= 1) return;

        float normalized = (float)index / (rows.Count - 1);
        StartCoroutine(ScrollToNormalized(normalized));
    }

    private IEnumerator ScrollToNormalized(float target)
    {
        float start = clueScrollRect.horizontalNormalizedPosition;
        float time = 0f;

        while (time < scrollToCentreTime)
        {
            time += Time.deltaTime;
            clueScrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, time / scrollToCentreTime);
            yield return null;
        }

        clueScrollRect.horizontalNormalizedPosition = target;
    }

    // Types a line into a TMP text one letter at a time, like the dialogue box.
    private IEnumerator TypeText(TMP_Text text, string fullText)
    {
        if (text == null) yield break;

        text.text = fullText;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();

        int totalLetters = text.textInfo.characterCount;

        for (int i = 1; i <= totalLetters; i++)
        {
            text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void PlayVoice(AudioClip clip)
    {
        voiceAudioSource.Stop();
        if (clip == null) return;

        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);

        float time = 0f;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, time / fadeTime);
            yield return null;
        }

        fadePanel.alpha = to;

        if (to <= 0f)
        {
            fadePanel.gameObject.SetActive(false);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
}