using HarmonyLib;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Patches;

// Only actual draws replace these deck cards; generating or presenting evidence does not.
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardDrawn))]
public static class Patch_EvidenceDraw
{
    [HarmonyPostfix]
    private static void Postfix(ref Task __result, PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card is HirosPen or NoahCorpsePhoto or MiriaCorpsePhoto or HannaCorpsePhoto
            or AlisaCorpsePhoto or NanokaCorpsePhoto)
            __result = ReplaceDraw(__result, choiceContext, card);
    }

    private static async Task ReplaceDraw(Task original, PlayerChoiceContext context, CardModel card)
    {
        await original;
        await CardPileCmd.Draw(context, 1m, card.Owner);
    }
}
