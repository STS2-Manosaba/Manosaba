using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.GameActions.Multiplayer;
namespace Manosaba.Characters.HasumiLeia.Powers;

public sealed class GazeGuidingPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageReductionPercent", 0m)];
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncDamageReductionPercent();
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = amount;
        _ = props;
        _ = cardSource;

        if (target == null || dealer != Owner || !target.IsPlayer)
            return 1m;

        return 1m - Math.Clamp(Amount / 100m, 0m, 1m);
    }

    private void SyncDamageReductionPercent()
    {
        DynamicVars["DamageReductionPercent"].BaseValue = Math.Clamp(Amount, 0m, 100m);
        InvokeDisplayAmountChanged();
    }
}

