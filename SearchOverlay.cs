using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SearchOverlay
{
    private const string OVERLAY_NAME = "__BlueprintSearchOverlay";
    private const float ROW_HEIGHT = 56f;

    private static readonly Color RowNormal = new Color(0.13f, 0.14f, 0.18f, 1f);
    private static readonly Color RowSelected = new Color(0.20f, 0.28f, 0.45f, 1f);

    private struct RowData
    {
        public Image Background;
        public System.Action OnClick;
    }

    private static readonly List<RowData> _rows = new List<RowData>();
    private static int _selectedIndex = -1;
    private static ScrollRect _scrollRect;

    // ── Public API ──────────────────────────────────────────────────────────────

    public static void EnsureOverlay(HUDBlueprintLibrary lib)
    {
        var root = lib.UIMainCanvasGroup.transform;
        if (root.Find(OVERLAY_NAME) != null) return;

        var overlay = new GameObject(OVERLAY_NAME);
        overlay.transform.SetParent(root, false);

        var rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = overlay.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.09f, 0.97f);

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(overlay.transform, false);
        var vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero;
        vpRT.anchoredPosition = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot = new Vector2(0.5f, 1f);
        cRT.sizeDelta = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 3f;
        vlg.padding = new RectOffset(12, 12, 52, 12);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = overlay.AddComponent<ScrollRect>();
        sr.viewport = vpRT;
        sr.content = cRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 30f;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.inertia = true;
        sr.decelerationRate = 0.135f;

        overlay.SetActive(false);
    }

    public static void Refresh(HUDBlueprintLibrary lib, string term)
    {
        var root = lib.UIMainCanvasGroup.transform;
        var overlayTransform = root.Find(OVERLAY_NAME);
        if (overlayTransform == null) return;

        bool searching = !string.IsNullOrEmpty(term);
        overlayTransform.gameObject.SetActive(searching);

        _selectedIndex = -1;
        _rows.Clear();

        if (!searching) return;

        _scrollRect = overlayTransform.GetComponent<ScrollRect>();

        var content = overlayTransform.Find("Viewport/Content");
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Object.Destroy(content.GetChild(i).gameObject);

        bool anyMatch = false;
        foreach (var entry in lib.BlueprintLibrary.RootEntry.AllChildren)
        {
            if (!(entry is BlueprintLibraryEntry bp)) continue;
            if (!Matches(term, bp.RelativePathWithoutExtension)) continue;

            var fullPath = bp.RelativePathWithoutExtension;
            var lastSlash = fullPath.LastIndexOf('/');
            var folderPath = lastSlash >= 0 ? fullPath.Substring(0, lastSlash) : "";

            var captured = bp;
            CreateRow(content, bp.Title, folderPath, () => lib.SelectBlueprint(captured.Blueprint));
            anyMatch = true;
        }

        if (!anyMatch)
            CreateEmptyRow(content);
    }

    public static bool IsVisible(HUDBlueprintLibrary lib)
    {
        var t = lib.UIMainCanvasGroup.transform.Find(OVERLAY_NAME);
        return t != null && t.gameObject.activeSelf;
    }

    // delta = +1 (down) or -1 (up). field is used to re-focus when going above the first row.
    public static void MoveSelection(int delta, TMP_InputField field)
    {
        if (_rows.Count == 0) return;

        if (_selectedIndex >= 0)
            SetRowHighlight(_selectedIndex, false);

        int next = _selectedIndex + delta;

        if (next < 0)
        {
            _selectedIndex = -1;
            field.ActivateInputField();
            field.Select();
            return;
        }

        next = Mathf.Clamp(next, 0, _rows.Count - 1);
        _selectedIndex = next;
        SetRowHighlight(_selectedIndex, true);
        field.DeactivateInputField();
        ScrollToRow(_selectedIndex);
    }

    public static void ConfirmSelection()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
            _rows[_selectedIndex].OnClick();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static void SetRowHighlight(int index, bool selected)
    {
        _rows[index].Background.color = selected ? RowSelected : RowNormal;
    }

    private static void ScrollToRow(int index)
    {
        if (_scrollRect == null) return;

        var vlg = _scrollRect.content.GetComponent<VerticalLayoutGroup>();
        float padTop = vlg != null ? vlg.padding.top : 0f;
        float spacing = vlg != null ? vlg.spacing : 0f;

        float rowTop = padTop + index * (ROW_HEIGHT + spacing);
        float rowBottom = rowTop + ROW_HEIGHT;

        float viewH = _scrollRect.viewport.rect.height;
        float scrollTop = -_scrollRect.content.anchoredPosition.y;
        float scrollBottom = scrollTop + viewH;

        if (rowTop < scrollTop)
            _scrollRect.content.anchoredPosition = new Vector2(0f, -rowTop);
        else if (rowBottom > scrollBottom && viewH > 0f)
            _scrollRect.content.anchoredPosition = new Vector2(0f, -(rowBottom - viewH));
    }

    private static void CreateRow(Transform parent, string title, string folderPath, System.Action onClick)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(parent, false);

        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = ROW_HEIGHT;
        le.minHeight = ROW_HEIGHT;

        var bg = row.AddComponent<Image>();
        bg.color = RowNormal;

        var btn = row.AddComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.4f, 1.4f, 1.6f, 1f);
        colors.pressedColor = new Color(1.6f, 1.6f, 2f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        _rows.Add(new RowData { Background = bg, OnClick = onClick });

        float titleTop = string.IsNullOrEmpty(folderPath) ? 0.2f : 0.45f;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(row.transform, false);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.color = Color.white;
        titleText.fontSize = 15f;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        var tRT = titleText.rectTransform;
        tRT.anchorMin = new Vector2(0f, titleTop);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.offsetMin = new Vector2(14f, 0f);
        tRT.offsetMax = new Vector2(-14f, 0f);

        if (!string.IsNullOrEmpty(folderPath))
        {
            var pathGO = new GameObject("Path");
            pathGO.transform.SetParent(row.transform, false);
            var pathText = pathGO.AddComponent<TextMeshProUGUI>();
            pathText.text = folderPath;
            pathText.color = new Color(0.55f, 0.60f, 0.70f, 1f);
            pathText.fontSize = 11f;
            pathText.alignment = TextAlignmentOptions.MidlineLeft;
            var pRT = pathText.rectTransform;
            pRT.anchorMin = new Vector2(0f, 0f);
            pRT.anchorMax = new Vector2(1f, titleTop);
            pRT.offsetMin = new Vector2(14f, 0f);
            pRT.offsetMax = new Vector2(-14f, 0f);
        }
    }

    // ── Matching ─────────────────────────────────────────────────────────────────

    // 1. Substring match on the full path (case-insensitive).
    //    "giant" → matches "SPACE/PAINT/giant mixer"
    //    "paint/giant" → matches "SPACE/PAINT/giant mixer"
    // 2. Subsequence match on stripped+lowercased path for 3+ char queries.
    //    "spacgimix" → "spacepaintgiantmixer" → s,p,a,c from space; g,i from giant; m,i,x from mixer
    private static bool Matches(string term, string fullPath)
    {
        if (fullPath.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (term.Length < 3) return false;
        return IsSubsequence(StripNonAlpha(term), StripNonAlpha(fullPath));
    }

    private static bool IsSubsequence(string query, string text)
    {
        int qi = 0;
        for (int ti = 0; ti < text.Length && qi < query.Length; ti++)
            if (text[ti] == query[qi]) qi++;
        return qi == query.Length;
    }

    private static string StripNonAlpha(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLower(c));
        return sb.ToString();
    }

    private static void CreateEmptyRow(Transform parent)
    {
        var go = new GameObject("NoResults");
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 60f;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = "No blueprints found";
        text.color = new Color(0.5f, 0.5f, 0.6f, 1f);
        text.fontSize = 14f;
        text.alignment = TextAlignmentOptions.Center;
    }
}
