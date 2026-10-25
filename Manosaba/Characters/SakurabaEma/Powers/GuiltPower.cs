using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace manosaba.Characters.SakurabaEma.Powers;

/// <summary>An inert counter used by Interrogation Start to boost Trial attacks and True Culprits' attacks.</summary>
public sealed class GuiltPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
}
