using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers;

public sealed class WitchLanguageMasteryPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => ModelDb.Power<GuiltPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    private const int MultiplierPerStack = 2;
    private const string MultiplierKey = "Multiplier";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(MultiplierKey, MultiplierPerStack)];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpdateMultiplier();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power == this) UpdateMultiplier();
        return Task.CompletedTask;
    }

    private void UpdateMultiplier() => DynamicVars[MultiplierKey].BaseValue = (decimal)Amount * MultiplierPerStack;

    // This hook receives the result for each actual recipient, including lethal hits.
    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (target != Owner || result.TotalDamage <= 0 || Amount <= 0 || Owner.CombatState is not { } combat)
            return;

        Flash();
        decimal loss = (decimal)result.TotalDamage * MultiplierPerStack * Amount;
        await CreatureCmd.Damage(context, combat.GetOpponentsOf(Owner).Where(enemy => enemy.IsAlive),
            loss, DamageProps.nonCardHpLoss, null, null);
    }
}
