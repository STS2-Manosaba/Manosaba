using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace manosaba.Characters.SakurabaEma.Powers;

/// <summary>An inert creature counter. Interrogation Start interprets it as Strength.</summary>
public sealed class GuiltPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
}
