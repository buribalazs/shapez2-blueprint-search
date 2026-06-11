using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SearchUI
{
    public static void EnsureSetup(HUDBlueprintLibrary lib)
    {
        var root = lib.UIMainCanvasGroup.transform;
        if (root.Find("__BlueprintSearchField") != null) return;

        SearchState.Register(lib);

        // Create overlay before the input field so the field renders on top
        SearchOverlay.EnsureOverlay(lib);

        var inputField = CreateInputField(root);
        inputField.onValueChanged.AddListener(SearchState.UpdateTerm);
        SearchKeyboardHandler.Attach(inputField.gameObject, inputField, lib);
    }

    public static void ResetSearch(HUDBlueprintLibrary lib)
    {
        var root = lib.UIMainCanvasGroup.transform;
        var fieldTransform = root.Find("__BlueprintSearchField");
        if (fieldTransform == null) return;

        // Always reset directly — onValueChanged won't fire if text is already "".
        var field = fieldTransform.GetComponent<TMP_InputField>();
        if (field != null) field.text = "";
        SearchOverlay.Refresh(lib, "");
        SearchFilter.ApplyToLibrary(lib);
    }

    private static TMP_InputField CreateInputField(Transform parent)
    {
        // Container
        var go = new GameObject("__BlueprintSearchField");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-20f, 36f);
        rt.anchoredPosition = new Vector2(0f, -8f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        // Clear (×) button — shown only when field has text
        var clearGO = new GameObject("ClearButton");
        clearGO.transform.SetParent(go.transform, false);
        var clearRT = clearGO.AddComponent<RectTransform>();
        clearRT.anchorMin = new Vector2(1f, 0.5f);
        clearRT.anchorMax = new Vector2(1f, 0.5f);
        clearRT.pivot = new Vector2(1f, 0.5f);
        clearRT.sizeDelta = new Vector2(28f, 28f);
        clearRT.anchoredPosition = new Vector2(-4f, 0f);
        var clearBg = clearGO.AddComponent<Image>();
        clearBg.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        var clearBtn = clearGO.AddComponent<Button>();
        clearBtn.targetGraphic = clearBg;
        var clearColors = clearBtn.colors;
        clearColors.highlightedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
        clearColors.pressedColor = new Color(2f, 2f, 2f, 1f);
        clearBtn.colors = clearColors;
        var clearLabelGO = new GameObject("Label");
        clearLabelGO.transform.SetParent(clearGO.transform, false);
        var clearLabel = clearLabelGO.AddComponent<TextMeshProUGUI>();
        clearLabel.text = "×";
        clearLabel.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        clearLabel.fontSize = 18f;
        clearLabel.alignment = TextAlignmentOptions.Center;
        var clRT = clearLabel.rectTransform;
        clRT.anchorMin = Vector2.zero;
        clRT.anchorMax = Vector2.one;
        clRT.sizeDelta = Vector2.zero;
        clRT.anchoredPosition = Vector2.zero;
        clearGO.SetActive(false);

        // Text area with clipping mask — narrowed on the right to leave room for the clear button
        var textAreaGO = new GameObject("Text Area");
        textAreaGO.transform.SetParent(go.transform, false);
        var textAreaRT = textAreaGO.AddComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.sizeDelta = new Vector2(-44f, -6f);
        textAreaRT.anchoredPosition = new Vector2(-16f, 0f);
        textAreaGO.AddComponent<RectMask2D>();

        // Placeholder text
        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textAreaGO.transform, false);
        var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Search blueprints...";
        placeholder.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        placeholder.fontSize = 16f;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        var pRT = placeholder.rectTransform;
        pRT.anchorMin = Vector2.zero;
        pRT.anchorMax = Vector2.one;
        pRT.sizeDelta = Vector2.zero;
        pRT.anchoredPosition = Vector2.zero;

        // Input text
        var inputTextGO = new GameObject("Text");
        inputTextGO.transform.SetParent(textAreaGO.transform, false);
        var inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
        inputText.color = Color.white;
        inputText.fontSize = 16f;
        inputText.alignment = TextAlignmentOptions.MidlineLeft;
        var iRT = inputText.rectTransform;
        iRT.anchorMin = Vector2.zero;
        iRT.anchorMax = Vector2.one;
        iRT.sizeDelta = Vector2.zero;
        iRT.anchoredPosition = Vector2.zero;

        // Wire up TMP_InputField
        var field = go.AddComponent<TMP_InputField>();
        field.textViewport = textAreaRT;
        field.textComponent = inputText;
        field.placeholder = placeholder;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.richText = false;

        clearBtn.onClick.AddListener(() =>
        {
            field.text = "";
            field.ActivateInputField();
        });
        field.onValueChanged.AddListener(s => clearGO.SetActive(!string.IsNullOrEmpty(s)));

        return field;
    }
}
