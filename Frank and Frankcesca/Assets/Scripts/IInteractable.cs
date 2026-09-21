using UnityEngine;
//Akhona Khoali
//The interface is the best methond for havinf multiple objects using the same button for differnt functions
//This is better than the method I was using for the last project
// Every object the player can interact with using E (pickups, notes, doors, the safe,
// computers, NPCs...) implements this interface.
// The PlayerInteractor only ever talks to this interface, so it never needs to know
// what kind of object it is looking at.
public interface IInteractable
{
    // The text shown on screen while the player looks at this object.
    // For example: "Pick up Toothbrush" or "Open door".
    string GetPrompt();

    // Called once when the player presses E while looking at this object.
    // The interactor is passed in so the object can talk back to the player
    // (for example, a pickup asks the interactor to hold it).
    void Interact(PlayerInteractor interactor);
}

public interface IInteractionScreen
{
    void OnInteractPressed();
}