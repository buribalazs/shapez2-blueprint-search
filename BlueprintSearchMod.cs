using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using ShapezShifter.SharpDetour;
using UnityEngine;
using UnityEngine.EventSystems;
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

        // Prevent any keybinding (including remapped ones) from closing the library
        // while the user is typing in the search field.
        var hideMethod = typeof(HUDBlueprintLibrary)
            .GetMethod("Hide", BindingFlags.Instance | BindingFlags.NonPublic);
        _hooks.Add(new Hook(hideMethod,
            new Action<Action<HUDBlueprintLibrary>, HUDBlueprintLibrary>((orig, self) =>
            {
                // Block Hide() only when the user is typing in the search field
                // AND the close was not triggered by ESC. ESC should always close
                // the library — the X button exists for clearing without leaving.
                var selected = EventSystem.current?.currentSelectedGameObject;
                if (selected != null && selected.name == "__BlueprintSearchField"
                    && !Input.GetKeyDown(KeyCode.Escape))
                    return;

                orig(self);
            })
        ));
    }

    public void Dispose()
    {
        foreach (var hook in _hooks) hook.Dispose();
        _hooks.Clear();
        SearchState.Clear();
    }
}
