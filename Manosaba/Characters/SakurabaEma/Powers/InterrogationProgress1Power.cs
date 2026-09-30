using BaseLib.Utils;
using Manosaba.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class InterrogationProgress1Power : InterrogationProgressPowerBase
{
    protected override Task AddDrawStack(PlayerChoiceContext choiceContext) =>
        CommonActions.Apply<InterrogationProgress1Power>(choiceContext, Owner, null, 1m);

    protected override async Task Advance(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        await CommonActions.Apply<InterrogationProgress2Power>(choiceContext, Owner, cardSource, InitialAmount);
        await PowerCmd.Remove(this);
    }

    protected override Task ResolveProgressEffect(PlayerChoiceContext choiceContext) => AddStatementToAllPlayers(choiceContext);
}
