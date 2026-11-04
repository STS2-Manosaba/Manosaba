using BaseLib.Extensions;
using BaseLib.Utils;
using Manosaba.Characters.TonoHanna.Visuals;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers;

/// <summary>Reduces incoming powered attack damage by 20%. Its stacks count down at the start of the owner's side turn.</summary>
public sealed class FloatingPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => false;

    public override LocString Title => new LocString("powers", Id.Entry + ".title");

    public override LocString Description => new LocString("powers", Id.Entry + ".description");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageDecrease", 20m)];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = amount;
        _ = dealer;
        _ = cardSource;

        if (target != Owner || Amount <= 0m || !props.IsPoweredAttack_())
        {
            return 1m;
        }

        return 1m - DynamicVars["DamageDecrease"].BaseValue / 100m;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = cardSource;
        await HannaSoarLiftVisual.RegisterLiftAsync(Owner, applier, this, HannaSoarLiftVisual.HalfLiftPixels);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await HannaSoarLiftVisual.RemoveLiftAsync(oldOwner, this);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        _ = combatState;
        if (side != Owner.Side || Amount <= 0m)
        {
            return;
        }

        Flash();
        await CommonActions.Apply<FloatingPower>(new ThrowingPlayerChoiceContext(), Owner, null, -1m, silent: true);
    }
}
