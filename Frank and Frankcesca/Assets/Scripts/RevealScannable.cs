using UnityEngine;
using UnityEngine.Events;
//Akhona Khoali
//placed on a GameObject that should be revealed when the scanner beam touches it.

public class RevealScannable : MonoBehaviour, IScannable
{
    [Header("Shown while the beam touches it")]
    public GameObject[] revealObjects;          // e.g. the fingerprint quad. They are hidden at the start.
    public ParticleSystem[] particleEffects;    // Play while lit, stop when the beam leaves.
    public bool stayRevealed = false;           // true = the reveal objects stay visible once found.

    [Header("Discovery")]
    public AudioClip discoveredClip;            // Optional sound the first time it is found.
    public UnityEvent onFirstScanned;           // Later: tick the checklist, show the notebook notification...

    public bool HasBeenScanned { get; private set; }

    private void Awake()
    {
        // Everything hidden until the beam finds it.
        foreach (GameObject obj in revealObjects)
        {
            if (obj != null) obj.SetActive(false);
        }

        foreach (ParticleSystem particles in particleEffects)
        {
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // IScannable 

    // Called by PlayerScanner the moment the beam starts touching this object.
    public void OnScanStart()
    {
        foreach (GameObject obj in revealObjects)
        {
            if (obj != null) obj.SetActive(true);
        }

        foreach (ParticleSystem particles in particleEffects)
        {
            if (particles != null) particles.Play();
        }

        if (!HasBeenScanned)
        {
            HasBeenScanned = true;

            if (discoveredClip != null)
            {
                AudioSource.PlayClipAtPoint(discoveredClip, transform.position);
            }

            onFirstScanned.Invoke();
        }
    }

    // Called by PlayerScanner when the beam stops touching this object.
    public void OnScanEnd()
    {
        foreach (ParticleSystem particles in particleEffects)
        {
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (!stayRevealed)
        {
            foreach (GameObject obj in revealObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }
}