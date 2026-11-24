using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.Common.Powers;

public abstract class ManosabaTemporaryStatPower<TStat> : PathCustomPowerModel, ITemporaryPower where TStat : PowerModel
{
    private bool _shouldIgnoreNextInstance;

    public override PowerType Type => IsPositive ? PowerType.Buff : PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public abstract AbstractModel? OriginModel { get; }

    AbstractModel ITemporaryPower.OriginModel => OriginModel ?? InternallyAppliedPower;

    public PowerModel InternallyAppliedPower => ModelDb.Power<TStat>();

    protected virtual bool IsPositive => true;

    private int Sign => IsPositive ? 1 : -1;

    public void IgnoreNextInstance()
    {
        _shouldIgnoreNextInstance = true;
    }

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_shouldIgnoreNextInstance)
        {
            _shouldIgnoreNextInstance = false;
        }
        else
        {
            await CommonActions.Apply<TStat>(new ThrowingPlayerChoiceContext(), target, cardSource, (decimal)Sign * amount);
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && amount != Amount)
        {
            if (_shouldIgnoreNextInstance)
            {
                _shouldIgnoreNextInstance = false;
            }
            else
            {
                await CommonActions.Apply<TStat>(choiceContext, base.Owner, cardSource, (decimal)Sign * amount);
            }
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side == base.Owner.Side)
        {
            Flash();
            await PowerCmd.Remove(this);
            await CommonActions.Apply<TStat>(choiceContext, base.Owner, null, -Sign * base.Amount);
        }
    }
}

