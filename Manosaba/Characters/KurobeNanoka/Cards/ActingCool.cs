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
public sealed class ActingCool : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ActingCoolPower>(40m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ActingCoolPower>()];

    public ActingCool() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        CommonActions.Apply<ActingCoolPower>(context, Owner.Creature, this, DynamicVars["ActingCoolPower"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["ActingCoolPower"].UpgradeValueBy(20m);
}
