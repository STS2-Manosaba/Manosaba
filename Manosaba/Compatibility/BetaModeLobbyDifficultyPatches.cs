using HarmonyLib;
using Manosaba.Patches;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using System.Reflection;

namespace Manosaba.Compatibility;

/// <summary>
/// Installs lobby difficulty controls for beta-only modes without adding hard references to their screen types.
/// The type lookups naturally no-op on main builds where Blitz and Dread screens do not exist.
/// </summary>
internal static class BetaModeLobbyDifficultyPatches
{
    private static readonly string[] ScreenTypeNames =
    [
        "MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzScreen",
        "MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen",
    ];

    internal static void Install(Harmony harmony)
    {
        foreach (string typeName in ScreenTypeNames)
        {
            Type? screenType = AccessTools.TypeByName(typeName);
            if (screenType == null)
            {
                continue;
            }

            try
            {
                PatchIfFound(harmony, AccessTools.Method(screenType, "AfterInitialized"), postfix: nameof(AfterInitializedPostfix));
                PatchIfFound(harmony, AccessTools.Method(screenType, "_Process", [typeof(double)]), postfix: nameof(ProcessPostfix));
                PatchIfFound(harmony, AccessTools.Method(screenType, "CleanUpLobby"), prefix: nameof(CleanUpLobbyPrefix));
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to install Manosaba difficulty controls on {typeName}: {ex.Message}");
            }
        }
    }

    private static void PatchIfFound(Harmony harmony, MethodBase? target, string? prefix = null, string? postfix = null)
    {
        if (target == null)
        {
            return;
        }

        harmony.Patch(
            target,
            prefix: prefix == null ? null : new HarmonyMethod(typeof(BetaModeLobbyDifficultyPatches), prefix),
            postfix: postfix == null ? null : new HarmonyMethod(typeof(BetaModeLobbyDifficultyPatches), postfix));
    }

    private static void AfterInitializedPostfix(object __instance)
    {
        if (__instance is not Godot.Node ownerNode || __instance is not Godot.Control attachParent)
        {
            return;
        }

        ManosabaLobbyDifficultyUiHost.OnEnter(
            __instance,
            ownerNode,
            attachParent,
            () => GetLobbyContext(__instance),
            LobbyDifficultyPanelLayout.CharacterSelectDefault,
            LobbyDifficultyUiEnterKind.FirstOpen);
    }

    private static void ProcessPostfix(object __instance, double delta)
    {
        if (__instance is Godot.Node ownerNode)
        {
            ManosabaLobbyDifficultyUiHost.OnProcess(__instance, ownerNode, () => GetLobbyContext(__instance));
        }
    }

    private static void CleanUpLobbyPrefix(object __instance) =>
        ManosabaLobbyDifficultyUiHost.OnCleanup(__instance, () => GetLobbyContext(__instance));

    private static LobbyDifficultyUiNetContext? GetLobbyContext(object owner)
    {
        object? lobbyObject = AccessTools.Property(owner.GetType(), "Lobby")?.GetValue(owner)
            ?? AccessTools.Field(owner.GetType(), "_lobby")?.GetValue(owner);
        return lobbyObject is StartRunLobby lobby ? LobbyDifficultyUiNetContext.From(lobby) : null;
    }
}
