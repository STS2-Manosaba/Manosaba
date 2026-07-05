using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public sealed class SlayTheSpireIi : PathCustomCardModel
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;
    private const int baseCardCount = 8;

    private static readonly LocString SelectionPrompt = new("cards", "MANOSABA-SLAY_THE_SPIRE_II.selectionScreenPrompt");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MinCards", baseCardCount),
        new CardsVar(baseCardCount),
    ];

    protected override bool IsPlayable => base.IsPlayable && GetCombatPileCardCount() > DynamicVars["MinCards"].IntValue;

    public SlayTheSpireIi() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        List<CardModel> candidates =
        [
            .. PileType.Hand.GetPile(Owner).Cards,
            .. PileType.Draw.GetPile(Owner).Cards,
            .. PileType.Discard.GetPile(Owner).Cards,
        ];

        if (candidates.Count == 0)
        {
            return;
        }

        int keepCount = DynamicVars.Cards.IntValue;
        var prefs = new CardSelectorPrefs(SelectionPrompt, keepCount);

        IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            prefs)).ToList();

        if (selected.Count == 0)
        {
            return;
        }

        List<CardModel> toRemove = candidates.Except(selected).ToList();
        if (toRemove.Count > 0)
        {
            await CardPileCmd.RemoveFromCombat(toRemove);
        }

        await CardPileCmd.Add(selected, PileType.Hand, CardPilePosition.Bottom, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MinCards"].UpgradeValueBy(-2m);
        DynamicVars.Cards.UpgradeValueBy(-2m);
    }

    private int GetCombatPileCardCount()
        => CardPile.GetCards(Owner, PileType.Hand, PileType.Draw, PileType.Discard).Count();
}
