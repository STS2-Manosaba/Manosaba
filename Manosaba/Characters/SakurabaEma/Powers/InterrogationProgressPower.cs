using Manosaba.Combat;
using manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace manosaba.Characters.SakurabaEma.Powers;

/// <summary>A shared field counter of Trial card plays, unlocking conditional effects at ten.</summary>
public sealed class InterrogationProgressPower : FieldPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShowAtZero => true;
    public override string CustomPackedIconPath => "interrogation_progress1_power.png".PowerImagePath();
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
}
