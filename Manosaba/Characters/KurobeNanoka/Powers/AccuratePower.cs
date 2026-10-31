using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class AccuratePower : PathCustomPowerModel
{
    private const decimal StackScale = 100m;
    private const string MultiplierVar = "Multiplier";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => Amount;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(MultiplierVar, 1m),
    ];

    private static bool ShouldAffectDamage(ValueProp props)
    {
        return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        RefreshDerivedValues();
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power != this)
        {
            return;
        }

        RefreshDerivedValues();
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _ = target;
        _ = amount;

        if (!ShouldAffectDamage(props))
        {
            return 1m;
        }

        if (dealer != Owner)
        {
            return 1m;
        }

        if (cardSource is not GunBase || cardSource.Type != CardType.Attack)
        {
            return 1m;
        }

        if (Amount < 1m)
        {
            return 1m;
        }

        return 1m + Amount / StackScale;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        if (cardPlay.Card is not GunBase gunCard || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        if (!gunCard.SpentBulletsThisPlay)
        {
            Console.WriteLine($"[AccuratePower] Skipping consume because bullets were not spent. ownerNetId={Owner?.Player?.NetId} ownerName={Owner?.Name} card={cardPlay.Card.Id} amount={Amount}");
            return;
        }

        Console.WriteLine($"[AccuratePower] Consuming after gun attack. ownerNetId={Owner?.Player?.NetId} ownerName={Owner?.Name} card={cardPlay.Card.Id} amount={Amount}");
        if (Owner.GetPower<CampingPower>() is not null)
            return;

        await PowerCmd.Remove(this);
    }

    private void RefreshDerivedValues()
    {
        DynamicVars[MultiplierVar].BaseValue = 1m + Amount / StackScale;
        InvokeDisplayAmountChanged();
    }
}
