using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace Manosaba.Patches;

/// <summary>
/// Base and modded random-card effects commonly build their candidates from
/// UnlockState.CharacterCardPools or CardPoolModel.GetUnlockedCards. Exclude pools
/// and card models belonging to characters hidden from random selection.
/// </summary>
[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.CharacterCardPools), MethodType.Getter)]
internal static class Patch_HiddenCharacterCardPools_FromRandomGeneration
{
    [HarmonyPostfix]
    private static void ExcludeHiddenCharacterPools(ref IEnumerable<CardPoolModel> __result)
    {
        __result = __result.Where(pool => !IsHiddenCharacterPool(pool));
    }

    private static bool IsHiddenCharacterPool(CardPoolModel pool) =>
        ModelDb.AllCharacters.Any(character =>
            character is CustomCharacterModel { AllowInVanillaRandomCharacterSelect: false }
            && ReferenceEquals(character.CardPool, pool));
}

[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
internal static class Patch_HiddenCharacterCards_FromRandomGeneration
{
    private static readonly string EmaCardsNamespace = "manosaba.Characters.SakurabaEma.Cards";

    [HarmonyPostfix]
    private static void ExcludeHiddenCharacterCards(ref IEnumerable<CardModel> __result)
    {
        __result = __result.Where(card => !IsHiddenCharacterCard(card));
    }

    private static bool IsHiddenCharacterCard(CardModel card)
    {
        string? cardNamespace = card.GetType().Namespace;
        if (cardNamespace != EmaCardsNamespace && cardNamespace?.StartsWith(EmaCardsNamespace + ".", StringComparison.Ordinal) != true)
        {
            return false;
        }

        return ModelDb.Character<manosaba.Characters.SakurabaEma.SakurabaEma>()
            is CustomCharacterModel { AllowInVanillaRandomCharacterSelect: false };
    }
}
