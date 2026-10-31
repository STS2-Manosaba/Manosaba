using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.KurobeNanoka.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class BurningWound : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BurningWoundPower>(30m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BurningWoundPower>()];

    public BurningWound() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        CommonActions.Apply<BurningWoundPower>(context, Owner.Creature, this, 1m);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
