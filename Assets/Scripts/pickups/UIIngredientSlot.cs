using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIIngredientSlot : MonoBehaviour
{
    public TextMeshProUGUI ingredientText;   // must be TextMeshProUGUI
    public Image checkmarkIcon;

    public void SetUnlocked(bool unlocked)
    {
        checkmarkIcon.gameObject.SetActive(unlocked);
    }
}
