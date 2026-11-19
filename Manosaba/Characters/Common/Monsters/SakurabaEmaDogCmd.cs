using BaseLib.Utils;
using Manosaba.Characters.NikaidoHiro.Powers;
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

namespace Manosaba.Characters.Common.Monsters
{
    public static class SakurabaEmaDogCmd
    {
        public static async Task<SummonResult> Summon(PlayerChoiceContext choiceContext, Player summoner, decimal amount, AbstractModel? source)
        {
            if (summoner.Creature?.CombatState is not { } combatState || summoner.PlayerCombatState == null)
            {
                return new SummonResult(null, 0m);
            }

            amount = Hook.ModifySummonAmount(combatState, summoner, amount, source);
            Creature? ema = combatState.Allies.FirstOrDefault((Creature c) => c.Monster is SakurabaEmaDog && c.PetOwner == summoner);
            if (amount == 0m)
            {
                return new SummonResult(ema, 0m);
            }

            if (CombatManager.Instance.IsInProgress)
            {
                SfxCmd.Play("event:/sfx/characters/necrobinder/necrobinder_summon");
            }
            if (ema != null && ema.IsAlive)
            {
                await CreatureCmd.GainMaxHp(ema, amount);
            }
            else
            {
                bool isReviving = ema != null;
                if (isReviving)
                {
                    Creature revivedEma = ema!;
                    if (revivedEma.IsAlive)
                    {
                        throw new InvalidOperationException("We shouldn't make it here if Ema is still alive!");
                    }

                    summoner.PlayerCombatState.AddPetInternal(revivedEma);
                    ema = revivedEma;
                }
                else
                {
                    ema = await PlayerCmd.AddPet<SakurabaEmaDog>(summoner);
                    if (ema == null)
                    {
                        return new SummonResult(null, 0m);
                    }

                    NCreature? emaNode = NCombatRoom.Instance?.GetCreatureNode(ema);
                    await CommonActions.Apply<IkiteyoHirochanPower>(choiceContext, ema, null, 1m, silent: true);
                    emaNode?.TrackBlockStatus(summoner.Creature);
                }

                await CreatureCmd.SetMaxHp(ema, amount);
                await CreatureCmd.Heal(ema, amount, isReviving);
            }

            if (TestMode.IsOff)
            {
                NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(ema);
                if (nCreature != null)
                {
                    Manosaba.Compatibility.GameApiCompat.ScaleOstyToSize(nCreature, ema.MaxHp, 0.75f);
                    Manosaba.Compatibility.GameApiCompat.SetCreatureInteractable(nCreature, true);
                }
            }

            CombatManager.Instance.History.Summoned(combatState, (int)amount, summoner);
            await Hook.AfterSummon(combatState, choiceContext, summoner, amount);
            return new SummonResult(ema, amount);
        }

    }
}
