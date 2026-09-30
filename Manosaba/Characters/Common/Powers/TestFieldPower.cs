using Manosaba.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers;

public sealed class TestFieldPower : FieldPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>().PackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>().ResolvedBigIconPath;
    public override string CustomBigBetaIconPath => CustomBigIconPath;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        dealer != null && !props.HasFlag(ValueProp.Unpowered) ? Amount : 0m;
}
