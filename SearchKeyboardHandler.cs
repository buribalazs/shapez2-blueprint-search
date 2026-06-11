using TMPro;
using UnityEngine;

public class SearchKeyboardHandler : MonoBehaviour
{
    private TMP_InputField _field;
    private HUDBlueprintLibrary _lib;

    public static void Attach(GameObject go, TMP_InputField field, HUDBlueprintLibrary lib)
    {
        var h = go.AddComponent<SearchKeyboardHandler>();
        h._field = field;
        h._lib = lib;
    }

    private void Update()
    {
        bool cmdOrCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                         Input.GetKey(KeyCode.LeftCommand)  || Input.GetKey(KeyCode.RightCommand);

        if (cmdOrCtrl && Input.GetKeyDown(KeyCode.F))
        {
            _field.ActivateInputField();
            _field.Select();
            return;
        }

        if (!SearchOverlay.IsVisible(_lib)) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            SearchOverlay.MoveSelection(+1, _field);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            SearchOverlay.MoveSelection(-1, _field);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SearchOverlay.ConfirmSelection();
    }
}
