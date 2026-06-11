using System.Collections.Generic;

public static class SearchState
{
    private static string _term = "";
    private static readonly List<System.WeakReference<HUDBlueprintLibrary>> _libraries =
        new List<System.WeakReference<HUDBlueprintLibrary>>();

    public static string Term => _term;

    public static void Register(HUDBlueprintLibrary lib)
    {
        _libraries.RemoveAll(wr => !wr.TryGetTarget(out var l) || l == null);
        _libraries.Add(new System.WeakReference<HUDBlueprintLibrary>(lib));
    }

    public static void PrepareForShow()
    {
        _term = "";
    }

    public static void UpdateTerm(string term)
    {
        _term = term ?? "";
        _libraries.RemoveAll(wr => !wr.TryGetTarget(out var l) || l == null);
        foreach (var wr in _libraries)
        {
            if (!wr.TryGetTarget(out var lib) || lib == null) continue;
            SearchOverlay.Refresh(lib, _term);
            if (string.IsNullOrEmpty(_term))
                SearchFilter.ApplyToLibrary(lib);
        }
    }

    public static void Clear()
    {
        _term = "";
        _libraries.Clear();
    }
}
