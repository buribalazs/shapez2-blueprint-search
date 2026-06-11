using System;

public static class SearchFilter
{
    public static void ApplyToLibrary(HUDBlueprintLibrary lib)
    {
        foreach (var nav in lib.UIFolderNavigators)
        {
            if (nav != null && nav.gameObject.activeSelf)
                ApplyToNavigator(nav);
        }
    }

    public static void ApplyToNavigator(HUDBlueprintLibraryFolderNavigator nav)
    {
        string term = SearchState.Term;
        foreach (var entry in nav.Entries)
        {
            if (entry == null) continue;
            bool show = string.IsNullOrEmpty(term) ||
                        (entry.Entry?.Title?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
            entry.gameObject.SetActive(show);
        }
    }
}
