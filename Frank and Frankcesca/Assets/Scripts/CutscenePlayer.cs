using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

// Put this on your UI manager object (an active object, NOT the cutscene panel).
// Plays a video full screen. It freezes the player while it plays (like the dialogue and the
// computer), lets the player press E to skip, and fades out at the end.
// The intro plays by itself when the game starts. Other scripts can play more cutscenes with:
//   CutscenePlayer.Instance.Play(someVideoClip);
// It adds its own VideoPlayer and Audio Sources, so you only set up the UI.
public class CutscenePlayer : MonoBehaviour, IInteractionScreen
{
    public static CutscenePlayer Instance { get; private set; }

    [Header("Intro Cutscene")]
    public VideoClip introClip;
    public bool playIntroOnStart = true;

    [Header("UI References")]
    public GameObject cutscenePanel;        // Black full-screen panel. Put it LAST under the Canvas so it draws on top.
    public RawImage screenImage;            // Inside the panel. Shows the video.
    public GameObject skipHint;             // Optional text like "Press E to skip".

    [Header("Playback")]
    [Range(0f, 1f)]
    public float volume = 1f;               // Volume of the video's own sound.
    public AudioClip extraAudio;            // Optional music or narration, for videos with no sound.
    public bool allowSkip = true;
    public float skipLockSeconds = 2f;      // Skipping only works after this many seconds.
    public float fadeOutSeconds = 1f;       // Fade from the video into the game.

    [Header("Events")]
    public UnityEvent onFinished;           // Anything that should happen when the cutscene ends.

    private VideoPlayer videoPlayer;
    private AudioSource videoAudioSource;
    private AudioSource extraAudioSource;
    private CanvasGroup panelGroup;
    private RenderTexture renderTexture;
    private PlayerInteractor interactor;

    private bool isPlaying;
    private bool isEnding;
    private float startTime;

    private void Awake()
    {
        Instance = this;

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        videoAudioSource = gameObject.AddComponent<AudioSource>();
        videoAudioSource.playOnAwake = false;

        extraAudioSource = gameObject.AddComponent<AudioSource>();
        extraAudioSource.playOnAwake = false;
        extraAudioSource.loop = false;
        extraAudioSource.pitch = 1f;

        // The fade-out uses a CanvasGroup, so add one if the panel doesn't have it.
        panelGroup = cutscenePanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
        {
            panelGroup = cutscenePanel.AddComponent<CanvasGroup>();
        }

        // A RawImage with no picture draws white, so keep it hidden until the video is ready.
        screenImage.gameObject.SetActive(false);

        if (skipHint != null)
        {
            skipHint.SetActive(false);
        }

        // If the intro plays at the start, keep the panel black from the very first frame,
        // so the player never sees the level flash before the video.
        bool introWillPlay = playIntroOnStart && introClip != null;
        cutscenePanel.SetActive(introWillPlay);
        panelGroup.alpha = 1f;
    }

    private void Start()
    {
        if (playIntroOnStart && introClip != null)
        {
            Play(introClip);
        }
    }

    // ----- Playing -----

    public void Play(VideoClip clip)
    {
        if (isPlaying || clip == null) return;

        isPlaying = true;
        isEnding = false;
        startTime = Time.time;

        // Freeze the player. false = keep the cursor hidden.
        interactor = FindFirstObjectByType<PlayerInteractor>();
        if (interactor != null)
        {
            interactor.LockPlayer(this, false);
        }

        cutscenePanel.SetActive(true);
        panelGroup.alpha = 1f;
        screenImage.gameObject.SetActive(false);

        // A texture the same size as the video, shown on the RawImage.
        renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        screenImage.texture = renderTexture;

        // If the video is not 16:9, an Aspect Ratio Fitter on the RawImage keeps it from stretching.
        AspectRatioFitter fitter = screenImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.aspectRatio = (float)clip.width / clip.height;
        }

        // The video's own sound (Procreate videos may have none).
        if (clip.audioTrackCount > 0)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
            videoAudioSource.volume = volume;
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;

        // Load the video first, then play it, so it starts without stuttering.
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        videoPlayer.prepareCompleted -= OnPrepared;

        screenImage.gameObject.SetActive(true);
        videoPlayer.Play();
        startTime = Time.time;

        if (extraAudio != null)
        {
            extraAudioSource.clip = extraAudio;
            extraAudioSource.volume = volume;
            extraAudioSource.Play();
        }

        if (skipHint != null && allowSkip)
        {
            StartCoroutine(ShowSkipHintLater());
        }
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        EndCutscene();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("CutscenePlayer: the video could not play. " + message +
                       " Export it as an MP4 (H.264) and import it again.");
        EndCutscene();
    }

    // E skips the cutscene (after a short lock, so it isn't skipped by accident).
    public void OnInteractPressed()
    {
        if (!isPlaying || !allowSkip) return;
        if (Time.time - startTime < skipLockSeconds) return;

        EndCutscene();
    }

    private IEnumerator ShowSkipHintLater()
    {
        yield return new WaitForSeconds(skipLockSeconds);

        if (isPlaying && !isEnding)
        {
            skipHint.SetActive(true);
        }
    }

    // ----- Ending -----

    private void EndCutscene()
    {
        if (!isPlaying || isEnding) return;

        isEnding = true;

        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;

        videoPlayer.Stop();
        videoAudioSource.Stop();
        extraAudioSource.Stop();

        if (skipHint != null)
        {
            skipHint.SetActive(false);
        }

        StartCoroutine(FadeOutAndFinish());
    }

    private IEnumerator FadeOutAndFinish()
    {
        float time = 0f;

        while (time < fadeOutSeconds)
        {
            time += Time.deltaTime;
            panelGroup.alpha = 1f - Mathf.Clamp01(time / fadeOutSeconds);
            yield return null;
        }

        panelGroup.alpha = 0f;
        cutscenePanel.SetActive(false);
        screenImage.gameObject.SetActive(false);
        screenImage.texture = null;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        isPlaying = false;
        isEnding = false;

        // Give the player control back.
        if (interactor != null)
        {
            interactor.UnlockPlayer();
        }

        onFinished.Invoke();
    }
}