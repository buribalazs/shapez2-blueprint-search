using System.Collections.Generic;
using JetBrains.Annotations;
using ShapezShifter.SharpDetour;
using ILogger = Core.Logging.ILogger;

[UsedImplicitly]
public class BlueprintSearchMod : IMod
{
    private readonly List<System.IDisposable> _hooks = new List<System.IDisposable>();

    public BlueprintSearchMod(ILogger logger)
    {
        _hooks.Add(DetourHelper.CreatePostfixHook<HUDBlueprintLibrary>(
            lib => lib.Run(),
            lib => SearchUI.EnsureSetup(lib)
        ));

        _hooks.Add(DetourHelper.CreatePostfixHook<HUDBlueprintLibrary>(
            lib => lib.Show(),
            lib => SearchUI.ResetSearch(lib)
        ));

        _hooks.Add(DetourHelper.CreatePostfixHook<HUDBlueprintLibraryFolderNavigator>(
            nav => nav.RefreshChildList(),
            nav => SearchFilter.ApplyToNavigator(nav)
        ));
    }

    public void Dispose()
    {
        foreach (var hook in _hooks) hook.Dispose();
        _hooks.Clear();
        SearchState.Clear();
    }
}
