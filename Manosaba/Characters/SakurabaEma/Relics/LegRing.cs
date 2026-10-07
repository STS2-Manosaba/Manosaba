using BaseLib.Utils;
using BaseLib.Utils.Attributes;
using manosaba.Characters.NikaidoHiro;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using NikaidoHiroCharacter = manosaba.Characters.NikaidoHiro.NikaidoHiro;

namespace manosaba.Characters.SakurabaEma.Relics;

// Keep the existing save ID and localization keys after renaming the class.
[CustomID("MANOSABA-LEG_IRONS_SAKURABA_EMA")]
[Pool(typeof(SakurabaEmaRelicPool))]
public sealed class LegRing : LevelingPathCustomRelicModel
{
    public override string PackedIconPath => "leg_ring.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "leg_ring.png".RelicImagePath();
    protected override string BigIconPath => "leg_ring.png".RelicImagePath();

    private const decimal MajokaMultiplierPercent = 50m;
    private bool _hirosPenAddedToDeck;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override int MaxRelicLevel => 5;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [..base.ExtraHoverTips, HoverTipFactory.FromPower<SearchTimePower>(), HoverTipFactory.FromCard<RawTellOwk>()];

    internal static int GetSearchCardRequirement(int relicLevel)
        => Math.Max(2, 10 - 2 * Math.Max(1, relicLevel));

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

        var choiceContext = new ThrowingPlayerChoiceContext();
        await CommonActions.Apply<LegRingMajokaPower>(choiceContext, Owner.Creature, null, MajokaMultiplierPercent);

        if (RelicLevel >= 5)
        {
            await SearchTimePower.GiveRawTellOwk(Owner);
        }
        else
        {
            // Native application skips zero amounts, so attach once and then initialize the counter at zero.
            SearchTimePower? search = await CommonActions.Apply<SearchTimePower>(choiceContext, Owner.Creature, null, 1m);
            search?.StartSearch(GetSearchCardRequirement(RelicLevel));
        }

        if (!HasNikaidoHiroTeammate())
            await TryAddHirosPenToDeck();
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
