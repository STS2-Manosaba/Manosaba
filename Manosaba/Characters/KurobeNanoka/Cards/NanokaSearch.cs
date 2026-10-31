using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class NanokaSearch : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(3), new CardsVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<EnergyNextTurnPower>(), HoverTipFactory.FromPower<DrawCardsNextTurnPower>()];

    public NanokaSearch() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<EnergyNextTurnPower>(context, Owner.Creature, this, DynamicVars.Energy.BaseValue);
        await CommonActions.Apply<DrawCardsNextTurnPower>(context, Owner.Creature, this, DynamicVars.Cards.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
