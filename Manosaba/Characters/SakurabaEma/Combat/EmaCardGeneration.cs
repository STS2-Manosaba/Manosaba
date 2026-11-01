using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Combat;

// A card effect or AfterCardPlayed hook may finish combat before generating its
// follow-up card. The native single-card command indexes an empty result then.
internal static class EmaCardGeneration
{
    public static async Task<CardPileAddResult> Add(CardModel card, PileType pile, Player creator,
        CardPilePosition position = CardPilePosition.Bottom)
    {
        if (!CombatManager.Instance.IsOverOrEnding && card.Owner.Creature.IsAlive)
        {
            var results = await CardPileCmd.AddGeneratedCardsToCombat([card], pile, creator, position);
            if (results.Count > 0)
                return results[0];
        }
        return new CardPileAddResult { cardAdded = card, success = false };
    }
}
