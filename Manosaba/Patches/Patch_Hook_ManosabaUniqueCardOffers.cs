using System.Collections.Generic;
using HarmonyLib;
using Manosaba.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Patches;

/// <summary>
/// Post-filters card offers so <see cref="Manosaba.Characters.Common.Overrides.ManosabaKeywords.Unique"/> templates
/// never appear for a player who already has a copy in their deck (rewards, merchant pool before roll).
/// Transform paths use <see cref="ManosabaTransformHelper"/> (including <see cref="ManosabaTransformHelper.CardFactoryCustomPoolTransformUniquePatch"/>).
/// <see cref="Hook.ModifyMerchantCardCreationResults"/> alone is ineffective: <c>MerchantCardEntry</c> never reapplies the list to <c>CreationResult</c>.
/// Custom Manosaba events that build <see cref="CardCreationOptions"/> or card lists without <see cref="Hook"/> should call
/// <see cref="ManosabaUniqueCardEligibility.FilterCardCreationOptions"/> / <see cref="ManosabaUniqueCardEligibility.FilterCardCreationResults"/> explicitly.
/// </summary>
public static class Patch_Hook_ManosabaUniqueCardOffers
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardRewardCreationOptions))]
    public static class Unique_ModifyCardRewardCreationOptions
    {
        private static void Postfix(IRunState runState, Player player, CardCreationOptions options, ref CardCreationOptions __result)
        {
            __result = ManosabaUniqueCardEligibility.FilterCardCreationOptions(player, __result);
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.TryModifyCardRewardOptions))]
    public static class Unique_TryModifyCardRewardOptions
    {
        private static void Postfix(IRunState runState, Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
        {
            if (creationOptions.Flags.HasFlag(CardCreationFlags.NoModifyHooks))
                return;

            int before = cardRewardOptions.Count;
            ManosabaUniqueCardEligibility.FilterCardCreationResults(player, cardRewardOptions);
            int removed = before - cardRewardOptions.Count;

            // 帶 NoCardPoolModifications 的獎勵（如「藥水的未來」）不會在抽前過濾唯一卡，導致被移除後少一張；補回等量非唯一/未擁有的卡。
            ManosabaUniqueCardEligibility.RefillCardRewardAfterUniqueRemoval(player, cardRewardOptions, creationOptions, removed);
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyMerchantCardPool))]
    public static class Unique_ModifyMerchantCardPool
    {
        private static void Postfix(IRunState runState, Player player, IEnumerable<CardModel> options, ref IEnumerable<CardModel> __result)
        {
            __result = ManosabaUniqueCardEligibility.FilterMerchantCardPool(player, __result);
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyMerchantCardCreationResults))]
    public static class Unique_ModifyMerchantCardCreationResults
    {
        private static void Postfix(IRunState runState, Player player, List<CardCreationResult> cards)
        {
            ManosabaUniqueCardEligibility.FilterCardCreationResults(player, cards);
        }
    }
}
