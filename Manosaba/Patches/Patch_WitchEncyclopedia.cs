using Godot;
using HarmonyLib;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Visuals;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Patches;

// Vanilla checksum snapshots enumerate the five original piles explicitly rather
// than AllPiles. Include the encyclopedia so evidence divergence is detectable.
[HarmonyPatch(typeof(NetFullCombatState), nameof(NetFullCombatState.FromRun))]
public static class Patch_NetFullCombatState_WitchEncyclopedia
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, NetFullCombatState __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return;
        foreach (var playerSnapshot in __result.Players)
        {
            var player = runState.GetPlayer(playerSnapshot.playerId);
            if (player != null && WitchEncyclopediaState.Get(player) is { } state)
                playerSnapshot.piles.Add(NetFullCombatState.CombatPileState.From(state.Pile));
        }
    }
}

[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.AllPiles), MethodType.Getter)]
public static class Patch_PlayerCombatState_WitchEncyclopedia
{
    [HarmonyPostfix]
    private static void Postfix(PlayerCombatState __instance, ref IReadOnlyList<CardPile> __result) =>
        __result = [..__result, WitchEncyclopediaState.Get(__instance).Pile];
}

[HarmonyPatch(typeof(CardPile), nameof(CardPile.Get))]
public static class Patch_CardPile_Get_WitchEncyclopedia
{
    [HarmonyPrefix]
    private static bool Prefix(PileType type, Player player, ref CardPile? __result)
    {
        if (type != WitchEncyclopediaState.PileType)
            return true;
        __result = WitchEncyclopediaState.Get(player)?.Pile;
        return false;
    }
}

[HarmonyPatch(typeof(PileTypeExtensions), nameof(PileTypeExtensions.IsCombatPile))]
public static class Patch_PileType_IsCombatPile_WitchEncyclopedia
{
    [HarmonyPostfix]
    private static void Postfix(PileType pileType, ref bool __result)
    {
        if (pileType == WitchEncyclopediaState.PileType)
            __result = true;
    }
}

[HarmonyPatch(typeof(PileTypeExtensions), nameof(PileTypeExtensions.GetTargetPosition))]
public static class Patch_PileType_TargetPosition_WitchEncyclopedia
{
    [HarmonyPrefix]
    private static bool Prefix(PileType pileType, ref Vector2 __result)
    {
        if (pileType != WitchEncyclopediaState.PileType)
            return true;
        var ui = NCombatRoom.Instance?.Ui;
        var button = ui?.GetNodeOrNull<NWitchEncyclopediaButton>("ManosabaWitchEncyclopedia");
        __result = button != null ? button.GlobalPosition + button.Size * 0.5f : Vector2.Zero;
        return false;
    }
}

// Encyclopedia cards are stored models, not card nodes on the combat table.
// Vanilla FindOnTable throws for unknown piles, including callers outside our
// capture hook (for example enqueue-play effects before autoplay moves a card).
[HarmonyPatch(typeof(NCard), nameof(NCard.FindOnTable))]
public static class Patch_NCard_FindOnTable_WitchEncyclopedia
{
    [HarmonyPrefix]
    private static bool Prefix(CardModel card, PileType? overridePile, ref NCard? __result)
    {
        if ((card.Pile?.Type ?? overridePile) != WitchEncyclopediaState.PileType)
            return true;
        __result = null;
        return false;
    }
}
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
    [typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])]
public static class Patch_CardPileCmd_Add_WitchEncyclopedia
{
    [HarmonyPrefix]
    private static bool Prefix(IEnumerable<CardModel> cards, CardPile newPile, CardPilePosition position,
        AbstractModel? clonedBy, ref bool skipVisuals, ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        if (newPile.Type == PileType.Hand && cards.Any(WitchEncyclopediaState.ShouldStore))
        {
            __result = CaptureHandEvidence(cards.ToList(), newPile, position, clonedBy, skipVisuals);
            return false;
        }
        if (newPile.Type == WitchEncyclopediaState.PileType)
        {
            // Core pile movement and hooks still run. Custom piles use our own button,
            // so bypass the vanilla animation branches that assume five fixed piles.
            skipVisuals = true;
            var incoming = cards.ToList();
            if (incoming.Count > 1 || incoming.Any(card => newPile.Cards.Any(existing => existing.Id == card.Id)))
            {
                __result = AddUnique(incoming, newPile, position, clonedBy);
                return false;
            }
        }
        return true;
    }

    [HarmonyPostfix]
    private static void Postfix(CardPile newPile, ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        if (newPile.Type == WitchEncyclopediaState.PileType)
            __result = FinishCapture(__result, newPile);
    }

    private static async Task<IReadOnlyList<CardPileAddResult>> CaptureHandEvidence(List<CardModel> cards,
        CardPile hand, CardPilePosition position, AbstractModel? source, bool skipVisuals)
    {
        var results = new List<CardPileAddResult>();
        foreach (var card in cards)
        {
            var target = WitchEncyclopediaState.ShouldStore(card) ? WitchEncyclopediaState.Get(card.Owner)?.Pile : hand;
            results.Add(target == null ? new CardPileAddResult { cardAdded = card, success = false } :
                await CardPileCmd.Add(card, target, position, source, skipVisuals));
        }
        return results;
    }

    private static async Task<IReadOnlyList<CardPileAddResult>> AddUnique(List<CardModel> cards,
        CardPile pile, CardPilePosition position, AbstractModel? source)
    {
        var results = new List<CardPileAddResult>();
        foreach (var card in cards)
        {
            if (pile.Cards.Any(existing => existing.Id == card.Id && existing != card))
            {
                var oldPile = card.Pile;
                card.RemoveFromCurrentPile();
                RemoveTableNode(card, oldPile);
                card.RemoveFromState();
                results.Add(new CardPileAddResult { cardAdded = card, oldPile = oldPile, success = false });
            }
            else if (pile.Cards.Contains(card))
                results.Add(new CardPileAddResult { cardAdded = card, oldPile = pile, success = true });
            else
                results.Add(await CardPileCmd.Add(card, pile, position, source, skipVisuals: true));
        }
        return results;
    }

    private static async Task<IReadOnlyList<CardPileAddResult>> FinishCapture(Task<IReadOnlyList<CardPileAddResult>> original, CardPile pile)
    {
        var results = await original;
        foreach (var result in results.Where(result => result.success))
        {
            WitchEncyclopediaState.Get(result.cardAdded.Owner)?.MarkReceivedEvidence();
            RemoveTableNode(result.cardAdded, result.oldPile);
            result.oldPile?.InvokeContentsChanged();
            result.oldPile?.InvokeCardRemoveFinished();
        }
        pile.InvokeCardAddFinished();
        return results;
    }

    private static void RemoveTableNode(CardModel card, CardPile? oldPile)
    {
        if (TestMode.IsOn)
            return;
        // The model has already moved into the encyclopedia. FindOnTable reads
        // its current pile even when an override is supplied, so look up only
        // the OLD hand/play containers directly. Freshly generated evidence
        // has no old pile and no table node to clean up.
        var ui = NCombatRoom.Instance?.Ui;
        if (ui == null)
            return;
        var node = oldPile?.Type switch
        {
            PileType.Hand => ui.Hand.GetCard(card) ?? ui.PlayQueue.GetCardNode(card) ?? ui.GetCardFromPlayContainer(card),
            PileType.Play => ui.GetCardFromPlayContainer(card),
            _ => null,
        };
        if (node == null)
            return;
        if (oldPile?.Type == PileType.Hand)
            ui.Hand.Remove(card);
        node.GetParent()?.RemoveChild(node);
        node.QueueFree();
    }
}
