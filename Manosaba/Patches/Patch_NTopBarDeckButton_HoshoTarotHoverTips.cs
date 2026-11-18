using Godot;
using HarmonyLib;
using manosaba.Characters.HoshoMago;
using manosaba.Characters.HoshoMago.Relics;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.HoshoMago.Cards;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using System.Collections.Generic;
using System.Linq;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(NTopBarDeckButton), "OnFocus")]
public static class Patch_NTopBarDeckButton_HoshoTarotHoverTips
{
    private static readonly AccessTools.FieldRef<NTopBarDeckButton, Player> _playerRef =
        AccessTools.FieldRefAccess<NTopBarDeckButton, Player>("_player");

    [HarmonyPostfix]
    private static void Postfix(NTopBarDeckButton __instance)
    {
        Player player = _playerRef(__instance);
        if (player?.Character?.CardPool is not HoshoMagoCardPool)
        {
            return;
        }

        NHoverTipSet.Remove(__instance);

        List<CardModel> tarotPool = GetEligibleTarotCards(player)
            .OrderBy(card => card.Id)
            .ToList();
        HashSet<ModelId> ownedTarotIds = player.Deck.Cards
            .Where(card => card.Tags.Contains(ManosabaCardTags.Tarot) && card is not TheWorld)
            .Select(card => card.Id)
            .ToHashSet();

        string tarotCollectionText = BuildTarotCollectionText(tarotPool, ownedTarotIds);
        LocString deckTitle = new LocString("static_hover_tips", "DECK.title");
        string hotkey = NInputManager.Instance?.GetCurrentHotkey(MegaInput.viewDeckAndTabLeft).ToString() ?? string.Empty;
        deckTitle.Add("Hotkey", hotkey);
        IEnumerable<IHoverTip> tips =
        [
            new HoverTip(
                deckTitle,
                new LocString("static_hover_tips", "DECK.description")
            ),
            new HoverTip(
                ModelDb.Relic<TarotDeck>().Title,
                tarotCollectionText
            )
        ];

        NHoverTipSet? tipSet = NHoverTipSet.CreateAndShow(__instance, tips);
        if (tipSet == null)
        {
            return;
        }

        tipSet.GlobalPosition = __instance.GlobalPosition + new Vector2(__instance.Size.X - tipSet.Size.X, __instance.Size.Y + 20f);
    }

    private static List<CardModel> GetEligibleTarotCards(Player player)
    {
        return ModelDb.CardPool<HoshoMagoCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Tags.Contains(ManosabaCardTags.Tarot) && card is not TheWorld)
            .ToList();
    }

    private static string BuildTarotCollectionText(List<CardModel> tarotPool, HashSet<ModelId> ownedTarotIds)
    {
        int total = tarotPool.Count;
        int owned = tarotPool.Count(card => ownedTarotIds.Contains(card.Id));
        string missingHeader = new LocString("relics", "MANOSABA-TAROT_DECK.missingHeader").GetFormattedText();
        string completedText = new LocString("relics", "MANOSABA-TAROT_DECK.completed").GetFormattedText();

        List<string> missingLines = tarotPool
            .Where(card => !ownedTarotIds.Contains(card.Id))
            .Select(card => card.Title)
            .ToList();

        if (missingLines.Count == 0)
        {
            return $"[gold]{owned}[/gold] / [gold]{total}[/gold]\n[color=gray]{completedText}[/color]";
        }

        return $"[gold]{owned}[/gold] / [gold]{total}[/gold]\n{missingHeader}\n" + string.Join("\n", missingLines);
    }
}
