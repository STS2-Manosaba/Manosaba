using BaseLib.Utils;
using Godot;
using Manosaba.Characters.JogasakiNoah;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.JogasakiNoa.Orbs;

[Pool(typeof(JogasakiNoahOrbPool))]
public sealed class GreenPaintOrb : ManosabaOrbModel
{
    public override Color DarkenedColor => new("3D8A4B");

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
            throw new InvalidOperationException("Green paint orb cannot target creatures.");
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
        await CommonActions.Apply<PoisonPower>(choiceContext, chosen, null, PassiveVal);
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

        foreach (Creature enemy in enemies)
        {
            await CommonActions.Apply<PoisonPower>(playerChoiceContext, enemy, null, EvokeVal);
        }
        return enemies;
    }
}
