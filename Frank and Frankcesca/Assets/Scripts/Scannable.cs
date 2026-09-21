using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//AKhona Khoali
// ths script is attached to objects that can be scanned by the player. It handles revealing objects, playing particle effects, and triggering events when first scanned.
public class Scannable : MonoBehaviour
{
    // Every active Scannable in the scene. PlayerScanner loops through this list.
    public static readonly List<Scannable> All = new List<Scannable>();

    [Header("Shown while the beam touches it")]
    public GameObject[] revealObjects;          // e.g. the fingerprint quad. They are hidden at the start.
    public ParticleSystem[] particleEffects;    // Play while lit, stop when the beam leaves.
    public bool stayRevealed = false;           // true = the reveal objects stay visible once found.

    [Header("Discovery")]
    public AudioClip discoveredClip;            // Optional sound the first time it is found.
    public UnityEvent onFirstScanned;           // Later: tick the checklist, show the notebook notification...

    [Header("Detection")]
    public float fallbackRadius = 0.3f;         // Used only if the object has no Collider.

    public bool HasBeenScanned { get; private set; }

    private Collider[] colliders;
    private bool isLit;

    // Makes sure the list is empty every time Play mode starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetList()
    {
        All.Clear();
    }

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();

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

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    // Called by PlayerScanner: true while the beam is touching this object.
    public void SetLit(bool lit)
    {
        if (lit == isLit) return;
        isLit = lit;

        if (lit)
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
        else
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

    // The box that surrounds all of this object's colliders.
    public Bounds GetBounds()
    {
        bool found = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one * fallbackRadius * 2f);

        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;

            if (!found)
            {
                bounds = c.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        return bounds;
    }

    // True if this collider is part of this Scannable (used to check the beam's line of sight).
    public bool OwnsCollider(Collider collider)
    {
        return collider.GetComponentInParent<Scannable>() == this;
    }
}