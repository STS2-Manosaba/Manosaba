using System.Linq;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.NikaidoHiro.Powers
{
    public class EmaDogAttackPower : PathCustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;
        public override bool ShouldPlayVfx => false;

        public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            ICombatState? combatState = CombatState;
            if (side == CombatSide.Player && Owner.IsAlive && combatState != null)
            {
                List<Creature> targets = combatState.GetOpponentsOf(Owner)
                    .Where(c => c.IsHittable)
                    .ToList();
                if (targets.Count == 0)
                {
                    return;
                }

                Creature? target = combatState.RunState.Rng.CombatTargets.NextItem(targets);
                if (target == null)
                {
                    return;
                }

                await CreatureCmd.Damage(choiceContext, target, Owner.CurrentHp, ValueProp.Move, Owner, null, null);
            }
        }

        public override bool ShouldPowerBeRemovedAfterOwnerDeath()
        {
            return false;
        }
    }
}
