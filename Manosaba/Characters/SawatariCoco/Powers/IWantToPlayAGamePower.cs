using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

namespace Manosaba.Characters.SawatariCoco.Powers;

/// <summary>I want to play a game：回合結束時獲得魔女化；魔女化達 150 時每回合開始時被接管。</summary>
public sealed class IWantToPlayAGamePower : PathCustomPowerModel
{
    private const decimal MajokaPerTurnEnd = 30m;
    private const decimal TakeoverMajokaThreshold = 150m;
    private const int MaxAutoPlayCards = 13;

    private bool _pendingTakeover;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side || !creatures.Contains(Owner))
        {
            return;
        }

        await CommonActions.Apply<MajokaPower>(choiceContext, Owner, null, MajokaPerTurnEnd);

        if (Owner.GetPowerAmount<MajokaPower>() >= TakeoverMajokaThreshold)
        {
            _pendingTakeover = true;
        }
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || !_pendingTakeover)
        {
            return;
        }

        _pendingTakeover = false;

        if (Owner.GetPowerAmount<MajokaPower>() < TakeoverMajokaThreshold)
        {
            return;
        }

        await TriggerTakeoverAsync(choiceContext);
    }

    private async Task TriggerTakeoverAsync(PlayerChoiceContext choiceContext)
    {
        Player? ownerPlayer = Owner.Player;
        Creature? ownerCreature = ownerPlayer?.Creature;
        ICombatState? combatState = ownerCreature?.CombatState;
        if (ownerPlayer == null || ownerCreature == null || combatState == null)
        {
            return;
        }

        using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
        {
            int cardsPlayed = 0;
            for (; cardsPlayed < MaxAutoPlayCards; cardsPlayed++)
            {
                if (CombatManager.Instance.IsOverOrEnding)
                {
                    break;
                }

                if (CombatManager.Instance.IsPlayerReadyToEndTurn(ownerPlayer))
                {
                    break;
                }

                CardPile hand = PileType.Hand.GetPile(ownerPlayer);
                CardModel? card = hand.Cards.FirstOrDefault(c => c.CanPlay());
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
        }
    }

    private static bool RequiresTarget(TargetType targetType)
        => targetType is TargetType.AnyEnemy or TargetType.AnyAlly or TargetType.AnyPlayer;

    private static Creature? GetTarget(CardModel card, Player ownerPlayer, Creature ownerCreature, ICombatState combatState)
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
            .Where(c => c != null && c.IsAlive && c.IsPlayer && c != ownerCreature)
            .ToList();

        return allies.Count == 0 ? null : combatTargets.NextItem(allies);
    }
}
