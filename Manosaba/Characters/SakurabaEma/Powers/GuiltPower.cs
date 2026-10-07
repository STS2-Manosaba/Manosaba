using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

/// <summary>An inert creature counter. Interrogation Start interprets it as Strength.</summary>
public sealed class GuiltPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override string CustomPackedIconPath => ModelDb.Power<EvidencePower>().CustomPackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<EvidencePower>().CustomBigIconPath;
    public override string CustomBigBetaIconPath => CustomBigIconPath;
}
