using BaseLib.Utils;
using Manosaba.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class InterrogationProgress4Power : InterrogationProgressPowerBase
{
    protected override Task AddDrawStack(PlayerChoiceContext choiceContext) =>
        CommonActions.Apply<InterrogationProgress4Power>(choiceContext, Owner, null, 1m);

    protected override async Task Advance(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        await PowerCmd.Remove(this);
        await CommonActions.Apply<InterrogationProgress4Power>(choiceContext, Owner, cardSource, InitialAmount);
    }

    protected override async Task ResolveProgressEffect(PlayerChoiceContext choiceContext)
    {
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
            return;

        foreach (Player player in combatState.Players)
        {
            if (player?.Creature is not { IsAlive: true } creature)
                continue;

            await PlayerCmd.GainEnergy(1m, player);
            await CreatureCmd.GainBlock(creature, 5m, ValueProp.Unpowered, null);
        }

        await AddStatementToAllPlayers(choiceContext);
    }
}
