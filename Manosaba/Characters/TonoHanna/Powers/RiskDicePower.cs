using BaseLib.Abstracts;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.TonoHanna.Powers;

public sealed class RiskDicePower : PathCustomPowerModel
{
    public const decimal InitialGoodChance = 95m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("GoodChance", InitialGoodChance),
        new DynamicVar("UsesSinceMisfortune", 0m),
    ];

    public decimal GoodChance => DynamicVars["GoodChance"].BaseValue;
    public decimal UsesSinceMisfortune => DynamicVars["UsesSinceMisfortune"].BaseValue;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public void RecordUse() => DynamicVars["UsesSinceMisfortune"].BaseValue += 1m;

    public void RecordSuccess() => DynamicVars["GoodChance"].BaseValue = Math.Max(0m, GoodChance - 5m);

    public void ResetAfterMisfortune()
    {
        DynamicVars["GoodChance"].BaseValue = InitialGoodChance;
        DynamicVars["UsesSinceMisfortune"].BaseValue = 0m;
    }
}
