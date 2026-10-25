using UnityEngine;
//Akhona khoali

// For a task that is "go to a place", e.g. "Go to Franklin's office".
// Belongs to an empty object with a Box Collider sized over the area (it switches the collider
// to "Is Trigger" for you). The first time the player walks in, it reports the Clue Id, which ticks
// that task on the checklist (and the notebook, if the id is also a notebook clue).
[RequireComponent(typeof(Collider))]
public class ClueZone : MonoBehaviour
{
    public string clueId = "";          // The same id you typed on the checklist task.
    public bool onlyOnce = true;

    private bool done;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (done && onlyOnce) return;

        // Only the player counts, not NPCs or thrown objects.
        if (other.GetComponentInParent<PlayerInteractor>() == null) return;

        done = true;
        ClueTrigger.DiscoverClue(clueId);
    }
}