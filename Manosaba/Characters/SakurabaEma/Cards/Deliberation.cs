using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Deliberation : EmaTrialCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [ManosabaKeywords.Trial] : [ManosabaKeywords.Trial, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public Deliberation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        int maxSelect = PileType.Hand.GetPile(Owner).Cards.Count;
        if (maxSelect <= 0)
            return;

        IEnumerable<CardModel> selected = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, maxSelect),
            null,
            this);

        List<CardModel> cards = selected.ToList();
        if (cards.Count <= 0)
            return;

        await CardCmd.Discard(choiceContext, cards);
        await CardPileCmd.Draw(choiceContext, cards.Count, Owner);
    }

    protected override void OnUpgrade()
    {
    }
}
