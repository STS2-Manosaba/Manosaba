using HarmonyLib;
using Manosaba.Combat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
public static class Patch_FieldPower_SetAmount
{
    [HarmonyPrefix]
    private static bool Prefix(PowerModel __instance, int amount, ref int ____amount)
    {
        if (__instance is not FieldPowerModel fieldPower)
            return true;

        fieldPower.AssertMutable();
        int clamped = fieldPower.StackType == PowerStackType.Single
            ? Math.Clamp(amount, 0, 1)
            : Math.Clamp(amount, -999999999, 999999999);
        if (____amount != clamped)
        {
            ____amount = clamped;
            fieldPower.NotifyAmountChanged();
        }
        return false;
    }
}

// ModifyDamageInternal is shared by actual damage, intents, and individual/AOE card
// previews. Apply at the additive stage so Weak/Vulnerable and damage caps still work.
[HarmonyPatch(typeof(Hook), "ModifyDamageInternal")]
public static class Patch_FieldPower_ModifyDamage
{
    [HarmonyPrefix]
    private static void Prefix(ICombatState? combatState, Creature? target, Creature? dealer,
        ref decimal damage, ValueProp props, CardModel? cardSource, ModifyDamageHookType modifyDamageHookType,
        out List<AbstractModel> __state)
    {
        __state = [];
        if (combatState == null || !modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive))
            return;

        foreach (var power in FieldPowerState.Get(combatState))
        {
            decimal extra = power.ModifyDamageAdditive(target, damage, props, dealer, cardSource);
            if (extra == 0m)
                continue;
            damage += extra;
            __state.Add(power);
        }
    }

    [HarmonyPostfix]
    private static void Postfix(List<AbstractModel> modifiers, List<AbstractModel> __state) =>
        modifiers.AddRange(__state);
}
