using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers;

public sealed class TrueCulpritPower : PathCustomPowerModel
{
    private const decimal AttackBonus = 2m;
    private const decimal BlockBonus = 2m;
    private const decimal EnergyBonus = 1m;
    private const decimal IncomingDamageBonus = 2m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override string CustomPackedIconPath => ModelDb.Power<GuiltPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<GuiltPower>().CustomBigIconPath;
    public override string CustomBigBetaIconPath => CustomBigIconPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("AttackBonus", AttackBonus),
        new DynamicVar("BlockBonus", BlockBonus),
        new DynamicVar("EnergyBonus", EnergyBonus),
        new DynamicVar("IncomingDamageBonus", IncomingDamageBonus),
    ];

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        decimal bonus = Owner == dealer && props.IsPoweredAttack() ? AttackBonus : 0m;
        if (target == Owner && amount > 0m)
            bonus += IncomingDamageBonus;
        return bonus;
    }

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        bool ownBlock = cardSource != null ? cardSource.Owner.Creature == Owner : target == Owner;
        return ownBlock && props.IsPoweredCardOrMonsterMoveBlock() ? BlockBonus : 0m;
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
        => Owner.Player == player ? amount + EnergyBonus : amount;
}
