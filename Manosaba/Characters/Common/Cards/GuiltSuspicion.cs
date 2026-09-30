using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class GuiltSuspicion : PathCustomCardModel
{
    // Intentionally has no upgrade, as requested. It can still affect a solo hand at turn end.
    public override int MaxUpgradeLevel => 0;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override bool HasTurnEndInHandEffect => true;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GuiltPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];
    public override string PortraitPath => ModelDb.Card<BadEnd>().PortraitPath;

    public GuiltSuspicion() : base(1, CardType.Status, CardRarity.Token, TargetType.AnyAlly, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target?.Player is not { } teammate || CombatState is not { } combat)
            return;

        await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<GuiltSuspicion>(teammate), PileType.Hand, teammate);
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
        => await CommonActions.Apply<GuiltPower>(choiceContext, Owner.Creature, this, DynamicVars["GuiltPower"].BaseValue);
}
