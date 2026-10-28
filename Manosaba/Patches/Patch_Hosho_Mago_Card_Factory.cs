using System.Collections.Generic;
using HarmonyLib;
using manosaba.Characters.HoshoMago;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.HoshoMago.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using System.Reflection;

namespace Manosaba.Patches;

[HarmonyPatch]
public static class Patch_Hosho_Mago_Card_Factory
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodInfo? transform = AccessTools.Method(typeof(CardFactory), nameof(CardFactory.CreateRandomCardForTransform), [typeof(CardModel), typeof(bool), typeof(Rng)]);
        if (transform != null)
        {
            yield return transform;
        }

        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(CardFactory)).Where(m => m.Name == nameof(CardFactory.GetDistinctForCombat)))
        {
            yield return method;
        }

        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(CardFactory)).Where(m => m.Name == nameof(CardFactory.GetForCombat)))
        {
            yield return method;
        }

        MethodInfo? lastingCandy = AccessTools.Method(typeof(LastingCandy), nameof(LastingCandy.TryModifyCardRewardOptions));
        if (lastingCandy != null)
        {
            yield return lastingCandy;
        }
    }

    [HarmonyPrefix]
    private static bool Prefix(MethodBase __originalMethod, object[] __args, ref object? __result, object? __instance = null)
    {
        if (__originalMethod.Name == nameof(CardFactory.CreateRandomCardForTransform))
        {
            return PrefixCreateRandomCardForTransform(__args, ref __result);
        }

        if (__originalMethod.Name == nameof(CardFactory.GetDistinctForCombat))
        {
            return PrefixGetDistinctForCombat(__args, ref __result);
        }

        if (__originalMethod.Name == nameof(CardFactory.GetForCombat))
        {
            return PrefixGetForCombat(__args, ref __result);
        }

        if (__originalMethod.Name == nameof(LastingCandy.TryModifyCardRewardOptions))
        {
            return PrefixLastingCandyTryModifyCardRewardOptions(__args, ref __result, __instance);
        }

        return true;
    }

    [HarmonyPostfix]
    private static void Postfix(MethodBase __originalMethod, object[] __args, ref object? __result, object? __instance = null)
    {
        if (__originalMethod.Name == nameof(LastingCandy.TryModifyCardRewardOptions))
        {
            PostfixLastingCandyTryModifyCardRewardOptions(__args, ref __result, __instance);
        }
    }

    private static bool PrefixCreateRandomCardForTransform(object[] args, ref object? result)
    {
        if (args.Length < 3 || args[0] is not CardModel original || args[1] is not bool isInCombat || args[2] is not Rng rng)
        {
            return true;
        }

        if (original.Pool is not HoshoMagoCardPool)
        {
            return true;
        }

        if (!ManosabaTransformHelper.TryCreateTransformResult(original, isInCombat, rng, out CardModel replacement))
        {
            return true;
        }

        result = replacement;
        return false;
    }

    private static bool PrefixGetDistinctForCombat(object[] args, ref object? result)
    {
        if (args.Length < 4)
        {
            return true;
        }

        Player? player = args.OfType<Player>().FirstOrDefault();
        Rng? rng = args.OfType<Rng>().FirstOrDefault();
        int? count = args.OfType<int>().Select(v => (int?)v).FirstOrDefault();
        object? poolArg = args.FirstOrDefault(a => a != null && a is not Player && a is not Rng && a is not int && a is not bool);
        if (player == null || rng == null || count == null || poolArg == null)
        {
            return true;
        }

        if (!TryGetPoolCards(poolArg, player, out List<CardModel>? poolCards) || poolCards == null)
        {
            return true;
        }

        if (!IsHoshoPool(poolArg, poolCards))
        {
            return true;
        }

        List<CardModel> available = BuildHoshoFallbackPool(poolCards, isInCombat: true, excludeCardId: null)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        if (available.Count == 0)
        {
            result = Enumerable.Empty<CardModel>();
            return false;
        }

        int targetCount = Math.Clamp(count.Value, 0, available.Count);
        List<CardModel> picked = [];
        for (int i = 0; i < targetCount; i++)
        {
            CardModel? canonical = rng.NextItem(available);
            if (canonical == null)
            {
                continue;
            }

            CardModel card = player.Creature?.CombatState != null
                ? player.Creature.CombatState.CreateCard(canonical, player)
                : player.RunState.CreateCard(canonical, player);
            picked.Add(card);
            available.Remove(canonical);
        }

        result = picked;
        return false;
    }

    private static bool PrefixGetForCombat(object[] args, ref object? result)
    {
        if (args.Length < 4)
        {
            return true;
        }

        Player? player = args.OfType<Player>().FirstOrDefault();
        Rng? rng = args.OfType<Rng>().FirstOrDefault();
        int? count = args.OfType<int>().Select(v => (int?)v).FirstOrDefault();
        object? poolArg = args.FirstOrDefault(a => a != null && a is not Player && a is not Rng && a is not int && a is not bool);
        if (player == null || rng == null || count == null || poolArg == null)
        {
            return true;
        }

        if (!TryGetPoolCards(poolArg, player, out List<CardModel>? poolCards) || poolCards == null)
        {
            return true;
        }

        if (!IsHoshoPool(poolArg, poolCards))
        {
            return true;
        }

        List<CardModel> available = BuildHoshoFallbackPool(poolCards, isInCombat: true, excludeCardId: null)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();
        if (available.Count == 0)
        {
            result = Enumerable.Empty<CardModel>();
            return false;
        }

        int targetCount = Math.Max(count.Value, 0);
        List<CardModel> picked = [];
        for (int i = 0; i < targetCount; i++)
        {
            CardModel? canonical = rng.NextItem(available);
            if (canonical == null)
            {
                continue;
            }

            CardModel card = player.Creature?.CombatState != null
                ? player.Creature.CombatState.CreateCard(canonical, player)
                : player.RunState.CreateCard(canonical, player);
            picked.Add(card);
        }

        result = picked;
        return false;
    }

    private static bool PrefixLastingCandyTryModifyCardRewardOptions(object[] args, ref object? result, object? instance)
    {
        if (instance is not LastingCandy lastingCandy || args.Length < 3 || args[0] is not Player player || args[1] is not List<CardCreationResult> options || args[2] is not CardCreationOptions creationOptions)
        {
            return true;
        }

        if (player.Character?.CardPool is not HoshoMagoCardPool)
        {
            return true;
        }

        if (lastingCandy.Owner != player)
        {
            return true;
        }

        if (creationOptions.Source != CardCreationSource.Encounter)
        {
            return true;
        }

        bool isInTriggeringCombat = lastingCandy.CombatsSeen > 0 && lastingCandy.CombatsSeen % 2 == 0;
        if (!isInTriggeringCombat)
        {
            return true;
        }

        List<CardModel> powerCards = BuildMagoLastingCandyPowerPool(player, creationOptions, options);
        if (powerCards.Count == 0)
        {
            result = false;
            return false;
        }

        bool singleRarity = powerCards.Select(c => c.Rarity).Distinct().Count() == 1;
        if (!singleRarity)
        {
            return true;
        }

        CardCreationOptions safeOptions = new CardCreationOptions(powerCards, CardCreationSource.Other, CardRarityOddsType.Uniform)
            .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);
        CardModel? cardModel = CardFactory.CreateForReward(lastingCandy.Owner, 1, safeOptions).FirstOrDefault()?.Card;
        if (cardModel != null)
        {
            CardCreationResult cardCreationResult = new(cardModel);
            cardCreationResult.ModifyCard(cardModel, lastingCandy);
            options.Add(cardCreationResult);
        }

        result = cardModel != null;
        return false;
    }

    private static void PostfixLastingCandyTryModifyCardRewardOptions(object[] args, ref object? result, object? instance)
    {
        if (instance is not LastingCandy lastingCandy || args.Length < 3 || args[0] is not Player player || args[1] is not List<CardCreationResult> options || result is not bool modified || !modified)
        {
            return;
        }

        if (player.Character?.CardPool is not HoshoMagoCardPool)
        {
            return;
        }

        CardCreationResult? candyResult = options.FirstOrDefault(o => o.ModifyingRelics.Contains(lastingCandy));
        if (candyResult == null)
        {
            return;
        }

        if (candyResult.Card.Tags.Contains(ManosabaCardTags.Tarot) && candyResult.Card is not TheWorld)
        {
            return;
        }

        List<CardModel> tarotPool = ModelDb.CardPool<HoshoMagoCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Tags.Contains(ManosabaCardTags.Tarot) && card is not TheWorld)
            .ToList();
        if (tarotPool.Count == 0)
        {
            return;
        }

        HashSet<ModelId> existingTarotIds = options
            .Select(o => o.Card)
            .Where(card => card.Tags.Contains(ManosabaCardTags.Tarot) && card is not TheWorld)
            .Select(card => card.Id)
            .ToHashSet();

        List<CardModel> available = tarotPool
            .Where(card => !existingTarotIds.Contains(card.Id))
            .Where(card => !ManosabaUniqueCardEligibility.IsBlockedForPlayerOffer(player, card))
            .ToList();
        if (available.Count == 0)
        {
            available = tarotPool
                .Where(card => !existingTarotIds.Contains(card.Id))
                .ToList();
        }

        if (available.Count == 0)
        {
            available = tarotPool;
        }

        CardModel? canonical = player.RunState.Rng.Niche.NextItem(available);
        if (canonical == null)
        {
            return;
        }

        CardModel replacement = player.RunState.CreateCard(canonical, player);
        candyResult.ModifyCard(replacement, lastingCandy);
    }

    private static List<CardModel> BuildMagoLastingCandyPowerPool(Player player, CardCreationOptions creationOptions, List<CardCreationResult> rewardOptions)
    {
        bool NotAlreadyInRewards(CardModel c) =>
            rewardOptions.TrueForAll(o => o.Card.CanonicalInstance.Id != c.Id);

        IEnumerable<CardModel> tier1Base() =>
            creationOptions.GetPossibleCards(player).Where(c => c.Type == CardType.Power && NotAlreadyInRewards(c));

        IEnumerable<CardModel> tier2Base() =>
            creationOptions.GetPossibleCards(player).Where(c => c.Type == CardType.Power);

        List<CardModel> withUnique(IEnumerable<CardModel> source) =>
            source.Where(c => !ManosabaUniqueCardEligibility.IsBlockedForPlayerOffer(player, c)).ToList();

        List<CardModel> list = withUnique(tier1Base());
        if (list.Count > 0)
        {
            return list;
        }

        list = tier1Base().ToList();
        if (list.Count > 0)
        {
            return list;
        }

        list = withUnique(tier2Base());
        if (list.Count > 0)
        {
            return list;
        }

        return tier2Base().ToList();
    }

    private static bool TryGetPoolCards(object poolArg, Player player, out List<CardModel>? cards)
    {
        if (poolArg is IEnumerable<CardModel> directEnumerable)
        {
            cards = directEnumerable.ToList();
            return true;
        }

        MethodInfo? getUnlockedCards = poolArg
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "GetUnlockedCards" && m.GetParameters().Length == 2);
        if (getUnlockedCards == null)
        {
            cards = null;
            return false;
        }

        object? unlocked = getUnlockedCards.Invoke(poolArg, [player.UnlockState, player.RunState.CardMultiplayerConstraint]);
        if (unlocked is IEnumerable<CardModel> unlockedEnumerable)
        {
            cards = unlockedEnumerable.ToList();
            return true;
        }

        cards = null;
        return false;
    }

    private static bool IsHoshoPool(object poolArg, List<CardModel> cards)
    {
        if (poolArg is HoshoMagoCardPool)
        {
            return true;
        }

        return cards.Count > 0 && cards.All(c => c.Pool is HoshoMagoCardPool);
    }

    private static List<CardModel> BuildHoshoFallbackPool(IEnumerable<CardModel> source, bool isInCombat, ModelId? excludeCardId = null)
    {
        IEnumerable<CardModel> query = source
            .Where(c => excludeCardId == null || c.Id != excludeCardId)
            .Where(c => c.Rarity != CardRarity.Token)
            // Tarot cards use Ancient rarity; allow them without admitting starter cards.
            .Where(c => c.Rarity != CardRarity.Basic)
            .Where(c => c is not TheWorld && c is not Echoes)
            .Where(c => !isInCombat || c.CanBeGeneratedInCombat);

        return query.ToList();
    }
}
