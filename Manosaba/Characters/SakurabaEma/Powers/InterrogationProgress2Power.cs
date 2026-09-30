using BaseLib.Utils;
using Manosaba.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class InterrogationProgress2Power : InterrogationProgressPowerBase
{
    protected override Task AddDrawStack(PlayerChoiceContext choiceContext) =>
        CommonActions.Apply<InterrogationProgress2Power>(choiceContext, Owner, null, 1m);

    protected override async Task Advance(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        await CommonActions.Apply<InterrogationProgress3Power>(choiceContext, Owner, cardSource, InitialAmount);
        await PowerCmd.Remove(this);
    }

    protected override async Task ResolveProgressEffect(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player != null)
            await PlayerCmd.GainEnergy(1m, Owner.Player);

        await AddStatementToAllPlayers(choiceContext);
    }
}
