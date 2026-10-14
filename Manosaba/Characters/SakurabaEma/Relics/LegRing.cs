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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
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
    private int _corpsePhotoFlags;
    private HashSet<Creature> _pendingKills = [];

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

    // Each bit records a character's first death across the entire run, including after reload.
    [SavedProperty]
    public int CorpsePhotoFlags
    {
        get => _corpsePhotoFlags;
        set { AssertMutable(); _corpsePhotoFlags = value & 31; }
    }

    public override async Task BeforeCombatStart()
    {
        _pendingKills = [];
        if (Owner.Creature == null)
            return;

        var choiceContext = new ThrowingPlayerChoiceContext();
        if (Owner.Creature.GetPower<LegRingMajokaPower>() is not { } multiplier)
            await CommonActions.Apply<LegRingMajokaPower>(choiceContext, Owner.Creature, null, MajokaMultiplierPercent + YouDidWell.Level(Owner) * 10);
        else
            await PowerCmd.ModifyAmount(choiceContext, multiplier, MajokaMultiplierPercent + YouDidWell.Level(Owner) * 10 - multiplier.Amount, null, null);

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

        if (creature.CombatState != Owner.Creature.CombatState)
            return;

        if (creature.Side != Owner.Creature.Side)
        {
            if (_pendingKills.Remove(creature) && !creature.HasPower<MinionPower>()) await YouDidWell.Gain(Owner);
            return;
        }

        if (creature.Player == null || creature == Owner.Creature)
            return;

        await YouDidWell.Gain(Owner);
        await TryAddCorpsePhotoToDeck(creature.Player);
        if (IsNikaidoHiro(creature.Player)) await TryAddHirosPenToDeck();
    }

    private async Task TryAddCorpsePhotoToDeck(Player fallen)
    {
        (string Character, int Flag, Type Card)[] photos =
        [
            ("jogasaki_noah", 1, typeof(NoahCorpsePhoto)),
            ("saeki_miria", 2, typeof(MiriaCorpsePhoto)),
            ("tono_hanna", 4, typeof(HannaCorpsePhoto)),
            ("shito_alisa", 8, typeof(AlisaCorpsePhoto)),
            ("kurobe_nanoka", 16, typeof(NanokaCorpsePhoto)),
        ];
        foreach (var photo in photos)
        {
            if (!fallen.Character.Id.Entry.EndsWith(photo.Character, StringComparison.OrdinalIgnoreCase)) continue;
            if ((CorpsePhotoFlags & photo.Flag) != 0) return;
            CorpsePhotoFlags |= photo.Flag;
            CardModel canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(photo.Card));
            if (Owner.Deck.Cards.Any(card => card.Id == canonical.Id)) return;
            CardModel card = Owner.RunState.CreateCard(canonical, Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 1.2f, CardPreviewStyle.GridLayout);
            return;
        }
    }

    internal void RecordExecution(Creature target) => _pendingKills.Add(target);

    public override Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner.Creature && target.Side != Owner.Creature.Side && result.WasTargetKilled && !target.HasPower<MinionPower>())
            _pendingKills.Add(target);
        return Task.CompletedTask;
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
