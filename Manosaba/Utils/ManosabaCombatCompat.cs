using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Utils;

/// <summary>Helpers for accessing combat state through the current unified game API.</summary>
public static class ManosabaCombatCompat
{
    public static ICombatState? From(CardModel card) => card.CombatState;

    public static ICombatState? From(Creature? creature) => creature?.CombatState;

    public static ICombatState? RawFrom(CardModel card) => card.CombatState;

    public static ICombatState? RawFrom(Creature? creature) => creature?.CombatState;

    /// <summary>Hand preview: card combat state is often null; fall back to owner's creature.</summary>
    public static ICombatState? FromCardOrOwner(CardModel card) =>
        From(card) ?? From(card.Owner?.Creature);

    public static object? RawFromCardOrOwner(CardModel card) =>
        RawFrom(card) ?? RawFrom(card.Owner?.Creature);

    public static bool HasCombatState(Creature creature) => creature.CombatState != null;

    public static EncounterModel? GetEncounter(Creature creature) =>
        creature.CombatState?.Encounter;

    public static AttackCommand TargetingAllOpponentsCompat(this AttackCommand cmd, ICombatState? combatState)
    {
        if (combatState != null)
        {
            cmd.TargetingAllOpponents(combatState);
        }

        return cmd;
    }

    public static AttackCommand TargetingAllOpponentsCompat(this AttackCommand cmd, CardModel card)
    {
        return cmd.TargetingAllOpponentsCompat(RawFrom(card) ?? RawFrom(card.Owner?.Creature));
    }
}
