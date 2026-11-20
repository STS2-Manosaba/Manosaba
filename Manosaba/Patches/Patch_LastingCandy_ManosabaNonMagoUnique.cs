using HarmonyLib;
using Manosaba.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Manosaba.Patches;

/// <summary>
/// After vanilla Lasting Candy adds a power to encounter rewards, removes Unique offers already in deck for non-Mago Manosaba characters (Mago uses <see cref="Patch_Hosho_Mago_Card_Factory"/>).
/// </summary>
[HarmonyPatch(typeof(LastingCandy), nameof(LastingCandy.TryModifyCardRewardOptions))]
public static class Patch_LastingCandy_ManosabaNonMagoUnique
{
    [HarmonyPostfix]
    private static void Postfix(Player player, List<CardCreationResult> rewardOptions, bool __result)
    {
        if (!__result)
        {
            return;
        }

        if (!ManosabaPlayerHelper.IsManosabaPlayer(player) || ManosabaPlayerHelper.IsHoshoMagoPlayer(player))
        {
            return;
        }

        ManosabaUniqueCardEligibility.FilterCardCreationResults(player, rewardOptions);
    }
}
