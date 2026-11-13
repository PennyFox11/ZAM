using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
//Akhona Khoali
// Plays the ending video for the chosen option, then loads the next scene (the main menu).
// Goes on the EndingCutscenePanel, which sits directly under the Canvas (NOT inside the Dialogue Panel).
public class EndingCutscene : MonoBehaviour
{
    public static EndingCutscene Instance { get; private set; }

    [Header("References")]
    public GameObject cutscenePanel;    // The full-screen black panel (usually this same object).
    public RawImage screen;             // The Raw Image that shows the video.
    public VideoPlayer videoPlayer;

    [Header("Options")]
    public bool allowSkip = true;       // Space skips the video.

    private string nextScene;
    private bool loading;
    private bool playing;

    private void Awake()
    {
        Instance = this;

        if (cutscenePanel == null) cutscenePanel = gameObject;
        cutscenePanel.SetActive(false);
    }

    public void Play(DialogueChoice choice)
    {
        EndingResult.Chosen = choice.endingId;
        nextScene = choice.sceneName;
        loading = false;

        // This script sits on the panel, which is hidden at the start. A hidden object can't run
        // coroutines, so it has to be switched on BEFORE the coroutine starts.
        cutscenePanel.SetActive(true);

        StartCoroutine(Run(choice.endingVideo));
    }

    private IEnumerator Run(VideoClip clip)
    {
        cutscenePanel.SetActive(true);
        playing = true;

        // No video assigned: just go straight to the next scene.
        if (clip == null)
        {
            GoNext();
            yield break;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        screen.texture = videoPlayer.texture;
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        GoNext();
    }

    private void Update()
    {
        if (!playing || !allowSkip) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GoNext();
        }
    }

    private void GoNext()
    {
        if (loading) return;
        loading = true;
        playing = false;

        if (videoPlayer != null) videoPlayer.Stop();

        // The main menu needs the mouse.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(nextScene);
    }
}