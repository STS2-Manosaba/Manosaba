using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class HallucinationMagic : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;
    private const string SelectableCardsVar = "SelectableCardsVar";
    private static readonly LocString Prompt = new("cards", "MANOSABA-HALLUCINATION_MAGIC.selectionScreenPrompt");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Mahou, CardKeyword.Eternal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(10m),
        new CalculatedVar(SelectableCardsVar).WithMultiplier(GetMajokaFactor),
    ];

    public HallucinationMagic()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> candidates =
        [
            //.. PileType.Hand.GetPile(Owner).Cards,
            .. PileType.Draw.GetPile(Owner).Cards,
            .. PileType.Discard.GetPile(Owner).Cards,
        ];

        if (candidates.Count == 0)
        {
            return;
        }

        int maxSelectable = (int)Math.Min(((CalculatedVar)DynamicVars[SelectableCardsVar]).Calculate(null), candidates.Count);
        if (maxSelectable <= 0)
        {
            return;
        }

        IReadOnlyList<CardModel> selected = (IReadOnlyList<CardModel>)await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            CreateSelectorPrefs(maxSelectable));

        if (selected.Count == 0)
        {
            return;
        }

        List<CardModel> drawPileCards = PileType.Draw.GetPile(Owner).Cards.ToList();
        drawPileCards.StableShuffle(Owner.RunState.Rng.Shuffle);
        foreach (CardModel card in drawPileCards)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom, this, skipVisuals: true);
        }

        // Reverse so the first selected card ends up highest on the draw pile.
        await CardPileCmd.Add(selected.Reverse<CardModel>(), PileType.Draw, CardPilePosition.Top, this, skipVisuals: false);

        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private CardSelectorPrefs CreateSelectorPrefs(int maxSelectable)
    {
        CardSelectorPrefs prefs = new(Prompt, 1, maxSelectable);
        prefs.ShouldGlowGold = card => card.Pile?.Type == PileType.Draw;
        return prefs;
    }

    private static decimal GetMajokaFactor(CardModel card, Creature? _)
        => Math.Min(card.Owner.Creature.GetPowerAmount<MajokaPower>() / 100m, 1m);
}
