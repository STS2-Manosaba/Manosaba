using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace manosaba.Characters.SakurabaEma.Cards;

internal static class EmaCardTargeting
{
    public static List<Player> FindTeammatesByCharacterIds(
        ICombatState combatState,
        Creature owner,
        params string[] characterIds)
    {
        HashSet<string> ids = characterIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return combatState.GetTeammatesOf(owner)
            .Where(creature => creature.IsAlive && creature.Player != null)
            .Select(creature => creature.Player!)
            .Where(player => ids.Any(id => player.Character.Id.Entry.EndsWith(id, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(player => player.NetId)
            .ToList();
    }
}
