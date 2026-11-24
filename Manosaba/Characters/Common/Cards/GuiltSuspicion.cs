using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class GuiltSuspicion : PathCustomCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override bool HasTurnEndInHandEffect => true;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GuiltPower>(2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];

    public GuiltSuspicion() : base(1, CardType.Status, CardRarity.Token, TargetType.AnyAlly, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target?.Player is not { } teammate || CombatState is not { } combat)
            return;

        var suspicion = combat.CreateCard<GuiltSuspicion>(teammate);
        if (IsUpgraded) CardCmd.Upgrade(suspicion);
        await CardPileCmd.AddGeneratedCardToCombat(suspicion, PileType.Hand, teammate);
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
        => await CommonActions.Apply<GuiltPower>(choiceContext, Owner.Creature, this, DynamicVars["GuiltPower"].BaseValue);
}
