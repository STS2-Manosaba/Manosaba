using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class SpecialMagicPower : PathCustomPowerModel
{
    private const int DieCardsPerTurn = 5;
    private const int EveryoneShouldJustDieCardsPerTurn = 3;
    private const int MaxAutoPlayedCardsPerTurn = 13;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;

        if (player != Owner.Player || Owner.CombatState == null)
            return;

        Flash();

        List<CardModel> cards = [];
        for (int i = 0; i < DieCardsPerTurn; i++)
            cards.Add(Owner.CombatState.CreateCard<DieSakurabaEma>(player));

        for (int i = 0; i < EveryoneShouldJustDieCardsPerTurn; i++)
            cards.Add(Owner.CombatState.CreateCard<EveryoneShouldJustDie>(player));

        IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
        CardCmd.PreviewCardPileAdd(results);
    }

    public override async Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
    {
        Player? ownerPlayer = Owner.Player;
        Creature? ownerCreature = ownerPlayer?.Creature;
        ICombatState? combatState = ownerCreature?.CombatState;
        if (ownerPlayer == null || ownerCreature == null || combatState == null || player != ownerPlayer)
            return;

        for (int cardsPlayed = 0; cardsPlayed < MaxAutoPlayedCardsPerTurn; cardsPlayed++)
        {
            if (CombatManager.Instance.IsOverOrEnding || CombatManager.Instance.IsPlayerReadyToEndTurn(player))
                break;

            CardModel? card = PileType.Hand.GetPile(ownerPlayer).Cards.FirstOrDefault(c => c.CanPlay());
            if (card == null)
                break;

            Creature? target = GetTarget(card, ownerPlayer, ownerCreature, combatState);
            if (RequiresTarget(card.TargetType) && target == null)
                break;

            await card.SpendResources();
            await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
        }
    }

    private static bool RequiresTarget(TargetType targetType) =>
        targetType is TargetType.AnyEnemy or TargetType.AnyAlly or TargetType.AnyPlayer;

    private static Creature? GetTarget(CardModel card, Player ownerPlayer, Creature ownerCreature, ICombatState combatState) =>
        card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => PickRandomAlly(ownerPlayer, combatState, ownerCreature),
            TargetType.AnyPlayer => ownerCreature,
            _ => null,
        };

    private static Creature? PickRandomAlly(Player ownerPlayer, ICombatState combatState, Creature ownerCreature)
    {
        List<Creature> allies = combatState.Allies
            .Where(c => c != null && c.IsAlive && c.IsPlayer && c != ownerCreature)
            .ToList();

        return allies.Count == 0 ? null : ownerPlayer.RunState.Rng.CombatTargets.NextItem(allies);
    }
}
