using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Utils;

namespace Manosaba.Characters.Common.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class LibraryMagnifyingGlass : PathCustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override string PackedIconPath => "res://Manosaba/images/relics/magnifying_glass.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PerStack", 2), new DynamicVar("Cap", 20)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<GuiltPower>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    private decimal Bonus => Math.Min(DynamicVars["Cap"].BaseValue,
        Owner.Creature.GetPowerAmount<GuiltPower>() * DynamicVars["PerStack"].BaseValue);

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) => dealer == Owner.Creature && !props.HasFlag(ValueProp.Unpowered) ? Bonus : 0;

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props,
        CardModel? cardSource, CardPlay? cardPlay) => target == Owner.Creature && props.IsPoweredCardOrMonsterMoveBlock() ? Bonus : 0;
}

[Pool(typeof(EventRelicPool))]
public sealed class LibraryHannaDoll : PathCustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override string PackedIconPath => "res://Manosaba/images/powers/hanna_puppet_collection.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PerStack", 3), new DynamicVar("Cap", 30)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];

    public override Task AfterCombatEnd(CombatRoom room)
    {
        int gold = (int)Math.Min(DynamicVars["Cap"].BaseValue,
            Owner.Creature.GetPowerAmount<GuiltPower>() * DynamicVars["PerStack"].BaseValue);
        if (gold > 0 && Owner.Creature.IsAlive)
        {
            Flash();
            room.AddExtraReward(Owner, new GoldReward(gold, Owner));
        }
        return Task.CompletedTask;
    }
}
