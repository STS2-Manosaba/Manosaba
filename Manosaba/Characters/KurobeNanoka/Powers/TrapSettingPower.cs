using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class TrapSettingPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => _activeThisTurn ? 2 : 1;

    private int _appliedRound;
    private bool _activeThisTurn;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;

        _appliedRound = Owner.CombatState?.RoundNumber ?? 0;
        _activeThisTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private static bool ShouldAffectDamage(ValueProp props)
    {
        return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = amount;

        if (!_activeThisTurn)
        {
            return 1m;
        }

        if (target != Owner || dealer == null)
        {
            return 1m;
        }

        if (cardSource is not GunBase)
        {
            return 1m;
        }

        return ShouldAffectDamage(props) ? 2m : 1m;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != CombatSide.Player)
        {
            return;
        }

        // Activates on the player's *next* turn after being applied.
        if (combatState.RoundNumber == _appliedRound + 1)
        {
            _activeThisTurn = true;
            Flash();
            InvokeDisplayAmountChanged();
            return;
        }

        // Safety: if we somehow made it past the intended window, remove.
        if (combatState.RoundNumber > _appliedRound + 1)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = choiceContext;

        // Remove at the end of the turn it was active for.
        if (side == CombatSide.Player && _activeThisTurn)
        {
            await PowerCmd.Remove(this);
        }
    }
}
