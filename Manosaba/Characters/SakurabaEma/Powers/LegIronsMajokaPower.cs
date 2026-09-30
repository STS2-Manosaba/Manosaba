using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class LegIronsMajokaPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        _ = applier;

        modifiedAmount = amount;
        if (target != Owner || canonicalPower is not MajokaPower || amount <= 0m)
            return false;

        decimal multiplier = Amount / 100m;
        modifiedAmount = Math.Max(1m, Math.Floor(amount * multiplier));
        return true;
    }
}
