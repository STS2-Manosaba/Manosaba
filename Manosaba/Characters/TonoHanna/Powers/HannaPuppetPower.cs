using System.Collections.Generic;
using Manosaba.Characters.TonoHanna.Visuals;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Powers;

/// <summary>Same damage reduction as <see cref="SoarPower"/>; removed at the next <see cref="CombatSide"/> turn start for <see cref="Creature.Side"/>. Uses vanilla Soar power icons.</summary>
public sealed class HannaPuppetPower : PathCustomPowerModel
{
    private static PowerModel SoarIconSource => ModelDb.Power<SoarPower>();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => SoarIconSource.PackedIconPath;

    public override string CustomBigIconPath => SoarIconSource.ResolvedBigIconPath;

    public override string CustomBigBetaIconPath => SoarIconSource.ResolvedBigIconPath;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageDecrease", 50m)];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner)
            return 1m;
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return 1m;
        return DynamicVars["DamageDecrease"].BaseValue / 100m;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = cardSource;
        await HannaSoarLiftVisual.RegisterLiftAsync(Owner, applier, this, HannaSoarLiftVisual.FullLiftPixels);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await HannaSoarLiftVisual.RemoveLiftAsync(oldOwner, this);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            await PowerCmd.Remove(this);
        }
    }
}
