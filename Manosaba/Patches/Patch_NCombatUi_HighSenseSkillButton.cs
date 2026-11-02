using HarmonyLib;
using Manosaba.Characters.TonoHanna.Combat;
using Manosaba.Characters.TonoHanna.Visuals;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(NCombatUi))]
public static class Patch_NCombatUi_HighSenseSkillButton
{
    [HarmonyPatch(nameof(NCombatUi.Activate))]
    [HarmonyPostfix]
    private static void Activate_Postfix(NCombatUi __instance, ICombatState state)
    {
        Player? me = TryResolveLocalPlayer(state);
        if (me == null)
        {
            HighSenseSkillButtonUi.RemoveFrom(__instance);
            return;
        }

        HighSenseSkillButtonUi.RefreshIfPresent(__instance, state);
    }

    [HarmonyPatch(nameof(NCombatUi.Deactivate))]
    [HarmonyPostfix]
    private static void Deactivate_Postfix(NCombatUi __instance)
    {
        HighSenseSkillButtonUi.RemoveFrom(__instance);
        HighSenseSkillActivation.ClearAllActivationPending();
    }

    private static Player? TryResolveLocalPlayer(ICombatState state)
    {
        try
        {
            return LocalContext.GetMe(state);
        }
        catch (Exception)
        {
            return state.Players.Count == 1 ? state.Players[0] : null;
        }
    }
}
