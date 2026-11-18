using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.HoshoMago.Powers;

public sealed class TheStarPower : PathCustomPowerModel
{
    private const decimal PercentScale = 100m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public static decimal EncodeReflectMultiplier(decimal multiplier) => multiplier * PercentScale;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _ = cardSource;

        if (ReflectionDamageGuard.IsActive)
        {
            return;
        }

        if (target != Owner || dealer == null || !props.IsPoweredAttack())
        {
            return;
        }

        decimal reflectedAmount = result.TotalDamage * ReflectMultiplier;
        int reflectedDamage = (int)Math.Floor(reflectedAmount);
        if (reflectedDamage <= 0)
        {
            return;
        }

        await ReflectionDamageGuard.Run(() =>
            CreatureCmd.Damage(choiceContext, dealer, reflectedDamage, ValueProp.Unpowered, Owner, null, null));
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }

    private decimal ReflectMultiplier => Amount / PercentScale;
}
