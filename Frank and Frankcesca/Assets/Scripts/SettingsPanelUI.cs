using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
//Akhona Khoali

// Settings panel Drag in a Slider for Volume and one for
// Brightness. Both save immediately as the player drags them (via PlayerPrefs), so there's no
// "Apply" button and the choice is remembered the next time the game is launched.
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Volume")]
    public Slider volumeSlider;
    public AudioMixer audioMixer;              // Optional. Needs an exposed float parameter (see below).
    public string volumeParameter = "MasterVolume";

    [Header("Brightness")]
    public Slider brightnessSlider;
    [Tooltip("A full-screen black Image, above everything else in the Canvas. Its alpha is lowered as brightness goes up. Simple and works with any render pipeline - swap for post-processing Exposure later if you add that.")]
    public Image brightnessOverlay;

    private const string VolumeKey = "Settings_Volume";
    private const string BrightnessKey = "Settings_Brightness";

    private void Awake()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 0.75f);
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 0.5f);

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        // Apply the saved values straight away, so the game starts at the right volume/brightness
        // even before the player opens Settings.
        SetVolume(savedVolume);
        SetBrightness(savedBrightness);
    }

    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (audioMixer != null)
        {
            // Sliders are linear (0-1), an Audio Mixer's exposed parameter wants decibels.
            // To expose one: select the mixer's Master group, right-click its Volume in the
            // Inspector -> "Expose to script", then rename it to match Volume Parameter above.
            float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            audioMixer.SetFloat(volumeParameter, dB);
        }
        else
        {
            // No mixer assigned: fall back to the overall listener volume. Fine for a quick setup,
            // but a mixer lets you add separate music/SFX sliders later without changing this script.
            AudioListener.volume = value;
        }

        PlayerPrefs.SetFloat(VolumeKey, value);
    }

    public void SetBrightness(float value)
    {
        value = Mathf.Clamp01(value);

        if (brightnessOverlay != null)
        {
            // At value = 1 the overlay is fully transparent (brightest). At 0 it's noticeably dark.
            Color c = brightnessOverlay.color;
            c.a = Mathf.Lerp(0.6f, 0f, value);
            brightnessOverlay.color = c;
        }

        PlayerPrefs.SetFloat(BrightnessKey, value);
    }
}