using TMPro;
using UnityEngine;
//Akhona Khoali
// canvas
// interact prompt 
public class InteractionPromptUI : MonoBehaviour
{
    public TMP_Text promptText;

    private void Awake()
    {
        Hide();
    }

    // Shows for example "[E] Pick up Note".
    //it strings the name from the object title/lable under the interactable
    public void Show(string message)
    {
        promptText.text = "[E] " + message;
        promptText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        promptText.gameObject.SetActive(false);
    }
}