using manosaba.Characters.HoshoMago;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Characters.Common;

internal static class ManosabaKaleidoscopeHelper
{
    internal static bool TryHandleAfterObtained(Kaleidoscope kaleidoscope, ref Task __result)
    {
        if (kaleidoscope.Owner?.RunState == null)
        {
            return false;
        }

        __result = RunAfterObtained(kaleidoscope);
        return true;
    }

    private static async Task RunAfterObtained(Kaleidoscope kaleidoscope)
    {
        Player owner = kaleidoscope.Owner;
        List<Reward> rewards = [];
        CardCreationOptions rerollOptions = CardCreationOptions.ForNonCombatWithDefaultOdds(Array.Empty<CardPoolModel>());
        List<CardModel>? tarotPool = null;

        for (int i = 0; i < kaleidoscope.DynamicVars.Cards.IntValue; i++)
        {
            List<CardModel> cardsToOffer = [];
            IEnumerable<CardPoolModel> pools = owner.UnlockState.CharacterCardPools
                .Where(pool => pool != owner.Character.CardPool)
                .ToList()
                .StableShuffle(owner.RunState.Rng.Niche)
                .Take(3);

            foreach (CardPoolModel pool in pools)
            {
                cardsToOffer.Add(CreateRewardCardFromPool(owner, pool, ref tarotPool));
            }

            rewards.Add(new CardReward(cardsToOffer, CardCreationSource.Other, owner, rerollOptions));
        }

        await RewardsCmd.OfferCustom(owner, rewards);
    }

    private static CardModel CreateRewardCardFromPool(Player player, CardPoolModel pool, ref List<CardModel>? tarotPool)
    {
        if (ManosabaTarotRewardHelper.UsesTarotPoolInsteadOfStandardRewards(pool))
        {
            tarotPool ??= ManosabaTarotRewardHelper.GetOfferableTarotPool(player);
            CardModel? tarot = ManosabaTarotRewardHelper.PickRandomTarotOrLog(
                player,
                tarotPool,
                "Kaleidoscope");
            if (tarot != null)
            {
                return tarot;
            }
        }

        CardCreationOptions options = new CardCreationOptions(
            [pool],
            CardCreationSource.Other,
            CardRarityOddsType.RegularEncounter).WithFlags(CardCreationFlags.NoCardPoolModifications);
        return CardFactory.CreateForReward(player, 1, options).First().Card;
    }
}
