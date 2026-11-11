using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Akhona Khoali

// Belongs to any empty object with a Collider that has "Is Trigger" ticked. Use the SAME script on
// every zone - each zone just gets its own Music Clip. When the player walks in, that zone's music
// fades in (and whatever was playing fades out). When they walk out, it fades out, or the music of
// the zone they are still standing in (overlapping zones) takes over.
// No manager object is needed: the first zone creates its own audio player.
[RequireComponent(typeof(Collider))]
public class MusicZone : MonoBehaviour
{
    [Header("Music")]
    public AudioClip musicClip;
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public float fadeInSeconds = 2f;
    public float fadeOutSeconds = 2f;

    [Header("When the player leaves")]
    [Tooltip("On: the music fades out when the player leaves. Off: it keeps playing until another zone replaces it.")]
    public bool stopOnExit = true;

    // Shared by every zone 
    private static readonly List<MusicZone> activeZones = new List<MusicZone>();
    private static MusicPlayer player;

    private void Reset()
    {
        // Makes the collider a trigger the moment the script is added.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (!activeZones.Contains(this))
        {
            activeZones.Add(this);
        }

        Play(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        activeZones.Remove(this);

        // Still inside another zone: go back to that one's music.
        if (activeZones.Count > 0)
        {
            Play(activeZones[activeZones.Count - 1]);
            return;
        }

        if (stopOnExit && player != null)
        {
            player.FadeOut(fadeOutSeconds);
        }
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    private static bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerInteractor>() != null;
    }

    private static void Play(MusicZone zone)
    {
        if (zone.musicClip == null) return;

        if (player == null)
        {
            GameObject host = new GameObject("MusicZonePlayer");
            player = host.AddComponent<MusicPlayer>();
        }

        player.CrossFadeTo(zone.musicClip, zone.volume, zone.fadeInSeconds, zone.fadeOutSeconds);
    }

    // Two AudioSources swapped back and forth so one fades out while the other fades in.
    private class MusicPlayer : MonoBehaviour
    {
        private AudioSource[] sources;
        private int current;
        private Coroutine[] fades;

        private void Awake()
        {
            sources = new AudioSource[2];
            fades = new Coroutine[2];

            for (int i = 0; i < 2; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].loop = true;
                sources[i].volume = 0f;
            }
        }

        public void CrossFadeTo(AudioClip clip, float volume, float fadeIn, float fadeOut)
        {
            // Already playing this clip: just make sure it's audible at the right level.
            if (sources[current].clip == clip && sources[current].isPlaying)
            {
                StartFade(current, volume, fadeIn);
                return;
            }

            int old = current;
            current = 1 - current;

            StartFade(old, 0f, fadeOut);

            sources[current].clip = clip;
            sources[current].volume = 0f;
            sources[current].Play();
            StartFade(current, volume, fadeIn);
        }

        public void FadeOut(float seconds)
        {
            StartFade(current, 0f, seconds);
        }

        private void StartFade(int index, float target, float seconds)
        {
            if (fades[index] != null)
            {
                StopCoroutine(fades[index]);
            }

            fades[index] = StartCoroutine(Fade(index, target, seconds));
        }

        // Unscaled time, so it still fades while the game is paused or a screen is open.
        private IEnumerator Fade(int index, float target, float seconds)
        {
            AudioSource source = sources[index];
            float start = source.volume;
            float time = 0f;

            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, target, time / seconds);
                yield return null;
            }

            source.volume = target;

            if (target <= 0f)
            {
                source.Stop();
            }

            fades[index] = null;
        }
    }
}
