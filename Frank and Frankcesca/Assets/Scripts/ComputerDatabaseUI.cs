using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
//Akhona Khoali This was exausting yoh 
// Belongs in Canvas
// One database screen is shared by all three computers: it builds its buttons from
// whichever ComputerInteractable the player opened.

// Flow:  Boot screen (typed lines)  then Home (category buttons)  thenn  List (entries)  then  View (image / text)
// The Back button (top-right) goes back one page. E leaves the computer completely.
// Entry text types itself out one letter at a time, like the dialogue box, the cursor blinks.
// Clicking the mouse while the boot screen or the text is typing skips to the end.
public class ComputerDatabaseUI : MonoBehaviour, IInteractionScreen//  IInteractionScreen is similar to the IInteractable interface, but for UI screens.
                                                                   // It allows the PlayerInteractor to lock the player and show the mouse cursor while this screen is open.
{
    public static ComputerDatabaseUI Instance { get; private set; }

    [Header("Panels")]
    public GameObject computerPanel;    // The whole database screen.
    public GameObject homePanel;        // Holds the category buttons.
    public GameObject listPanel;        // Holds the entry buttons.
    public GameObject viewPanel;        // Shows the image / text.

    [Header("Button Containers")]
    public Transform categoryContainer; // Inside homePanel (add a Vertical Layout Group).
    public Transform entryContainer;    // Inside listPanel (add a Vertical Layout Group).I learnt about vertical layout group from the Unity documentation.
                                        //It is a component that automatically arranges its child elements in a vertical list, making it easier to create dynamic UI layouts.
    public Button buttonPrefab;         // A Button with a TMP text child.

    [Header("Navigation")]
    public Button backButton;           // Top-right corner.
    public TMP_Text titleText;          // Shows the computer / category / entry name.

    [Header("Entry Viewer")]
    public Image entryImage;            // THIS just Shows the entry's image if it has one.
    public TMP_Text entryText;          // THIS just Shows the entry's text if it has any.

    [Header("Audio")]
    public AudioSource audioSource;     // Plays the audio recordings.
    public TMP_Text nowPlayingText;     // when audio files are playing 

    [Header("Typing Effect (entry text)")]
    public bool typeEntryText = true;       // Untick to show the text immediatlu.
    public float typingSpeed = 0.02f;       // Seconds between letters.
    public AudioClip typingClip;            // Optional. Loops while text types, stops when it finishes.
    public bool showCursor = true;          // A blinking underscore (_) after the finished text.
    public float cursorBlinkSpeed = 0.5f;   // Seconds between blinks.

    [Header("Boot Screen")]
    public bool showBootScreen = true;      // Typed lines before the Home page.
    public bool bootOnlyFirstTime = true;   // true = only the first time each computer is opened.
    public GameObject bootPanel;            // Dark panel inside ComputerPanel, full size.
    public TMP_Text bootText;               // Text inside the boot panel, aligned top-left.
    public string[] bootLines = { "Starting up...", "Loading {computer}...", "Access granted." };  // Default. Each computer can have its own.
    public float bootLineDelay = 0.4f;      // Pause after each line.

    [Header("Computer Sounds (all optional)")]
    public AudioClip openClip;              // Beep when the computer opens.
    public AudioClip humClip;               // Loops while the computer is open.
    public AudioClip clickClip;             // Every button press.
    public AudioClip closeClip;             // When the player leaves.

    private enum Page { Home, List, View }

    private Page currentPage;
    private ComputerInteractable currentComputer;
    private DatabaseCategory currentCategory;
    private PlayerInteractor currentInteractor;

    private AudioSource typingAudioSource;  // Typing sound only.
    private AudioSource sfxAudioSource;     // Beep, hum, clicks.

    private Coroutine typingRoutine;
    private Coroutine blinkRoutine;
    private bool isTyping;
    private int typedLetters;               // How many real letters the entry text has (no cursor).

    private Coroutine bootRoutine;
    private bool isBooting;
    private HashSet<ComputerInteractable> bootedComputers = new HashSet<ComputerInteractable>();

    private void Awake()
    {
        Instance = this;
        computerPanel.SetActive(false);

        if (bootPanel != null)
        {
            bootPanel.SetActive(false);
        }

        backButton.onClick.AddListener(OnBackPressed);
        backButton.onClick.AddListener(PlayClick);

        // If no Audio Source was assigned, make one so audio recordings still play.
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // A source just for the typing sound.
        typingAudioSource = gameObject.AddComponent<AudioSource>();
        typingAudioSource.playOnAwake = false;
        typingAudioSource.loop = true;
        typingAudioSource.pitch = 1f;               // Always the clip's normal speed.

        // A source for the computer's beep, hum and button clicks.
        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.pitch = 1f;
    }

    // Clicking the mouse while the boot screen or the text is typing skips to the end.
    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        if (isBooting)
        {
            SkipBoot();
        }
        else if (isTyping)
        {
            FinishTyping();
        }
    }

    // opening and closing the computer this called by the ComputerInteractable when the player presses E.

    public void Open(ComputerInteractable computer, PlayerInteractor interactor)
    {
        currentComputer = computer;
        currentInteractor = interactor;

        computerPanel.SetActive(true);

        // Freeze the player and show the mouse cursor so buttons can be clicked.
        currentInteractor.LockPlayer(this, true);

        PlayComputerOn();

        string[] linesToType = GetBootLines(computer);

        bool canBoot = showBootScreen && bootPanel != null && bootText != null
                       && linesToType != null && linesToType.Length > 0;
        bool alreadyBooted = bootOnlyFirstTime && bootedComputers.Contains(computer);

        if (canBoot && !alreadyBooted)
        {
            bootedComputers.Add(computer);
            bootRoutine = StartCoroutine(BootSequence());
        }
        else
        {
            ShowHome();
        }
    }

    // E leaves the computer completely.
    public void OnInteractPressed()
    {
        StopBoot();
        StopTyping();
        StopAudio();

        computerPanel.SetActive(false);
        PlayComputerOff();

        currentInteractor.UnlockPlayer();
    }

    // TheBoot screen

    private IEnumerator BootSequence()
    {
        isBooting = true;

        // Hide every page while the boot lines type out.
        homePanel.SetActive(false);
        listPanel.SetActive(false);
        viewPanel.SetActive(false);
        backButton.gameObject.SetActive(false);
        titleText.text = "";

        bootPanel.SetActive(true);
        bootText.text = "";

        string shown = "";

        foreach (string rawLine in GetBootLines(currentComputer))
        {
            // {computer} string is replaced with the computer's name.
            string line = rawLine.Replace("{computer}", currentComputer.computerName);

            if (typingClip != null)
            {
                typingAudioSource.clip = typingClip;
                typingAudioSource.Play();
            }

            // Type the line one letter at a time.
            foreach (char letter in line)
            {
                shown += letter;
                bootText.text = shown;
                yield return new WaitForSeconds(typingSpeed);
            }

            typingAudioSource.Stop();

            shown += "\n";
            bootText.text = shown;
            yield return new WaitForSeconds(bootLineDelay);
        }

        bootRoutine = null;
        EndBoot();
    }

    // The computer's own boot lines if it has any, otherwise the default lines on this script.
    private string[] GetBootLines(ComputerInteractable computer)
    {
        if (computer.bootLines != null && computer.bootLines.Length > 0)
        {
            return computer.bootLines;
        }

        return bootLines;
    }

    // Mouse click: jump straight to the Home page.
    private void SkipBoot()
    {
        if (bootRoutine != null)
        {
            StopCoroutine(bootRoutine);
            bootRoutine = null;
        }

        EndBoot();
    }

    private void EndBoot()
    {
        isBooting = false;
        typingAudioSource.Stop();
        bootPanel.SetActive(false);

        ShowHome();
    }

    // Cancels the boot screen without showing the Home page (used when leaving the computer).
    private void StopBoot()
    {
        if (bootRoutine != null)
        {
            StopCoroutine(bootRoutine);
            bootRoutine = null;
        }

        isBooting = false;
        typingAudioSource.Stop();

        if (bootPanel != null)
        {
            bootPanel.SetActive(false);
        }
    }

    // ThePages  

    private void ShowHome()
    {
        currentPage = Page.Home;
        StopTyping();
        StopAudio();

        homePanel.SetActive(true);
        listPanel.SetActive(false);
        viewPanel.SetActive(false);
        backButton.gameObject.SetActive(false);     // Nothing to go back to from Home.

        titleText.text = currentComputer.computerName;

        ClearButtons(categoryContainer);

        foreach (DatabaseCategory category in currentComputer.categories)
        {
            // Hidden categories (Case Studies) only appear once unlocked.//this is for only for the USB 
            if (!category.isUnlocked) continue;

            CreateButton(categoryContainer, category.categoryName, () => ShowList(category));
        }
    }

    private void ShowList(DatabaseCategory category)
    {
        currentPage = Page.List;
        currentCategory = category;
        StopTyping();

        homePanel.SetActive(false);
        listPanel.SetActive(true);
        viewPanel.SetActive(false);
        backButton.gameObject.SetActive(true);

        titleText.text = category.categoryName;

        ClearButtons(entryContainer);

        if (category.entries == null) return;

        foreach (DatabaseEntry entry in category.entries)
        {
            CreateButton(entryContainer, entry.title, () => SelectEntry(entry));
        }
    }

    private void SelectEntry(DatabaseEntry entry)
    {
        // Audio entries start playing straight away.
        if (entry.audio != null)
        {
            PlayAudio(entry);
        }

        // Entries with an image or text open the viewer.
        bool hasVisual = entry.image != null || !string.IsNullOrEmpty(entry.text);
        if (hasVisual)
        {
            ShowView(entry);
        }
    }

    private void ShowView(DatabaseEntry entry)
    {
        currentPage = Page.View;

        homePanel.SetActive(false);
        listPanel.SetActive(false);
        viewPanel.SetActive(true);
        backButton.gameObject.SetActive(true);

        titleText.text = entry.title;

        entryImage.gameObject.SetActive(entry.image != null);
        entryImage.sprite = entry.image;
        entryImage.preserveAspect = true;

        bool hasText = !string.IsNullOrEmpty(entry.text);
        entryText.gameObject.SetActive(hasText);

        StopTyping();

        if (hasText)
        {
            if (typeEntryText)
            {
                typingRoutine = StartCoroutine(TypeText(entry.text));
            }
            else
            {
                PrepareText(entry.text);
                EndTyping();
            }
        }
    }

    // Back button: goes back ONE page, it does not leave the computer.
    private void OnBackPressed()
    {
        switch (currentPage)
        {
            case Page.View:
                ShowList(currentCategory);
                break;

            case Page.List:
                ShowHome();
                break;
        }
    }

    // Typing effect and cursor

    // Puts the text in the box with every letter hidden, and counts the letters.
    private void PrepareText(string fullText)
    {
        // The cursor is one extra "_" character at the very end. It stays hidden until typing ends.
        entryText.text = showCursor ? fullText + "_" : fullText;
        entryText.maxVisibleCharacters = 0;
        entryText.ForceMeshUpdate();

        typedLetters = entryText.textInfo.characterCount;

        if (showCursor)
        {
            typedLetters--;     // The last character is the cursor, not a real letter.
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;

        PrepareText(fullText);

        if (typingClip != null)
        {
            typingAudioSource.clip = typingClip;
            typingAudioSource.Play();
        }

        // Reveal one letter at a time.
        for (int i = 1; i <= typedLetters; i++)
        {
            entryText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingRoutine = null;
        EndTyping();
    }

    // Shows the whole text straight away (mouse click).
    private void FinishTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        EndTyping();
    }

    // The text is fully shown: stop the typing sound and start the blinking cursor.
    private void EndTyping()
    {
        typingAudioSource.Stop();
        isTyping = false;

        entryText.maxVisibleCharacters = typedLetters;

        if (showCursor)
        {
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
            }
            blinkRoutine = StartCoroutine(BlinkCursor());
        }
    }

    // Shows and hides the cursor by changing how many characters are visible.
    private IEnumerator BlinkCursor()
    {
        bool cursorVisible = true;

        while (true)
        {
            entryText.maxVisibleCharacters = cursorVisible ? typedLetters + 1 : typedLetters;
            cursorVisible = !cursorVisible;
            yield return new WaitForSeconds(cursorBlinkSpeed);
        }
    }

    // Cancels typing and blinking (leaving the page or closing the computer).
    private void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        typingAudioSource.Stop();
        isTyping = false;
    }

    //  The Audio recordings Category viewpoint: plays the audio clip assigned to the entry, and shows the "Now playing" text.

    private void PlayAudio(DatabaseEntry entry)
    {
        if (audioSource == null) return;

        audioSource.Stop();
        audioSource.clip = entry.audio;
        audioSource.Play();

        if (nowPlayingText != null)
        {
            nowPlayingText.text = "Now playing: " + entry.title;
            nowPlayingText.gameObject.SetActive(true);
        }
    }

    private void StopAudio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (nowPlayingText != null)
        {
            nowPlayingText.gameObject.SetActive(false);
        }
    }

    //  Computer sounds but we'll focus on this in the next prototype 

    // Starts the hum, then plays the opening beep on top of it.
    private void PlayComputerOn()
    {
        if (humClip != null)
        {
            sfxAudioSource.clip = humClip;
            sfxAudioSource.loop = true;
            sfxAudioSource.Play();
        }

        if (openClip != null)
        {
            sfxAudioSource.PlayOneShot(openClip);
        }
    }

    // Stops the hum, then plays the closing sound.
    private void PlayComputerOff()
    {
        sfxAudioSource.Stop();
        sfxAudioSource.loop = false;
        sfxAudioSource.clip = null;

        if (closeClip != null)
        {
            sfxAudioSource.PlayOneShot(closeClip);
        }
    }

    private void PlayClick()
    {
        if (clickClip != null)
        {
            sfxAudioSource.PlayOneShot(clickClip);
        }
    }

    //  Button helpers

    private void CreateButton(Transform parent, string label, UnityAction onClick)
    {
        Button button = Instantiate(buttonPrefab, parent);

        // Works with a TextMeshPro button label or a legacy UI Text label.
        // 'true' also finds the label if it is on a hidden child.
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        Text legacyText = button.GetComponentInChildren<Text>(true);

        if (tmpText != null)
        {
            tmpText.text = label;
        }
        else if (legacyText != null)
        {
            legacyText.text = label;
        }
        else
        {
            Debug.LogWarning("The Button Prefab has no text object inside it, so the button has no title.");
        }

        button.onClick.AddListener(onClick);
        button.onClick.AddListener(PlayClick);
    }

    private void ClearButtons(Transform container)
    {
        foreach (Transform child in container)
        {
            
            // (Destroy only happens at the end of the frame).
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}