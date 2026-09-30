using HarmonyLib;
using manosaba.Characters.SakurabaEma.Visuals;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(NCombatUi))]
public static class Patch_NCombatUi_WitchEncyclopedia
{
    private const string NodeName = "ManosabaWitchEncyclopedia";
    [HarmonyPatch(nameof(NCombatUi.Activate))]
    [HarmonyPostfix]
    private static void Activate(NCombatUi __instance, CombatState state)
    {
        Remove(__instance);
        var player = state.Players.FirstOrDefault(LocalContext.IsMe);
        if (player == null)
            return;
        var button = new NWitchEncyclopediaButton { Name = NodeName };
        button.Initialize(player, __instance);
        __instance.AddChild(button);
    }
    [HarmonyPatch(nameof(NCombatUi.Deactivate))]
    [HarmonyPatch("PostCombatCleanUp")]
    [HarmonyPostfix]
    private static void Remove(NCombatUi __instance)
    {
        var node = __instance.GetNodeOrNull<NWitchEncyclopediaButton>(NodeName);
        if (node == null)
            return;
        __instance.RemoveChild(node);
        node.QueueFree();
    }
}
