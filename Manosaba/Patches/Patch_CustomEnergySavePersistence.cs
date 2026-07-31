using HarmonyLib;
using Manosaba.Characters.Common.Resources;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Manosaba.Patches;

[HarmonyPatch]
public static class Patch_CustomEnergySavePersistence
{
    [HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
    [HarmonyPrefix]
    private static void RunState_FromSerializable_Prefix()
    {
        CharacterCustomEnergyService.ClearAll();
    }

    [HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
    [HarmonyPostfix]
    private static void RunState_FromSerializable_Postfix(RunState __result)
    {
        CharacterCustomEnergyService.LoadSavedValues(__result.Players);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SyncWithSerializedPlayer))]
    [HarmonyPostfix]
    private static void Player_SyncWithSerializedPlayer_Postfix(Player __instance)
    {
        CharacterCustomEnergyService.LoadSavedValues(__instance);
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRun))]
    [HarmonyPrefix]
    private static void SaveManager_SaveRun_Prefix()
    {
        RunState? state = RunManager.Instance.DebugOnlyGetState();
        if (state == null)
        {
            return;
        }

        CharacterCustomEnergyService.SaveCurrentValuesToCarriers(state.Players);
    }
}
