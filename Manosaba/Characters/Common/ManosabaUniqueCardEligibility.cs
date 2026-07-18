using System.Collections.Generic;
using System.Linq;
using Manosaba.Characters.Common.Overrides;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Characters.Common;

/// <summary>Rules for <see cref="ManosabaKeywords.Unique"/>: per-player deck only; offer filtering for rewards/shop.</summary>
public static class ManosabaUniqueCardEligibility
{
    public static bool IsUniqueTemplate(CardModel card)
        => card.CanonicalKeywords.Contains(ManosabaKeywords.Unique);

    public static bool PlayerDeckContainsCardId(Player player, ModelId cardId)
    {
        if (player.Deck?.Cards == null)
            return false;

        return player.Deck.Cards.Any(c => c.Id == cardId);
    }

    /// <summary>True if this template should not be offered to <paramref name="player"/> (already in their deck).</summary>
    public static bool IsBlockedForPlayerOffer(Player player, CardModel templateOrInstance)
    {
        if (!IsUniqueTemplate(templateOrInstance))
            return false;

        return PlayerDeckContainsCardId(player, templateOrInstance.Id);
    }

    /// <summary>
    /// Merchant rolls cards from this pool before <see cref="Hook.ModifyMerchantCardCreationResults"/>; vanilla never copies the hook list back into <c>MerchantCardEntry.CreationResult</c>, so filtering must happen here.
    /// </summary>
    public static IEnumerable<CardModel> FilterMerchantCardPool(Player player, IEnumerable<CardModel>? options)
    {
        if (options == null)
            return options!;

        return options.Where(c => !IsBlockedForPlayerOffer(player, c));
    }

    public static CardCreationOptions FilterCardCreationOptions(Player player, CardCreationOptions options)
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
            return options;

        Func<CardModel, bool>? previous = options.CardPoolFilter;

        bool Combined(CardModel c)
        {
            if (previous != null && !previous(c))
                return false;
            if (IsBlockedForPlayerOffer(player, c))
                return false;
            return true;
        }

        if (options.CustomCardPool != null)
        {
            CardModel[] filtered = options.GetPossibleCards(player).Where(Combined).ToArray();
            if (filtered.Length == 0)
                return options;
            return options.WithCustomPool(filtered, options.RarityOdds);
        }

        // WithCardPools clears _cardPools before AddRange; CardPools returns that same list — snapshot first.
        List<CardPoolModel> poolSnapshot = options.CardPools.ToList();
        return options.WithCardPools(poolSnapshot, Combined);
    }

    public static void FilterCardCreationResults(Player player, List<CardCreationResult> results)
    {
        for (int i = results.Count - 1; i >= 0; i--)
        {
            CardModel card = results[i].Card;
            if (IsBlockedForPlayerOffer(player, card))
                results.RemoveAt(i);
        }
    }

    /// <summary>
    /// 事後補足：唯一卡被 <see cref="FilterCardCreationResults"/> 移除後，補回等量、非重複、且非「已擁有唯一」的卡。
    /// 只在帶 <see cref="CardCreationFlags.NoCardPoolModifications"/> 的獎勵才需要（此時抽前過濾 <see cref="FilterCardCreationOptions"/>
    /// 會被跳過，唯一卡才可能被抽進來，例如「藥水的未來」）；一般獎勵的唯一卡本來就不會被抽到，不必補足。
    /// 補不足時維持較少張數，絕不丟例外。使用與原生 <c>CardFactory.CreateForReward</c> 相同的 RNG
    /// （<c>options.RngOverride ?? player.PlayerRng.Rewards</c>）與相同的多人卡池限制，維持多人同步。
    /// </summary>
    public static void RefillCardRewardAfterUniqueRemoval(Player player, List<CardCreationResult> results, CardCreationOptions options, int removedCount)
    {
        if (removedCount <= 0)
            return;

        if (!options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
            return;

        HashSet<ModelId> presentIds = results
            .Select(r => r.Card.CanonicalInstance.Id)
            .ToHashSet();

        List<CardModel> candidates = FilterForPlayerCount(player, options.GetPossibleCards(player))
            .Where(c => c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
            .Where(c => !presentIds.Contains(c.CanonicalInstance.Id))
            .Where(c => !IsBlockedForPlayerOffer(player, c))
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        Rng rng = options.RngOverride ?? player.PlayerRng.Rewards;
        for (int i = 0; i < removedCount && candidates.Count > 0; i++)
        {
            CardModel? canonical = rng.NextItem(candidates);
            if (canonical == null)
                break;

            candidates.RemoveAll(c => c.Id == canonical.Id);
            results.Add(new CardCreationResult(player.RunState.CreateCard(canonical, player)));
        }
    }

    private static IEnumerable<CardModel> FilterForPlayerCount(Player player, IEnumerable<CardModel> options)
    {
        return player.RunState.Players.Count > 1
            ? options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly)
            : options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
    }
}
