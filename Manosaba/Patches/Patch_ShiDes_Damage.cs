using HarmonyLib;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class Patch_ShiDes_Damage
{
    // Restore raw damage after every modifier, including defensive powers and damage caps.
    public static void Postfix(CardModel? cardSource, decimal damage, ref decimal __result)
    {
        if (cardSource is ShiDes) __result = damage;
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyHpLost))]
public static class Patch_ShiDes_HpLost
{
    public static void Postfix(CardModel? cardSource, decimal amount, ref decimal __result)
    {
        if (cardSource is ShiDes) __result = amount;
    }
}
