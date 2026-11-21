using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;

namespace Manosaba.Patches;

/// <summary>
/// Vanilla <see cref="AssetLoadingSession"/> requests every non-VFX path through Godot's threaded loader at once
/// (up to 128 concurrently). Manosaba character scenes share sub-resources across characters (same override scripts,
/// <c>kreon_bold.ttf</c>, <c>selection_reticle.tscn</c>, ...). Concurrent threaded loads that share ext_resources can
/// race in Godot's loader and leave a request in <c>InProgress</c> forever, so the session never completes and the run
/// start (especially multiplayer, which preloads several characters) hangs on a black screen.
/// <para/>
/// Load this mod's own resources synchronously on the main thread and seed the session cache so the threaded loader
/// skips them. Vanilla assets keep the original threaded path.
/// </summary>
[HarmonyPatch(
    typeof(AssetLoadingSession),
    MethodType.Constructor,
    [typeof(string), typeof(IEnumerable<string>), typeof(ConcurrentDictionary<string, Resource>), typeof(AssetCache)])]
public static class Patch_AssetLoadingSession_MainThreadModAssets
{
    private const string ModResourcePrefix = "res://Manosaba/";

    private static bool IsModResource(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string normalized = path.Replace('\\', '/');
        if (!normalized.StartsWith("res://"))
        {
            normalized = "res://" + normalized.TrimStart('/');
        }

        return normalized.StartsWith(ModResourcePrefix);
    }

    [HarmonyPrefix]
    private static void Prefix(string name, ref IEnumerable<string> paths, ConcurrentDictionary<string, Resource> cache)
    {
        if (paths == null || cache == null)
        {
            return;
        }

        List<string> list = paths.ToList();
        paths = list;

        Stopwatch sw = Stopwatch.StartNew();
        int loaded = 0;
        // Mod helpers build paths with Path.Join, so on Windows they arrive as "Manosaba\scenes\..." (relative, backslash).
        // Normalize only for matching; keep the original string as the cache key the game will look up.
        foreach (string path in list.Where(IsModResource).Distinct().OrderBy(p => p, System.StringComparer.Ordinal))
        {
            if (cache.ContainsKey(path) || !ResourceLoader.Exists(path))
            {
                continue;
            }

            Resource? resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
            if (resource == null)
            {
                continue;
            }

            cache[path] = resource;
            loaded++;
        }

        if (loaded > 0)
        {
            Log.Info($"[Manosaba] Preloaded {loaded} mod assets on main thread for '{name}' in {sw.ElapsedMilliseconds}ms");
        }
    }
}
