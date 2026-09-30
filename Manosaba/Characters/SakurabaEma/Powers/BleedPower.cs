using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class BleedPower : PathCustomPowerModel
{
    private const string ThresholdVarName = "Threshold";
    private const string TriggerDamageVarName = "TriggerDamage";

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ThresholdVarName, 0m),
        new DynamicVar(TriggerDamageVarName, 0m),
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        RefreshDynamicValues();
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = amount;
        _ = applier;
        _ = cardSource;
        RefreshDynamicValues();
        if (power != this || Amount < GetThreshold())
            return;

        decimal damage = GetTriggerDamage();
        await PowerCmd.Remove(this);
        await CreatureCmd.Damage(choiceContext, Owner, damage, ValueProp.Unpowered, Owner, null);
    }

    private void RefreshDynamicValues()
    {
        DynamicVars[ThresholdVarName].BaseValue = GetThreshold();
        DynamicVars[TriggerDamageVarName].BaseValue = GetTriggerDamage();
        InvokeDisplayAmountChanged();
    }

    private decimal GetThreshold()
    {
        int playerCount = Owner.CombatState?.Players.Count() ?? 1;
        return Math.Floor(Owner.MaxHp * 0.10m + 100m * playerCount);
    }

    private decimal GetTriggerDamage() => Math.Floor(Owner.MaxHp * 0.15m + 100m);
}
