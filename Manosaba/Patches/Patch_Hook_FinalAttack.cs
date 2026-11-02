using HarmonyLib;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.HasumiLeia.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class Patch_Hook_FinalAttack
{
    [HarmonyPostfix]
    private static Task Postfix(Task __result, ICombatState combatState, CardPlay cardPlay) =>
        TriggerFinalAttackAfterResolution(__result, combatState, cardPlay);

    private static async Task TriggerFinalAttackAfterResolution(
        Task originalTask,
        ICombatState combatState,
        CardPlay cardPlay)
    {
        await originalTask;

        if (cardPlay.Card is FinalAttack ||
            !cardPlay.Card.CanonicalKeywords.Contains(ManosabaKeywords.SwordTechnique) ||
            cardPlay.Card.Owner is not { } owner)
        {
            return;
        }

        FinalAttack? finalAttack = PileType.Hand.GetPile(owner).Cards
            .OfType<FinalAttack>()
            .FirstOrDefault();
        if (finalAttack == null)
        {
            return;
        }

        Creature? target = cardPlay.Target is { IsEnemy: true, IsAlive: true, IsHittable: true }
            ? cardPlay.Target
            : combatState.HittableEnemies.FirstOrDefault();
        if (target == null)
        {
            return;
        }

        await CardCmd.AutoPlay(
            new ThrowingPlayerChoiceContext(),
            finalAttack,
            target,
            AutoPlayType.Default,
            skipXCapture: true);
    }
}
