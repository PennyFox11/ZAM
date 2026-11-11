using TMPro;
using UnityEngine;
using UnityEngine.UI;
//akhona khoali

// belongs to the ControlHints prefab, which is a vertical layout group of these rows.
public class ControlHintRow : MonoBehaviour
{
    public TMP_Text labelText;
    public Image iconImage;

    public void Setup(Sprite icon, string label)
    {
        if (labelText != null)
        {
            labelText.text = label;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.gameObject.SetActive(icon != null);   // No icon given: the words show alone.
        }
    }
}