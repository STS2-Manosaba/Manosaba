using HarmonyLib;
using Manosaba.Combat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(NCombatUi))]
public static class Patch_NCombatUi_FieldPowerDisplay
{
    private const string NodeName = "ManosabaFieldPowerDisplay";

    [HarmonyPatch(nameof(NCombatUi.Activate))]
    [HarmonyPostfix]
    private static void Activate(NCombatUi __instance, CombatState state)
    {
        Remove(__instance);
        var display = new NFieldPowerDisplay { Name = NodeName };
        display.Initialize(state);
        __instance.AddChild(display);
    }

    [HarmonyPatch(nameof(NCombatUi.Deactivate))]
    [HarmonyPatch("PostCombatCleanUp")]
    [HarmonyPostfix]
    private static void Remove(NCombatUi __instance)
    {
        var node = __instance.GetNodeOrNull<NFieldPowerDisplay>(NodeName);
        if (node == null)
            return;
        __instance.RemoveChild(node);
        node.QueueFree();
    }
}
