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

public sealed class DarkSightPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = amount;
        _ = cardSource;

        if (target != Owner || dealer == null)
        {
            return 1m;
        }

        // Invulnerable to attacks during the enemy's turn.
        if (dealer.Side != Owner.Side && props.HasFlag(ValueProp.Move))
        {
            return 0m;
        }

        if(dealer.Side == Owner.Side)
        {
            //Take no damage from allies.
            return 0m;
        }

        return 1m;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = choiceContext;

        // Remove after the enemy finishes their turn.
        if (side != Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

