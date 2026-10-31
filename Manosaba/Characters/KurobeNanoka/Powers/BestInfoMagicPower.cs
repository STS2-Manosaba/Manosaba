using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class BestInfoMagicPower : PathCustomPowerModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Chance", 30m)];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player.Creature == Owner && player.Creature.Player is { } viewer)
            PreviewTopCard(viewer, Owner);

        return Task.CompletedTask;
    }

    private void PreviewTopCard(MegaCrit.Sts2.Core.Entities.Players.Player viewer, Creature? caster)
    {
        decimal accuracy = 30m + Math.Min(70m, (caster ?? Owner).GetPowerAmount<MajokaPower>() * 0.7m);
        DynamicVars["Chance"].BaseValue = accuracy;
        InvokeDisplayAmountChanged();

        IReadOnlyList<CardModel> drawPile = PileType.Draw.GetPile(viewer).Cards;
        int cardCount = Math.Min(Math.Max(0, (int)Amount), drawPile.Count);
        if (cardCount <= 0)
            return;

        var rng = viewer.RunState.Rng.CombatCardSelection;
        List<CardModel> shownCards = [];
        for (int index = 0; index < cardCount; index++)
        {
            CardModel actualCard = drawPile[index];
            if (rng.NextInt(100) < (int)accuracy || drawPile.Count == 1)
            {
                shownCards.Add(actualCard);
                continue;
            }

            List<CardModel> alternatives = drawPile
                .Where((_, candidateIndex) => candidateIndex != index)
                .ToList();
            shownCards.Add(alternatives[rng.NextInt(alternatives.Count)]);
        }

        CardCmd.Preview(shownCards, 3f, CardPreviewStyle.HorizontalLayout);
    }
}
