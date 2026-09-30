using BaseLib.Utils;
using manosaba.Characters.NikaidoHiro;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using NikaidoHiroCharacter = manosaba.Characters.NikaidoHiro.NikaidoHiro;

namespace manosaba.Characters.SakurabaEma.Relics;

[Pool(typeof(SakurabaEmaRelicPool))]
public sealed class LegIronsSakurabaEma : LevelingPathCustomRelicModel
{
    private const decimal BaseMajokaMultiplierPercent = 20m;
    private const decimal MajokaMultiplierPercentPerLevel = 30m;
    private const decimal EvidenceThreshold = 10m;
    private bool _hirosPenAddedToDeck;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override int MaxRelicLevel => 5;

    [SavedProperty]
    public bool HirosPenAddedToDeck
    {
        get => _hirosPenAddedToDeck;
        set
        {
            AssertMutable();
            _hirosPenAddedToDeck = value;
        }
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature == null)
            return;

        decimal multiplierPercent = BaseMajokaMultiplierPercent + MajokaMultiplierPercentPerLevel * Math.Max(1, RelicLevel);
        await CommonActions.Apply<LegIronsMajokaPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, null, multiplierPercent);

        if (!HasNikaidoHiroTeammate())
            await TryAddHirosPenToDeck();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature == null || player != Owner)
            return;

        if (Owner.Creature.GetPowerAmount<EvidencePower>() >= EvidenceThreshold)
            await CommonActions.Apply<ArgumentPower>(choiceContext, Owner.Creature, null, 1m);
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        _ = choiceContext;
        _ = deathAnimLength;

        if (wasRemovalPrevented || Owner?.Creature?.CombatState == null)
            return;

        if (creature.CombatState != Owner.Creature.CombatState || creature.Side != Owner.Creature.Side)
            return;

        if (creature.Player == null || !IsNikaidoHiro(creature.Player))
            return;

        await TryAddHirosPenToDeck();
    }

    private bool HasNikaidoHiroTeammate()
    {
        if (Owner?.Creature?.CombatState == null)
            return false;

        return Owner.Creature.CombatState.GetTeammatesOf(Owner.Creature)
            .Where(creature => creature.Player != null)
            .Select(creature => creature.Player!)
            .Any(IsNikaidoHiro);
    }

    public async Task TryAddHirosPenToDeck()
    {
        if (HirosPenAddedToDeck || Owner?.Deck == null || Owner.RunState == null)
            return;

        HirosPenAddedToDeck = true;

        if (Owner.Deck.Cards.Any(card => card is HirosPen))
            return;

        CardModel canonicalCard = ModelDb.Card<HirosPen>();
        CardModel card = Owner.RunState.CreateCard(canonicalCard, Owner);
        CardPileAddResult result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.GridLayout);
    }

    private static bool IsNikaidoHiro(Player player) =>
        player.Character.Id.Entry.EndsWith(NikaidoHiroCharacter.CharacterId, StringComparison.OrdinalIgnoreCase);
}
