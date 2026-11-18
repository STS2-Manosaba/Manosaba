using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.Entities.Creatures;
namespace Manosaba.Characters.Common.Powers;

public sealed class UnluckyPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side || Amount <= 0m || !Owner.IsAlive)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, Owner, null, null);
    }
}
