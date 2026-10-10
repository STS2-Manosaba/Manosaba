using System.Runtime.CompilerServices;
using BaseLib.Utils;
using BaseLib.Patches.Content;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Manosaba.Characters.Common.Overrides;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Combat;

public sealed class WitchEncyclopediaState
{
    [CustomEnum("witch_encyclopedia")]
    public static PileType PileType;

    // Keyed by the combat state, never the persistent player: each battle starts empty.
    private static readonly ConditionalWeakTable<PlayerCombatState, WitchEncyclopediaState> States = new();
    public CardPile Pile { get; } = new(PileType);
    public bool HasReceivedEvidence { get; private set; }

    public static WitchEncyclopediaState Get(PlayerCombatState combat) => States.GetValue(combat, _ => new());
    public static WitchEncyclopediaState? Get(Player player) => player.PlayerCombatState is { } combat ? Get(combat) : null;
    public void MarkReceivedEvidence() => HasReceivedEvidence = true;

    public static HoverTip HoverTip => new(
        new LocString("static_hover_tips", "MANOSABA-WITCH_ENCYCLOPEDIA.title"),
        new LocString("static_hover_tips", "MANOSABA-WITCH_ENCYCLOPEDIA.description"));

    public static bool ShouldStore(CardModel card) =>
        card.Keywords.Contains(ManosabaKeywords.Evidence) || card.Keywords.Contains(ManosabaKeywords.Testimony);

    public static int CardCount(Player? player) => player == null ? 0 : Get(player)?.Pile.Cards.Count ?? 0;

    // Shared by Present Evidence and Rebuttal. Finish the selected card's effect
    // and return it before the caller calculates its follow-up effect.
    public static async Task Present(PlayerChoiceContext choiceContext, Player owner, LocString prompt)
    {
        var state = Get(owner);
        if (state == null || state.Pile.IsEmpty)
            return;
        var prefs = new CardSelectorPrefs(prompt, 1);
        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, state.Pile.Cards.ToList(), owner, prefs)).FirstOrDefault();
        if (selected == null)
            return;
        CardCmd.Preview(selected);
        try
        {
            await CardCmd.AutoPlay(choiceContext, selected, null, skipCardPileVisuals: true);
        }
        finally
        {
            if (!selected.HasBeenRemovedFromState && !state.Pile.Cards.Contains(selected))
                await CardPileCmd.Add(selected, state.Pile, skipVisuals: true);
        }
    }
    public static async Task GenerateRandomEvidence(Player player)
    {
        if (player.Creature.CombatState is not { } combat)
            return;
        CardModel[] options = [ModelDb.Card<BrokenCrossbowEvidence>(), ModelDb.Card<NoahsButterflyDrawing>(), ModelDb.Card<WitchIslandPrisonBlueprint>()];
        CardModel canonical = options[player.RunState.Rng.CombatCardSelection.NextInt(options.Length)];
        CardModel card = combat.CreateCard(canonical, player);
        // Use the same entry point as other card generation. The pile patch captures it
        // before hand capacity is checked, including when the hand already has ten cards.
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(card, MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand, player);
        CardCmd.PreviewCardPileAdd(result);
    }
}
