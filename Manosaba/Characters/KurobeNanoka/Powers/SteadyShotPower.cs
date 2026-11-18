using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class SteadyShotPower : PathCustomPowerModel
{
    private const string ExtraDamagePerShotVar = "ExtraDamagePerShot";
    private const string NextShotBonusVar = "NextShotBonus";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => Amount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ExtraDamagePerShotVar, 0m),
        new DynamicVar(NextShotBonusVar, 0m),
    ];

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

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = target;
        _ = amount;

        if (dealer != Owner)
        {
            return 0m;
        }

        if (cardSource is not GunBase || cardSource.Type != CardType.Attack)
        {
            return 0m;
        }

        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
        {
            return 0m;
        }

        return DynamicVars[NextShotBonusVar].BaseValue;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        if (cardPlay.Card is not GunBase || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        DynamicVars[NextShotBonusVar].BaseValue += Amount;
        RefreshDerivedValues();
        await Task.CompletedTask;
    }

    private void RefreshDerivedValues()
    {
        DynamicVars[ExtraDamagePerShotVar].BaseValue = Amount;
        InvokeDisplayAmountChanged();
    }
}
