using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class StranglePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    public override bool AllowNegative => false;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Applier == null || cardPlay.Card.Owner?.Creature != Applier || !Owner.IsAlive)
            return;

        Flash();
        if (Amount > 0m)
            await CreatureCmd.Damage(context, Owner, Amount, DamageProps.nonCardHpLoss, null, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = choiceContext;
        _ = creatures;

        if (Applier == null || side != Applier.Side)
            return;

        await PowerCmd.Remove(this);
    }
}
