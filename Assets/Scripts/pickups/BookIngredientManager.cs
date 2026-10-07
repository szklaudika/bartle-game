using UnityEngine;
using System.Collections.Generic;
//using UnityTutorial.Logging;

public class BookIngredientManager : MonoBehaviour
{
    [System.Serializable]
    public class Ingredient
    {
        public string ingredientName;
        public bool unlocked;
        public int pageIndex = 0;
        [HideInInspector] public UIIngredientSlot uiSlot;
    }

    [Header("Ingredient List")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("UI")]
    public UIIngredientSlot slotPrefab;
    public Book book;

    [Header("Win UI")]
    public GameObject winPanel; // Assign this in inspector

    private Dictionary<int, List<UIIngredientSlot>> pageSlots = new Dictionary<int, List<UIIngredientSlot>>();

    void Start()
    {
        if (!book)
            book = GetComponent<Book>();

        if (winPanel != null)
            winPanel.SetActive(false); // Hide at start

        GenerateUI();
        UpdatePageVisibility();

        if (book != null)
            book.OnFlip.AddListener(UpdatePageVisibility);
    }

    void GenerateUI()
    {
        pageSlots.Clear();

        foreach (var ing in ingredients)
        {
            UIIngredientSlot slot = Instantiate(slotPrefab);
            slot.ingredientText.text = ing.ingredientName;

            // ✔ Zarovnanie textu doľava
            slot.ingredientText.alignment = TMPro.TextAlignmentOptions.Left;
            // 🔹 Make text black
            slot.ingredientText.color = Color.black;

            slot.SetUnlocked(ing.unlocked);
            ing.uiSlot = slot;

            Transform parent = GetParentForPage(ing.pageIndex);
            slot.transform.SetParent(parent, false);

            if (!pageSlots.ContainsKey(ing.pageIndex))
                pageSlots[ing.pageIndex] = new List<UIIngredientSlot>();
            pageSlots[ing.pageIndex].Add(slot);
        }

        // Auto vertical spacing
        foreach (var page in pageSlots)
        {
            float yOffset = -20f;
            float spacing = -80f;

            foreach (var slot in page.Value)
            {
                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, yOffset);
                yOffset += spacing;
            }
        }
    }

    Transform GetParentForPage(int pageIndex)
    {
        if (book.currentPage == pageIndex)
            return book.Right.rectTransform;
        else if (book.currentPage - 1 == pageIndex)
            return book.Left.rectTransform;
        else if (book.currentPage + 1 == pageIndex)
            return book.RightNext.rectTransform;
        else if (book.currentPage - 2 == pageIndex)
            return book.LeftNext.rectTransform;

        GameObject placeholder = new GameObject("Page" + pageIndex);
        placeholder.transform.SetParent(book.BookPanel, false);
        placeholder.SetActive(false);
        return placeholder.transform;
    }

    void UpdatePageVisibility()
    {
        int[] visiblePages = new int[]
        {
            book.currentPage - 1,
            book.currentPage,
            book.currentPage - 2,
            book.currentPage + 1
        };

        foreach (var kvp in pageSlots)
        {
            bool isVisible = System.Array.Exists(visiblePages, p => p == kvp.Key);
            foreach (var slot in kvp.Value)
                slot.gameObject.SetActive(isVisible);
        }

        foreach (int page in visiblePages)
        {
            if (!pageSlots.ContainsKey(page)) continue;
            Transform parent = GetParentForPage(page);
            foreach (var slot in pageSlots[page])
                slot.transform.SetParent(parent, false);
        }
    }

    public void UnlockIngredient(string ingredientName)
    {
        var ing = ingredients.Find(i => i.ingredientName == ingredientName);
        if (ing != null)
        {
            ing.unlocked = true;

            if (ing.uiSlot != null)
                ing.uiSlot.SetUnlocked(true);

            Debug.Log($"Ingredient UNLOCKED → {ingredientName}");

            // 🔥 okamžitá aktualizácia UI
            UpdatePageVisibility();

            // Check if all ingredients are unlocked
            CheckWinCondition();
        }
        else
        {
            Debug.LogWarning($"Ingredient '{ingredientName}' not found!");
        }
    }

    void CheckWinCondition()
    {
        bool allUnlocked = ingredients.TrueForAll(i => i.unlocked);
        if (allUnlocked && winPanel != null)
        {
            winPanel.SetActive(true);
            //GameLogger.Instance.Log("finishlevel", playerTransform.position);
            Debug.Log("All ingredients unlocked! You WIN!");
        }
    }
}
