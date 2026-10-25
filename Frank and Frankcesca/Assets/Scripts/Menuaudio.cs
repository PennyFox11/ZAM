using System.Collections;
using UnityEngine;
//Akhona Khoali

//belongs to the canvas, so it plays the menu music and button hover / click sounds.
// It plays the menu music on a loop, and the hover / click sounds for every menu button.
// The buttons already have MenuButtonHoverEffect on them, which calls this on its own, so
// there is nothing to wire per button. Can be put in the game scene too if you want the
// pause menu's buttons to make the same sounds.
public class MenuAudio : MonoBehaviour
{
    public static MenuAudio Instance { get; private set; }

    [Header("Music")]
    public AudioClip musicClip;                 // Loops for as long as the menu is open.
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    public float musicFadeInSeconds = 2f;

    [Header("Button Sounds (all optional)")]
    public AudioClip hoverClip;                 // Mouse moves onto a button.
    public AudioClip clickClip;                 // Button is pressed.
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.pitch = 1f;
    }

    private void Start()
    {
        if (musicClip == null) return;

        musicSource.clip = musicClip;
        musicSource.Play();

        fadeRoutine = StartCoroutine(FadeMusic(musicVolume, musicFadeInSeconds));
    }

    public void PlayHover()
    {
        PlaySfx(hoverClip);
    }

    public void PlayClick()
    {
        PlaySfx(clickClip);
    }

    // Can be hooked to a button's OnClick (e.g. New Game) so the music fades out before the scene loads.
    public void FadeOutMusic(float seconds)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeMusic(0f, seconds));
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    // Unscaled time, so it also works while the game is paused behind the pause menu.
    private IEnumerator FadeMusic(float targetVolume, float seconds)
    {
        float startVolume = musicSource.volume;
        float time = 0f;

        while (time < seconds)
        {
            time += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, time / seconds);
            yield return null;
        }

        musicSource.volume = targetVolume;
        fadeRoutine = null;
    }
}