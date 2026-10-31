using Manosaba.Characters.Common.Monsters;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class WardenAttackPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> creatures)
    {
        Player? player = Owner.Player;
        if (side != CombatSide.Player || player?.PlayerCombatState == null || Owner.CombatState == null)
            return;

        Creature? jailer = player.PlayerCombatState.GetPet<Jailer>()
            ?? Owner.CombatState.Allies.FirstOrDefault(c => c.Monster is Jailer && c.PetOwner == player);
        if (jailer is not { IsAlive: true })
            return;

        IReadOnlyList<Creature> enemies = Owner.CombatState.GetOpponentsOf(Owner).Where(c => c.IsAlive && c.IsHittable).ToList();
        if (enemies.Count > 0)
            await CreatureCmd.Damage(context, enemies, Amount, ValueProp.Unpowered, Owner, null);
    }
}
