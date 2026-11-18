using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Patches;

/// <summary>
/// Ema's unfinished cards include tokens in the shared TokenCardPool, whose rarity makes them visible
/// under the card library's Misc filter. Hide every Ema card from the library while keeping the models
/// registered so existing card effects can still create them.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldShowInCardLibrary), MethodType.Getter)]
internal static class Patch_EmaCards_HiddenFromCardLibrary
{
    private const string EmaCardsNamespace = "manosaba.Characters.SakurabaEma.Cards";

    [HarmonyPrefix]
    private static bool HideUnfinishedEmaCards(CardModel __instance, ref bool __result)
    {
        string? modelNamespace = __instance.GetType().Namespace;
        if (modelNamespace == EmaCardsNamespace || modelNamespace?.StartsWith(EmaCardsNamespace + ".", StringComparison.Ordinal) == true)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
