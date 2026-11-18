using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.NatsumeAnan.Powers;

public sealed class SicklyPower : PathCustomPowerModel
{
    private bool _isDuplicatingStatusCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        _ = source;

        if (_isDuplicatingStatusCard
            || oldPileType != PileType.None
            || card.Owner != Owner.Player
            || card.Type != CardType.Status
            || card.Pile?.Type is not { } pileType
            || !pileType.IsCombatPile()
            || card.CombatState is not { } combatState)
        {
            return;
        }

        CardModel duplicate = combatState.CreateCard(card.CanonicalInstance, card.Owner);
        duplicate.AddKeyword(CardKeyword.Exhaust);
        duplicate.AddKeyword(CardKeyword.Ethereal);

        try
        {
            _isDuplicatingStatusCard = true;
            Flash();
            await CardPileCmd.AddGeneratedCardToCombat(duplicate, pileType, Owner.Player);
        }
        finally
        {
            _isDuplicatingStatusCard = false;
        }
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        _ = fromHandDraw;

        if (card.Owner != Owner.Player || card.Type != CardType.Status || Owner.Player == null)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, 1m, Owner.Player, fromHandDraw: true);
        await CreatureCmd.Heal(Owner, 1m);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = amount;
        _ = props;
        _ = dealer;

        return target == Owner && cardSource?.Type == CardType.Status ? 0.5m : 1m;
    }
}
