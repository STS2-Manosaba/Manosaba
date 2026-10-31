using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Powers;

public sealed class FingerSupportLinePower : PathCustomPowerModel
{
    private bool _tookUnblockedDamage;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _ = choiceContext;
        _ = props;
        _ = dealer;
        _ = cardSource;

        if (target == Owner && result.UnblockedDamage > 0m)
            _tookUnblockedDamage = true;

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = creatures;
        if (Owner.Player is not { } ownerPlayer || side == Owner.Side)
            return;

        if (!_tookUnblockedDamage)
            await PlayerCmd.GainGold(Amount * 7m, ownerPlayer);

        await PowerCmd.Remove(this);
    }
}
