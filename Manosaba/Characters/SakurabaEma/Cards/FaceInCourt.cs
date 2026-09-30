using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class FaceInCourt : EmaTrialCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SusPower>(1m),
        new CardsVar(3),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SusPower>(),
        HoverTipFactory.FromCard<Statement>(),
    ];

    public FaceInCourt() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target?.Player is not { } targetPlayer || CombatState == null)
            return;

        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, DynamicVars["SusPower"].BaseValue);
        await CommonActions.Apply<SusPower>(choiceContext, cardPlay.Target, this, DynamicVars["SusPower"].BaseValue);

        List<CardPileAddResult> results = [];
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            results.Add(await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<Statement>(Owner), PileType.Hand, Owner));
            results.Add(await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<Statement>(targetPlayer), PileType.Hand, targetPlayer));
        }

        CardCmd.PreviewCardPileAdd(results);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
