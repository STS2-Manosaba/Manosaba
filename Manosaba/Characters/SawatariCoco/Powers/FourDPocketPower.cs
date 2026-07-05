using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Manosaba.Characters.SawatariCoco.Powers;

/// <summary>百寶袋／四次元ポケット：同系列裝備只差一件時，自動穿上最後一件（不觸發裝備卡效果）。</summary>
public sealed class FourDPocketPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
