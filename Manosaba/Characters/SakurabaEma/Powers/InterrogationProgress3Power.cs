using BaseLib.Utils;
using Manosaba.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class InterrogationProgress3Power : InterrogationProgressPowerBase
{
    protected override Task AddDrawStack(PlayerChoiceContext choiceContext) =>
        CommonActions.Apply<InterrogationProgress3Power>(choiceContext, Owner, null, 1m);

    protected override async Task Advance(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        await CommonActions.Apply<InterrogationProgress4Power>(choiceContext, Owner, cardSource, InitialAmount);
        await PowerCmd.Remove(this);
    }

    protected override async Task ResolveProgressEffect(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player != null)
            await PlayerCmd.GainEnergy(1m, Owner.Player);

        await CreatureCmd.GainBlock(Owner, 5m, ValueProp.Unpowered, null);
        await AddStatementToAllPlayers(choiceContext);
    }
}
