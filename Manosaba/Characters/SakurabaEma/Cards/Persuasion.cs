using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Persuasion : PathCustomCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new EnergyVar(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        EnergyHoverTip,
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>(),
        HoverTipFactory.FromPower<EnergyNextTurnPower>(),
    ];

    public Persuasion() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await ApplyNextTurnEffects(choiceContext, Owner.Creature);
        await ApplyNextTurnEffects(choiceContext, cardPlay.Target);
    }

    private async Task ApplyNextTurnEffects(PlayerChoiceContext choiceContext, Creature target)
    {
        await CommonActions.Apply<DrawCardsNextTurnPower>(choiceContext, target, this, DynamicVars.Cards.BaseValue);
        await CommonActions.Apply<EnergyNextTurnPower>(choiceContext, target, this, DynamicVars.Energy.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
