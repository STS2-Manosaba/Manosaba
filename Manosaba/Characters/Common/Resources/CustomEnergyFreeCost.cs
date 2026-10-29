using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.Common.Resources;

public static class CustomEnergyFreeCost
{
    private static readonly AccessTools.FieldRef<CardEnergyCost, List<LocalCostModifier>> LocalModifiersRef =
        AccessTools.FieldRefAccess<CardEnergyCost, List<LocalCostModifier>>("_localModifiers");

    public static bool IsFree(CardModel card)
    {
        CardEnergyCost energyCost = card.EnergyCost;
        if (!energyCost.HasLocalModifiers)
        {
            return false;
        }

        // Read the native modifiers instead of tracking setter calls. BaseLib can
        // compile a caller before our patches and inline those setters, bypassing
        // later hooks. Native modifiers also follow cloning and expiry automatically.
        int currentCost = energyCost.GetWithModifiers((CostModifiers)0);
        foreach (LocalCostModifier modifier in LocalModifiersRef(energyCost))
        {
            // A zero base cost or relative energy reduction alone does not waive
            // custom resources. Preserve the existing reduceOnly behavior too.
            if (modifier.Type == LocalCostType.Absolute && modifier.Amount == 0
                && (!modifier.IsReduceOnly || currentCost > 0))
            {
                return true;
            }

            currentCost = modifier.Modify(currentCost);
        }

        return false;
    }
}
