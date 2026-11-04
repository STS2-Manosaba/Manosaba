using HarmonyLib;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Relics;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;
using HiroCharacter = manosaba.Characters.NikaidoHiro.NikaidoHiro;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(TheArchitect), "LoadDialogue")]
internal static class Patch_TheArchitect_EmaDialogue
{
    [HarmonyPrefix]
    private static bool Prefix(TheArchitect __instance, ref AncientDialogue? ____dialogue)
    {
        if (__instance.Owner is not { Character: SakurabaEma } ema)
            return true;

        int branch = ema.RunState.Players.Any(player => player != ema && player.Character is HiroCharacter)
            ? 0
            : YouDidWell.Level(ema) < 5 ? 1 : 2;

        // BaseLib registers the localized conversations by index. Choose by run state instead of win count.
        if (!__instance.DialogueSet.CharacterDialogues.TryGetValue(ema.Character.Id.Entry, out var dialogues)
            || branch >= dialogues.Count)
            return true;

        ____dialogue = dialogues[branch];
        return false;
    }
}
