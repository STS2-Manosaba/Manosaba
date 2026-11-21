using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using Manosaba.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(CombatRoom), nameof(CombatRoom.OnCombatEnded))]
public static class Patch_CombatRoom_OnCombatEnded_StopCustomBgm
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        GodotSfxRouter.StopCustomBgmAndResumeVanilla();
    }
}

[HarmonyPatch(typeof(NRun), nameof(NRun.SetCurrentRoom), new[] { typeof(Control) })]
public static class Patch_NRun_SetCurrentRoom_StopCustomBgmOutsideCombat
{
    [HarmonyPostfix]
    private static void Postfix(Control? node)
    {
        if (node is NCombatRoom)
        {
            return;
        }

        GodotSfxRouter.StopCustomBgmAndResumeVanilla();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.Abandon))]
public static class Patch_RunManager_Abandon_StopCustomBgm
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        GodotSfxRouter.StopCustomBgmAndResumeVanilla();
    }
}

[HarmonyPatch(typeof(NPauseMenu), "OnSaveAndQuitButtonPressed", new[] { typeof(NButton) })]
public static class Patch_NPauseMenu_SaveAndQuit_StopCustomBgm
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        GodotSfxRouter.StopCustomBgmAndResumeVanilla();
    }
}

[HarmonyPatch]
public static class Patch_NRun_ShowGameOverScreen_StopCustomBgm
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        AccessTools.GetDeclaredMethods(typeof(NRun))
            .Where(method => method.Name == nameof(NRun.ShowGameOverScreen));

    [HarmonyPrefix]
    private static void Prefix()
    {
        GodotSfxRouter.StopCustomBgmAndResumeVanilla();
    }
}

[HarmonyPatch]
public static class Patch_CreatureCmd_Kill_Player_StopCustomBgm
{
    private static MethodBase? TargetMethod()
    {
        return AccessTools.GetDeclaredMethods(typeof(CreatureCmd))
            .FirstOrDefault(method =>
            {
                if (method.Name != nameof(CreatureCmd.Kill))
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(Creature);
            });
    }

    [HarmonyPostfix]
    private static void Postfix(Creature __0)
    {
        Creature creature = __0;
        if (creature?.IsPlayer != true)
        {
            return;
        }

        if (creature.CombatState == null)
        {
            GodotSfxRouter.StopCustomBgmAndResumeVanilla();
            return;
        }

        bool anyAlivePlayer = creature.CombatState.Players
            .Any(player => player?.Creature is { IsAlive: true });
        if (!anyAlivePlayer)
        {
            GodotSfxRouter.StopCustomBgmAndResumeVanilla();
        }
    }
}
