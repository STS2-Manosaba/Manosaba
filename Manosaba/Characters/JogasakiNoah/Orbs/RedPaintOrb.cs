using BaseLib.Utils;
using Godot;
using Manosaba.Characters.JogasakiNoah;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Manosaba.Compatibility;

namespace Manosaba.Characters.JogasakiNoa.Orbs;

[Pool(typeof(JogasakiNoahOrbPool))]
public sealed class RedPaintOrb : ManosabaOrbModel
{
    public override Color DarkenedColor => new("8A3333");

    public override decimal PassiveVal => ModifyPaintOrbValue(3m);

    public override decimal EvokeVal => ModifyPaintOrbValue(5m);

    public override Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        return Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target != null)
        {
            throw new InvalidOperationException("Red paint orb cannot target creatures.");
        }

        if (CombatState == null || Owner?.Creature == null)
        {
            return;
        }

        List<Creature> candidates = CombatState.HittableEnemies
            .Where(e => e.IsHittable)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        Creature? chosen = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
        if (chosen == null)
        {
            return;
        }

        ActivatePassive();
        PlayPassiveSfx();
        await CreatureCmd.Damage(choiceContext, chosen, PassiveVal, ValueProp.Unpowered, Owner.Creature);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        if (CombatState == null || Owner?.Creature == null)
        {
            return [];
        }

        List<Creature> enemies = CombatState.HittableEnemies
            .Where(e => e.IsHittable)
            .ToList();
        if (enemies.Count == 0)
        {
            return [];
        }

        await GameApiCompat.Damage(playerChoiceContext, enemies, EvokeVal, ValueProp.Unpowered, Owner.Creature, null, null);
        return enemies;
    }
}
