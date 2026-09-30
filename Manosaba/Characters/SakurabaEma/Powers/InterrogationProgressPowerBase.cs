using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public abstract class InterrogationProgressPowerBase : PathCustomPowerModel
{
    private const int CardsDrawnThreshold = 10;
    public const decimal InitialAmount = 1m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override int DisplayAmount => (int)Math.Max(0m, Amount - InitialAmount);

    protected abstract Task AddDrawStack(PlayerChoiceContext choiceContext);
    protected abstract Task ResolveProgressEffect(PlayerChoiceContext choiceContext);
    protected abstract Task Advance(PlayerChoiceContext choiceContext, CardModel? cardSource);

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        _ = fromHandDraw;

        if (card.Owner != Owner.Player || Owner.CombatState == null)
            return;

        await AddDrawStack(choiceContext);

        if (DisplayAmount < CardsDrawnThreshold)
            return;

        Flash();
        await ResolveProgressEffect(choiceContext);
        await Advance(choiceContext, null);
    }

    protected async Task AddStatementToAllPlayers(PlayerChoiceContext choiceContext)
    {
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
            return;

        List<CardPileAddResult> results = [];
        foreach (Player player in combatState.Players)
        {
            if (player?.Creature is not { IsAlive: true })
                continue;

            CardModel statement = combatState.CreateCard<Statement>(player);
            results.Add(await CardPileCmd.AddGeneratedCardToCombat(statement, PileType.Hand, player));
        }

        if (results.Count > 0)
            CardCmd.PreviewCardPileAdd(results);
    }
}
