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
public sealed class WardenAttack : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WardenAttackPower>(10m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WardenAttackPower>()];

    public WardenAttack() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        CommonActions.Apply<WardenAttackPower>(context, Owner.Creature, this, DynamicVars["WardenAttackPower"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["WardenAttackPower"].UpgradeValueBy(5m);
}
