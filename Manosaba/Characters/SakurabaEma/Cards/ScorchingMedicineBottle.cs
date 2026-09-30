using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
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
public sealed class ScorchingMedicineBottle : EmaTrialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SusPower>(1m),
        new PowerVar<BurnPower>(15m),
        new CardsVar(1),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SusPower>(),
        HoverTipFactory.FromPower<BurnPower>(),
        HoverTipFactory.FromCard<Statement>(),
    ];

    public ScorchingMedicineBottle() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, DynamicVars["SusPower"].BaseValue);

        if (cardPlay.Target != null)
            await CommonActions.Apply<BurnPower>(choiceContext, cardPlay.Target, this, DynamicVars["BurnPower"].BaseValue);

        if (CombatState == null)
            return;

        CardModel statement = CombatState.CreateCard<Statement>(Owner);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(statement, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected override void OnUpgrade() => DynamicVars["BurnPower"].UpgradeValueBy(5m);
}
