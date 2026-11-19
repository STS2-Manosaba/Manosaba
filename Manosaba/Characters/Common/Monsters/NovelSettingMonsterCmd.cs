using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using Manosaba.Characters.JogasakiNoah.Powers;

namespace Manosaba.Characters.Common.Monsters;

public static class NovelSettingMonsterCmd
{
    public static async Task<SummonResult> Summon<TMonster>(PlayerChoiceContext choiceContext, Player summoner, AbstractModel? source, decimal summonAmount = 1m)
        where TMonster : MonsterModel
    {
        if (summoner.Creature?.CombatState is not { } combatState || summoner.PlayerCombatState == null)
        {
            return new SummonResult(null, 0m);
        }

        decimal amount = Hook.ModifySummonAmount(combatState, summoner, summonAmount, source);
        Creature? summoned = combatState.Allies.FirstOrDefault((Creature c) => c.Monster is TMonster && c.PetOwner == summoner && c.IsAlive);
        if (amount == 0m)
        {
            return new SummonResult(summoned, 0m);
        }

        if (CombatManager.Instance.IsInProgress)
        {
            SfxCmd.Play("event:/sfx/characters/necrobinder/necrobinder_summon");
        }

        bool isReviving = false;
        if (summoned != null && summoned.IsAlive)
        {
            // If a same-type summon is already alive, create another copy instead of increasing max HP.
            summoned = await PlayerCmd.AddPet<TMonster>(summoner);
            if (summoned == null)
            {
                return new SummonResult(null, 0m);
            }

            NCreature? summonedNode = NCombatRoom.Instance?.GetCreatureNode(summoned);
            summonedNode?.TrackBlockStatus(summoner.Creature);
        }
        else
        {
            summoned = combatState.Allies.FirstOrDefault((Creature c) => c.Monster is TMonster && c.PetOwner == summoner);
            isReviving = summoned != null;
            if (isReviving)
            {
                Creature revived = summoned!;
                if (revived.IsAlive)
                {
                    throw new InvalidOperationException("We shouldn't make it here if summoned monster is still alive!");
                }

                summoner.PlayerCombatState.AddPetInternal(revived);
                summoned = revived;
            }
            else
            {
                summoned = await PlayerCmd.AddPet<TMonster>(summoner);
                if (summoned == null)
                {
                    return new SummonResult(null, 0m);
                }

                NCreature? summonedNode = NCombatRoom.Instance?.GetCreatureNode(summoned);
                summonedNode?.TrackBlockStatus(summoner.Creature);
            }
        }

        if (summoned == null)
        {
            return new SummonResult(null, 0m);
        }

        await CreatureCmd.SetMaxHp(summoned, amount);
        await CreatureCmd.Heal(summoned, amount, isReviving);

        ICombatState? summonedCombatState = summoned.CombatState;
        if (summonedCombatState == null)
        {
            return new SummonResult(summoned, amount);
        }

        Creature perspective = summoned.PetOwner?.Creature ?? summoner.Creature;
        List<Creature> petTargets = summonedCombatState.GetOpponentsOf(perspective)
            .Where(c => c?.IsAlive == true)
            .Select(c => c!)
            .ToList();
        if (petTargets.Count > 0 && PetEnemyAiPower.TryPrepareForNextTurnWithTargets(summoned, petTargets, rollNewMove: true))
        {
            PetEnemyAiPower.TryAdvanceToValidMove(summoned, petTargets);
        }
        await CommonActions.Apply<PetEnemyAiPower>(choiceContext, summoned, null, 1m, silent: true);

        if (TestMode.IsOff)
        {
            NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(summoned);
            if (nCreature != null)
            {
                Manosaba.Compatibility.GameApiCompat.ScaleOstyToSize(nCreature, summoned.MaxHp, 0.75f);
                Manosaba.Compatibility.GameApiCompat.SetCreatureInteractable(nCreature, true);
            }
        }

        CombatManager.Instance.History.Summoned(combatState, (int)amount, summoner);
        await Hook.AfterSummon(combatState, choiceContext, summoner, amount);
        return new SummonResult(summoned, amount);
    }
}
