using Manosaba.Extensions;
using Manosaba.Input;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers;

public sealed class WitchIslandExpeditionPower : PathCustomPowerModel
{
    private const string ParrySuccessSfx = "event:/Manosaba/audio/SFX/parry_success.wav";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        PerfectGuardInputTracker.EnsureInstalled();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = amount;
        _ = cardSource;

        if (target != Owner)
        {
            return 1m;
        }

        bool canParry = dealer != null &&
            dealer.IsEnemy &&
            dealer.IsMonster &&
            !target.IsEnemy &&
            props.HasFlag(ValueProp.Move) &&
            !props.HasFlag(ValueProp.Unpowered);
        if (canParry && PerfectGuardInputTracker.TryConsumePerfectGuard(target))
        {
            SfxCmd.Play(ParrySuccessSfx);
            return 0m;
        }

        return canParry ? 3m : 1m;
    }
}
