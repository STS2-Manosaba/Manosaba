using HarmonyLib;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class Patch_Hook_AfterCardPlayed_InterrogationProgress
{
    [HarmonyPostfix]
    private static Task Postfix(Task __result, ICombatState combatState, CardPlay cardPlay) =>
        CountAfterPlay(__result, combatState, cardPlay);

    private static async Task CountAfterPlay(Task originalTask, ICombatState field, CardPlay cardPlay)
    {
        // Await successful resolution; no count for a canceled/failed play. This runs
        // once per CardPlay, independent of player count or number of field powers.
        await originalTask;
        if (FieldPowerState.Has<InterrogationStartPower>(field) &&
            cardPlay.Card.Keywords.Contains(ManosabaKeywords.Trial))
            FieldPowerState.Apply<InterrogationProgressPower>(field, 1);
    }
}
