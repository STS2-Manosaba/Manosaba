using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.NatsumeAnan.Powers;

public sealed class SanityPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (amount <= 0 || card.Owner?.Creature != Owner || !Owner.IsAlive)
        {
            return;
        }

        Flash();
        int remaining = await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -amount, Owner, card);
        if (remaining <= 0 && Owner.IsAlive)
        {
            await CreatureCmd.Kill(Owner);
        }
    }
}

public sealed class CallOfCthulhuPower : PathCustomPowerModel
{
    private const int DamageLimitPerTurn = 15;

    private decimal _damageTakenThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamageLimit", DamageLimitPerTurn),
        new PowerVar<StrengthPower>(1m),
    ];

    private decimal DamageTakenThisTurn
    {
        get => _damageTakenThisTurn;
        set
        {
            AssertMutable();
            _damageTakenThisTurn = value;
        }
    }

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = props;
        _ = dealer;
        _ = cardSource;

        if (!CombatManager.Instance.IsInProgress || target != Owner || amount <= 0m)
        {
            return amount;
        }

        return Math.Min(amount, Math.Max(0m, DynamicVars["DamageLimit"].BaseValue - DamageTakenThisTurn));
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = choiceContext;
        _ = props;
        _ = dealer;
        _ = cardSource;

        if (target == Owner && result.UnblockedDamage > 0)
        {
            DamageTakenThisTurn += result.UnblockedDamage;
        }

        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        _ = choiceContext;
        _ = combatState;

        if (side == Owner.Side)
        {
            DamageTakenThisTurn = 0m;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        _ = combatState;

        if (side != Owner.Side || !Owner.IsAlive)
        {
            return;
        }

        await CommonActions.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, null, DynamicVars["StrengthPower"].BaseValue);
    }
}

public sealed class ShadowFromTheSteeplePower : PathCustomPowerModel
{
    private const decimal DamageMultiplierPerStack = 0.5m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamageMultiplierPercent", 50m),
    ];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = amount;
        _ = props;
        _ = dealer;
        _ = cardSource;

        return target == Owner ? 1m + Amount * DamageMultiplierPerStack : 1m;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = choiceContext;

        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
