using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.Common.Commands;

public static class ManosabaCombatCmd
{
    public static async Task ForceWinWithoutDeathOrEscape(ICombatState? combatState)
    {
        if (combatState == null) return;
        CombatManager? combatManager = CombatManager.Instance;
        if (combatManager == null || !combatManager.IsInProgress)
        {
            return;
        }

        await ReleaseStolenCards(combatState);

        IReadOnlyList<Creature> enemies = combatState.Enemies.ToList();
        foreach (Creature enemy in enemies)
        {
            enemy.RemoveAllPowersInternalExcept();
            await CreatureCmd.Kill(enemy);
        }
        await combatManager.CheckWinCondition();
    }

    private static async Task ReleaseStolenCards(ICombatState combatState)
    {
        foreach (Creature creature in combatState.Creatures.ToList())
        {
            foreach (SwipePower swipePower in creature.GetPowerInstances<SwipePower>().ToList())
            {
                await swipePower.BeforeDeath(creature);
                if (creature.Powers.Contains(swipePower))
                {
                    swipePower.RemoveInternal();
                }
            }
        }
    }
}
