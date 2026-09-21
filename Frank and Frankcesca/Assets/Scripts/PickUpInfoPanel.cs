using TMPro;
using UnityEngine;
//Akhona Khoali
//this script is used to show the pickup information panel when the player picks up an item.
// It has a reference to the panel, title text, and description text.
// It has methods to show and hide the panel, and to set the title and description text.
// canvas

public class PickupInfoPanel : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    private void Awake()
    {
        Hide();
    }

    public void Show(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}