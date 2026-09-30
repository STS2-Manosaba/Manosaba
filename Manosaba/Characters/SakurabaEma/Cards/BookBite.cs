using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class BookBite : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VulnerablePower>(3m),
        new CardsVar(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromCard<LivingPage>(),
    ];

    public BookBite() : base(2, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null || CombatState == null)
        {
            return;
        }

        await CommonActions.Apply<VulnerablePower>(choiceContext, cardPlay.Target, this, DynamicVars["VulnerablePower"].BaseValue);

        List<CardModel> cards = [];
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            cards.Add(CombatState.CreateCard<LivingPage>(Owner));
        }

        IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(results);
    }

    protected override void OnUpgrade()
    {
    }
}
