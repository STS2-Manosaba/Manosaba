using BaseLib.Utils;
using Manosaba.Extensions;
using Manosaba.Characters.Common.Powers;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Relics;

[Pool(typeof(SakurabaEmaRelicPool))]
public sealed class YouDidWell : PathCustomRelicModel
{
    private int _stacks = 1;
    private int _pureTurns, _kindTurns, _goodTurns;
    private bool _gainedSuspicion, _playedAttack, _tookDamage;
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Stacks;
    public override string PackedIconPath => "res://Manosaba/images/relics/you_did_well.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    [SavedProperty]
    public int Stacks
    {
        get => _stacks;
        set { AssertMutable(); _stacks = Math.Clamp(value, 1, 13); InvokeDisplayAmountChanged(); }
    }
    public static int Level(Player? player) => player?.Relics.OfType<YouDidWell>().FirstOrDefault()?.Stacks ?? 0;
    public static IHoverTip DerivationTip => new HoverTip(
        new LocString("relics", "MANOSABA-YOU_DID_WELL.derivationsTitle"),
        new LocString("relics", "MANOSABA-YOU_DID_WELL.derivations"));
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [DerivationTip, HoverTipFactory.FromPower<PureEmaPower>(), HoverTipFactory.FromPower<KindEmaPower>(), HoverTipFactory.FromPower<GoodChildEmaPower>()];

    // Leg Ring is the sole listener for acquisition and kill/death stack gains.
    public static async Task Gain(Player player, bool onlyIfMissing = false)
    {
        YouDidWell? relic = player.Relics.OfType<YouDidWell>().FirstOrDefault();
        if (relic == null)
            await RelicCmd.Obtain(ModelDb.Relic<YouDidWell>().ToMutable(), player);
        else if (!onlyIfMissing && relic.Stacks < 13)
        {
            relic.Stacks++;
            relic.Flash();
            await relic.SyncMultiplier();
            await relic.UnlockMagic();
        }
    }
    public override async Task AfterObtained()
    {
        await SyncMultiplier();
        await UnlockMagic();
    }
    internal async Task SyncMultiplier()
    {
        if (Owner.Creature.CombatState == null) return;
        int percent = (Owner.Relics.Any(r => r is LegRing) ? 50 : 100) + Stacks * 10;
        LegRingMajokaPower? power = Owner.Creature.GetPower<LegRingMajokaPower>();
        if (power == null)
            await CommonActions.Apply<LegRingMajokaPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, null, percent);
        else
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), power, percent - power.Amount, null, null);
    }
    public override async Task BeforeCombatStart()
    {
        _pureTurns = _kindTurns = _goodTurns = 0;
        _gainedSuspicion = _playedAttack = _tookDamage = false;
        await SyncMultiplier();
        await UnlockMagic();
    }
    private async Task UnlockMagic()
    {
        if (Stacks < 5) return;
        if (!Owner.Deck.Cards.Any(c => c is MagicToKillTheWitch))
        {
            CardModel magic = Owner.RunState.CreateCard(ModelDb.Card<MagicToKillTheWitch>(), Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(magic, PileType.Deck));
            if (Owner.Creature.CombatState is { } combat)
                await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<MagicToKillTheWitch>(Owner), PileType.Hand, Owner);
        }
        // Combat copies already exist at BeforeCombatStart; deck transforms do not update them.
        var activeCombat = Owner.Creature.CombatState;
        var combatTraumas = activeCombat == null ? [] :
            Owner.PlayerCombatState?.AllCards.Where(c => c is TraumaSakurabaEma).ToList() ?? [];
        foreach (CardModel trauma in combatTraumas)
        {
            if (trauma.Pile == null || !trauma.IsTransformable) continue;
            CardModel replacement = activeCombat!.CreateCard<TraumaTrueSakurabaEma>(Owner);
            if (trauma.IsUpgraded) CardCmd.Upgrade(replacement);
            var result = await CardCmd.Transform(trauma, replacement);
            if (result is { success: true, cardAdded: { } transformed } && transformed.Pile?.Type == PileType.Play)
                await CardPileCmd.Add(transformed, PileType.Discard);
        }
        foreach (CardModel trauma in Owner.Deck.Cards.Where(c => c is TraumaSakurabaEma).ToList())
        {
            bool upgraded = trauma.IsUpgraded;
            var result = await CardCmd.TransformTo<TraumaTrueSakurabaEma>(trauma);
            if (upgraded && result is { success: true, cardAdded: { } transformed }) CardCmd.Upgrade(transformed);
        }
    }
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner == Owner.Creature && power is SusPower && amount > 0) _gainedSuspicion = true;
        return Task.CompletedTask;
    }
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner && play.Card.Type == CardType.Attack) _playedAttack = true;
        return Task.CompletedTask;
    }
    public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner.Creature && result.UnblockedDamage > 0) _tookDamage = true;
        return Task.CompletedTask;
    }
    // Evaluate at the next turn start, including damage and suspicion gained during the enemy turn.
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combat)
    {
        if (side != Owner.Creature.Side || !participants.Contains(Owner.Creature)) return;
        if (combat.RoundNumber > 1)
        {
            _pureTurns = Stacks >= 7 && !_gainedSuspicion ? _pureTurns + 1 : 0;
            _kindTurns = Stacks >= 9 && !_playedAttack ? _kindTurns + 1 : 0;
            _goodTurns = Stacks >= 11 && !_tookDamage ? _goodTurns + 1 : 0;
            int turns = Stacks >= 13 ? 2 : 3;
            if (_pureTurns >= turns && !Owner.Creature.HasPower<PureEmaPower>())
                await CommonActions.Apply<PureEmaPower>(context, Owner.Creature, null, 1);
            if (_kindTurns >= turns && !Owner.Creature.HasPower<KindEmaPower>())
                await CommonActions.Apply<KindEmaPower>(context, Owner.Creature, null, 1);
            if (_goodTurns >= turns && !Owner.Creature.HasPower<GoodChildEmaPower>())
                await CommonActions.Apply<GoodChildEmaPower>(context, Owner.Creature, null, 1);
        }
        _gainedSuspicion = _playedAttack = _tookDamage = false;
    }
}
