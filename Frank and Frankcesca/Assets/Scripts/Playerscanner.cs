using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
//Akhona Khoali

//This script is attached to the Player, and uses a Spot Light to scan for scannable objects in front of the player.
// The cone uses the Spot Light's own Range and Spot Angle, so what you see is what it hits.
public class PlayerScanner : MonoBehaviour
{
    [Header("Scanner Light")]
    public Light scannerLight;              // A Spot Light, child of the camera.
    public bool startsOn = false;

    [Header("Raycast Settings")]
    public LayerMask scanMask = ~0;         // ~0 means "everything".
    public int ringCount = 3;               // More rings and rays = the beam catches smaller objects...
    public int raysPerRing = 8;             // ...but costs a little more.
    public float blockTolerance = 0.1f;     // Surfaces this close behind a target don't block it (floors, tables).

    [Header("Audio (optional)")]
    public AudioSource audioSource;         // If empty, one is created for you.
    public AudioClip toggleOnClip;          // Plays once when the scanner turns on.
    public AudioClip loopClip;              // Loops the whole time the scanner is on (a hum).
    public AudioClip toggleOffClip;         // Plays once when the scanner turns off.
    [Range(0f, 1f)]
    public float volume = 1f;

    public bool IsOn { get; private set; }

    private PlayerInteractor interactor;
    private RaycastHit[] hitBuffer = new RaycastHit[16];
    private HashSet<IScannable> litNow = new HashSet<IScannable>();     // Touched by the beam this frame.
    private HashSet<IScannable> litBefore = new HashSet<IScannable>();  // Touched by the beam last frame.
    private int lastLitCount = -1;                                      // Only used by the debug lines.
    private string lastCentreName = "";                                 // Only used by the debug lines.

    private void Awake()
    {
        interactor = GetComponent<PlayerInteractor>();

        if (scannerLight == null)
        {
            Debug.LogWarning("PlayerScanner: the Scanner Light slot is empty. Drag the Spot Light into it.");
        }

        // If no Audio Source was assigned it makes one so the scanner sounds still play.
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        audioSource.volume = volume;
        audioSource.pitch = 1f;                 // Always the clip's normal speed.

        SetScanner(startsOn, false);
    }

    
    // Works the same way as PlayerInteractor's OnInteract.
    public void OnScanner(InputAction.CallbackContext context)
    {
        // TEMPORARY DEBUG LINE: delete once the scanner works.
        Debug.Log("Scanner phase: " + context.phase + ", light: " + scannerLight);

        // Only react once, at the moment the key is pressed.
        if (!context.performed) return;

        // Don't toggle while a dialogue / safe / computer screen is open.
        if (interactor != null && interactor.InputLocked) return;

        SetScanner(!IsOn, true);
    }

    // LateUpdate runs after the camera has moved this frame, so the beam is always up to date.
    private void LateUpdate()
    {
        if (!IsOn || scannerLight == null) return;

        FireBeam();
    }

    // Fires the cone of rays and works out what the beam started / stopped touching.
    private void FireBeam()
    {
        Transform lightTransform = scannerLight.transform;
        Vector3 origin = lightTransform.position;
        Vector3 forward = lightTransform.forward;
        float range = scannerLight.range;
        float halfAngle = scannerLight.spotAngle * 0.5f;

        litNow.Clear();

        // The centre ray.
        CastRay(origin, forward, range);

        // TEMPORARY DEBUG LINES: delete once the scanner works.
        string centreName = "nothing";
        if (Physics.Raycast(origin, forward, out RaycastHit centreHit, range, scanMask, QueryTriggerInteraction.Collide))
        {
            bool found = centreHit.collider.GetComponentInParent<IScannable>() != null;
            centreName = centreHit.collider.name + " (scannable found: " + found + ")";
        }
        if (centreName != lastCentreName)
        {
            Debug.Log("Beam centre is pointing at: " + centreName);
            lastCentreName = centreName;
        }

        // Rings of rays around the centre, out to the edge of the beam.
        for (int ring = 1; ring <= ringCount; ring++)
        {
            float ringAngle = halfAngle * ring / ringCount;

            // Tilt away from the centre, then spin around the beam to spread the rays round the ring.
            Vector3 tilted = Quaternion.AngleAxis(ringAngle, lightTransform.up) * forward;

            for (int i = 0; i < raysPerRing; i++)
            {
                float spin = 360f * i / raysPerRing;
                Vector3 direction = Quaternion.AngleAxis(spin, forward) * tilted;
                CastRay(origin, direction, range);
            }
        }

        // TEMPORARY DEBUG LINES: delete once the scanner works.
        if (litNow.Count != lastLitCount)
        {
            Debug.Log("Beam is touching " + litNow.Count + " scannable(s)");
            lastLitCount = litNow.Count;
        }

        // Anything touched now that wasn't before: the beam just found it.
        foreach (IScannable scannable in litNow)
        {
            if (!litBefore.Contains(scannable))
            {
                scannable.OnScanStart();
            }
        }

        // Anything touched before that isn't now: the beam moved away.
        foreach (IScannable scannable in litBefore)
        {
            if (!litNow.Contains(scannable))
            {
                scannable.OnScanEnd();
            }
        }

        // This frame's list becomes "last frame's" list.
        HashSet<IScannable> temp = litBefore;
        litBefore = litNow;
        litNow = temp;
    }

    // Fires one ray and adds every scannable it reaches to litNow.
    private void CastRay(Vector3 origin, Vector3 direction, float range)
    {
        // QueryTriggerInteraction.Collide lets the ray hit trigger colliders too,
        // so flat things like the oil trail can use "Is Trigger" colliders.
        int count = Physics.RaycastNonAlloc(origin, direction, hitBuffer, range, scanMask, QueryTriggerInteraction.Collide);

        // The nearest SOLID surface (not a trigger) blocks the beam.
        float blockDistance = range;
        for (int i = 0; i < count; i++)
        {
            if (!hitBuffer[i].collider.isTrigger && hitBuffer[i].distance < blockDistance)
            {
                blockDistance = hitBuffer[i].distance;
            }
        }

        // Anything scannable at or in front of that surface is touched by the beam.
        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].distance > blockDistance + blockTolerance) continue;

            // GetComponentInParent means the collider can be on a child object.
            IScannable scannable = hitBuffer[i].collider.GetComponentInParent<IScannable>();

            if (scannable != null)
            {
                litNow.Add(scannable);
            }
        }
    }

    private void SetScanner(bool on, bool playSound)
    {
        IsOn = on;

        if (scannerLight != null)
        {
            scannerLight.enabled = on;
        }

        if (!on)
        {
            ClearLit();
        }

        PlaySounds(on, playSound);
    }

    // Turn on: click, then the hum loops. Turn off: the hum stops, then the off sound plays.
    private void PlaySounds(bool on, bool playClicks)
    {
        if (audioSource == null) return;

        if (on)
        {
            if (playClicks && toggleOnClip != null)
            {
                audioSource.PlayOneShot(toggleOnClip);
            }

            // PlayOneShot plays on top of the looping clip, so both are heard together.
            if (loopClip != null)
            {
                audioSource.clip = loopClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            // Stops the hum.
            audioSource.Stop();
            audioSource.loop = false;

            if (playClicks && toggleOffClip != null)
            {
                audioSource.PlayOneShot(toggleOffClip);
            }
        }
    }

    // Switches everything off when the scanner is turned off.
    private void ClearLit()
    {
        foreach (IScannable scannable in litBefore)
        {
            scannable.OnScanEnd();
        }

        litBefore.Clear();
        litNow.Clear();
    }
}