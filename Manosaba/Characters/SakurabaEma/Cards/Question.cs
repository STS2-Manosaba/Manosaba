using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Question : EmaTrialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar("Options", 3m),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromKeyword(ManosabaKeywords.Testimony)];

    public Question() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (CombatState == null)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        List<CardModel> testimonyCards = TestimonyCardHelper.CreateAll(CombatState, Owner)
            .OrderBy(_ => Owner.RunState.Rng.CombatCardSelection.NextInt(int.MaxValue))
            .ToList();

        CardModel? selected = IsUpgraded
            ? await CardSelectCmd.FromChooseACardScreen(choiceContext, testimonyCards.Take(DynamicVars["Options"].IntValue).ToList(), Owner)
            : testimonyCards.FirstOrDefault();

        if (selected == null)
        {
            return;
        }

        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
