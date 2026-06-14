using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SearchKeyboardHandler : MonoBehaviour
{
    private TMP_InputField _field;
    private HUDBlueprintLibrary _lib;

    public static void Attach(GameObject go, TMP_InputField field, HUDBlueprintLibrary lib)
    {
        var h = go.AddComponent<SearchKeyboardHandler>();
        h._field = field;
        h._lib   = lib;
    }

    private void Update()
    {
        bool cmdOrCtrl = Input.GetKey(KeyCode.LeftControl)  || Input.GetKey(KeyCode.RightControl) ||
                         Input.GetKey(KeyCode.LeftCommand)  || Input.GetKey(KeyCode.RightCommand);

        if (cmdOrCtrl && Input.GetKeyDown(KeyCode.F))
        {
            _field.ActivateInputField();
            _field.Select();
            return;
        }

        if (!SearchOverlay.IsVisible(_lib)) return;

        // Click outside our search UI (search bar or overlay) → clear search, same as ×
        if (Input.GetMouseButtonDown(0) && !ClickHitSearchUI())
        {
            _field.text = "";
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
            SearchOverlay.MoveSelection(+1, _field);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            SearchOverlay.MoveSelection(-1, _field);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SearchOverlay.ConfirmSelection();
    }

    private bool ClickHitSearchUI()
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(
            new PointerEventData(EventSystem.current) { position = Input.mousePosition },
            hits);

        // Our UI roots: the search field container and the results overlay
        var root        = _lib.UIMainCanvasGroup.transform;
        var fieldRoot   = _field.transform;                   // __BlueprintSearchField
        var overlayRoot = root.Find("__BlueprintSearchOverlay");

        foreach (var hit in hits)
        {
            var t = hit.gameObject.transform;
            if (IsDescendantOf(t, fieldRoot))                          return true;
            if (overlayRoot != null && IsDescendantOf(t, overlayRoot)) return true;
        }
        return false;
    }

    private static bool IsDescendantOf(Transform child, Transform ancestor)
    {
        for (var t = child; t != null; t = t.parent)
            if (t == ancestor) return true;
        return false;
    }
}
