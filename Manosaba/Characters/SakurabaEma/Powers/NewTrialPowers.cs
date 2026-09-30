using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Manosaba.Characters.Common.Powers;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class RetrialExtraTurnPower : PathCustomPowerModel
{
    public override string CustomPackedIconPath => ModelDb.Power<ArgumentPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldTakeExtraTurn(Player player) => player == Owner.Player && Amount > 0;
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player)
            await PowerCmd.Decrement(this);
    }
}

public sealed class ScabbardPower : PathCustomPowerModel
{
    public override string CustomPackedIconPath => ModelDb.Power<GuiltPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageCap", 10m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MurderousImpulsePower>()];
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var attackingPlayer = dealer?.Player ?? cardSource?.Owner;
        return target == Owner && attackingPlayer != null && attackingPlayer != Owner.Player &&
               attackingPlayer.Creature.Side == Owner.Side
            ? DynamicVars["DamageCap"].BaseValue : decimal.MaxValue;
    }
}