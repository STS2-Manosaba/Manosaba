using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace Manosaba.Characters.HoshoMago.Powers;

public sealed class TheFoolPower : PathCustomPowerModel
{
    private bool _armed;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await PowerCmd.Remove(this);
    }

    public override async Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
    {
        Player? ownerPlayer = Owner?.Player;
        Creature? ownerCreature = ownerPlayer?.Creature;
        ICombatState? combatState = ownerCreature?.CombatState;
        if (_armed || ownerPlayer == null || ownerCreature == null || combatState == null || player != ownerPlayer)
        {
            return;
        }

        await PowerCmd.Remove(this);
        bool flag;
        using (Manosaba.Compatibility.GameApiCompat.PushSelector(player, new VakuuCardSelector()))
        {
            int cardsPlayed;
            for (cardsPlayed = 0; cardsPlayed < 13; cardsPlayed++)
            {
                if (CombatManager.Instance.IsOverOrEnding)
                {
                    break;
                }

                if (CombatManager.Instance.IsPlayerReadyToEndTurn(player))
                {
                    break;
                }

                CardPile pile = PileType.Hand.GetPile(ownerPlayer);
                CardModel? card = pile.Cards.FirstOrDefault((CardModel c) => c.CanPlay());
                if (card == null)
                {
                    break;
                }

                Creature? target = GetTarget(card, ownerPlayer, ownerCreature, combatState);
                if (RequiresTarget(card.TargetType) && target == null)
                {
                    break;
                }

                await card.SpendResources();
                await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
            }

            flag = cardsPlayed >= 13;
            if (cardsPlayed == 0)
            {
                return;
            }
        }

        LocString line = (flag ? new LocString("relics", "WHISPERING_EARRING.warning") : new LocString("relics", "WHISPERING_EARRING.approval"));
        TalkCmd.Play(line, ownerCreature, VfxColor.Orange);

        _armed = true;

    }

    private static bool RequiresTarget(TargetType targetType)
    {
        return targetType is TargetType.AnyEnemy or TargetType.AnyAlly or TargetType.AnyPlayer;
    }

    private Creature? GetTarget(CardModel card, Player ownerPlayer, Creature ownerCreature, ICombatState combatState)
    {
        Rng combatTargets = ownerPlayer.RunState.Rng.CombatTargets;
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => PickRandomAlly(combatTargets, combatState, ownerCreature),
            TargetType.AnyPlayer => ownerCreature,
            _ => null,
        };
    }

    private static Creature? PickRandomAlly(Rng combatTargets, ICombatState combatState, Creature ownerCreature)
    {
        List<Creature> allies = combatState.Allies
            .Where((Creature c) => c != null && c.IsAlive && c.IsPlayer && c != ownerCreature)
            .ToList();

        return allies.Count == 0 ? null : combatTargets.NextItem(allies);
    }
}
