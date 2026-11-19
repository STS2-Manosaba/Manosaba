using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace Manosaba.Patches;

internal static class StartRunLobbyRandomResolutionScope
{
    [ThreadStatic]
    private static int _depth;

    internal static bool IsActive => _depth > 0;

    internal static int Enter()
    {
        int previousDepth = _depth;
        _depth++;
        return previousDepth;
    }

    internal static void Restore(int previousDepth) => _depth = previousDepth;
}

[HarmonyPatch(typeof(StartRunLobby), "BeginRunLocally")]
internal static class Patch_StartRunLobby_BeginRunLocally_RandomResolutionScope
{
    private static void Prefix(out int __state) => __state = StartRunLobbyRandomResolutionScope.Enter();

    private static void Postfix(int __state) => StartRunLobbyRandomResolutionScope.Restore(__state);

    private static Exception? Finalizer(Exception? __exception, int __state)
    {
        StartRunLobbyRandomResolutionScope.Restore(__state);
        return __exception;
    }
}
