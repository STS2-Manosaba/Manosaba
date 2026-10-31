using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Manosaba.Characters.Common.Powers;

namespace Manosaba.Characters.Common.Monsters;

public static class JailerCmd
{
    public static async Task<Creature?> Heal(
        PlayerChoiceContext choiceContext,
        Player summoner,
        decimal amount,
        decimal? reviveMaxHp = null,
        bool allowOverhealToIncreaseMaxHp = false)
    {
        _ = choiceContext;

        if (summoner.Creature?.CombatState == null || summoner.PlayerCombatState == null || amount <= 0m)
        {
            return null;
        }

        Creature? jailer = GetExistingJailer(summoner);
        if (jailer == null)
        {
            if (reviveMaxHp is null || reviveMaxHp <= 0m)
            {
                return null;
            }

            MonsterModel monster = ModelDb.GetById<MonsterModel>(ModelDb.Monster<Jailer>().Id).ToMutable();
            jailer = summoner.Creature.CombatState.CreateCreature(monster, CombatSide.Player, null);
            jailer.SetMaxHpInternal(Math.Max(1, (int)decimal.Round(reviveMaxHp.Value, MidpointRounding.AwayFromZero)));
            jailer.SetCurrentHpInternal(0);
            await CommonActions.Apply<PersistentPetCorpsePower>(choiceContext, jailer, null, 1m, silent: true);
        }

        bool isReviving = !jailer.IsAlive;
        if (isReviving)
        {
            summoner.PlayerCombatState.AddPetInternal(jailer);
        }

        EnsureJailerNode(summoner, jailer);

        decimal overheal = jailer.CurrentHp + amount - jailer.MaxHp;
        if (allowOverhealToIncreaseMaxHp && overheal > 0m)
        {
            await CreatureCmd.GainMaxHp(jailer, overheal);
        }

        await CreatureCmd.Heal(jailer, amount, isReviving);
        return jailer;
    }

    public static Creature? Summon(Player summoner, decimal amount)
    {
        if (summoner.Creature?.CombatState == null || summoner.PlayerCombatState == null)
        {
            return null;
        }

        int hpAmount = Math.Max(0, (int)decimal.Round(amount, MidpointRounding.AwayFromZero));
        if (hpAmount <= 0)
        {
            return summoner.PlayerCombatState.GetPet<Jailer>();
        }

        Creature? jailer = GetExistingJailer(summoner);
        if (jailer is { IsAlive: true })
        {
            return jailer;
        }

        bool didSummon = false;

        if (jailer == null)
        {
            MonsterModel monster = ModelDb.GetById<MonsterModel>(ModelDb.Monster<Jailer>().Id).ToMutable();
            jailer = summoner.Creature.CombatState.CreateCreature(monster, CombatSide.Player, null);
            _ = CommonActions.Apply<PersistentPetCorpsePower>(new ThrowingPlayerChoiceContext(), jailer, null, 1m);
            didSummon = true;
        }

        jailer.SetMaxHpInternal(hpAmount);
        jailer.SetCurrentHpInternal(hpAmount);
        summoner.PlayerCombatState.AddPetInternal(jailer);
        didSummon = true;

        EnsureJailerNode(summoner, jailer);

        if (didSummon && CombatManager.Instance.IsInProgress)
        {
            SfxCmd.Play("event:/sfx/characters/necrobinder/necrobinder_summon");
            CombatManager.Instance.History.Summoned(summoner.Creature.CombatState, hpAmount, summoner);
        }

        return jailer;
    }

    private static Creature? GetExistingJailer(Player summoner)
    {
        return summoner.PlayerCombatState?.GetPet<Jailer>()
               ?? summoner.Creature.CombatState?.Allies.FirstOrDefault(c => c.Monster is Jailer && c.PetOwner == summoner);
    }

    private static void EnsureJailerNode(Player summoner, Creature jailer)
    {
        NCreature? jailerNode = NCombatRoom.Instance?.GetCreatureNode(jailer);
        if (jailerNode == null)
        {
            NCombatRoom.Instance?.AddCreature(jailer);
            jailerNode = NCombatRoom.Instance?.GetCreatureNode(jailer);
        }

        jailerNode?.TrackBlockStatus(summoner.Creature);
        if (jailerNode != null)
        {
            PushJailerToBack(jailerNode);
            jailerNode.ToggleIsInteractable(true);
            jailerNode.OstyScaleToSize(jailer.MaxHp, 0.75f);
        }
    }

    private static void PushJailerToBack(NCreature jailerNode)
    {
        Node? parent = jailerNode.GetParent();
        if (parent!= null)
        {
            parent.MoveChild(jailerNode, 0);
        }
    }
}
