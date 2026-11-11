using UnityEngine;
using UnityEngine.UI;

// Put this on the menu panel. Every Image and RawImage under it (the panel, frame and buttons)
// gets the glitch material. The glitch happens in random bursts, so nothing needs animating.
// Button text (TextMeshPro) is not affected yet.
public class UIGlitch : MonoBehaviour
{
    [Header("Material")]
    public Material glitchMaterial;               // A material using the "UI/Glitch" shader.

    [Header("Intensity")]
    [Range(0f, 3f)] public float intensity = 1f;
    public bool randomGlitchFrequency = true;
    [Range(0f, 1f)] public float glitchFrequency = 0.5f;   // Higher = more bursts.

    [Header("UV shifting")]
    public bool performUVShifting = true;
    [Range(0f, 1f)] public float shiftAmount = 1f;

    [Header("Colors")]
    public bool performColorShifting = true;
    public Color tintColor = new Color(0.5f, 0.5f, 0f, 1f);

    private Material runtimeMaterial;
    private bool bursting;
    private float burstEndTime;
    private float nextBurstTime;

    private void Awake()
    {
        if (glitchMaterial == null)
        {
            Shader shader = Shader.Find("UI/Glitch");
            if (shader == null)
            {
                Debug.LogError("UIGlitch: assign a material that uses the UI/Glitch shader.");
                enabled = false;
                return;
            }
            glitchMaterial = new Material(shader);
        }

        // One shared copy, so every graphic glitches at the same moment.
        runtimeMaterial = new Material(glitchMaterial);

        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            image.material = runtimeMaterial;
        }

        foreach (RawImage rawImage in GetComponentsInChildren<RawImage>(true))
        {
            rawImage.material = runtimeMaterial;
        }

        ScheduleNextBurst(Time.unscaledTime);
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        if (bursting && now >= burstEndTime)
        {
            bursting = false;
            ScheduleNextBurst(now);
        }
        else if (!bursting && now >= nextBurstTime)
        {
            bursting = true;
            burstEndTime = now + Random.Range(0.05f, 0.25f);
        }

        runtimeMaterial.SetFloat("_Intensity", bursting ? intensity : 0f);
        runtimeMaterial.SetFloat("_ShiftAmount", performUVShifting ? shiftAmount : 0f);
        runtimeMaterial.SetFloat("_ColorShift", performColorShifting ? 1f : 0f);
        runtimeMaterial.SetColor("_GlitchTint", tintColor);
        runtimeMaterial.SetFloat("_GlitchTime", now);
    }

    private void ScheduleNextBurst(float now)
    {
        // Bursts per second, from 0.2 (rare) up to 5 (frequent).
        float rate = Mathf.Lerp(0.2f, 5f, glitchFrequency);
        float interval = 1f / rate;

        if (randomGlitchFrequency)
        {
            interval *= Random.Range(0.3f, 1.7f);
        }

        nextBurstTime = now + interval;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
