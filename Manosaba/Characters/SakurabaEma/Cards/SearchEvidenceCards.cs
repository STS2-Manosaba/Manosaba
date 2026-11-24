using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.JogasakiNoa.Orbs;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Characters.SawatariCoco.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

internal static class SearchEvidencePool
{
    // Explicit pool: hidden corpse photos and Hiro's Pen have their own acquisition rules.
    internal static readonly Type[] Types =
    [
        typeof(BrokenCrossbowEvidence), typeof(NoahsButterflyDrawing), typeof(WitchIslandPrisonBlueprint),
        typeof(FruitBasketEvidence), typeof(WhiteSprayCanEvidence), typeof(BloodstainedHairRibbon),
        typeof(CocoStreamArchive), typeof(DismantledBroom), typeof(TortureShowInvitation),
        typeof(BrokenPunishmentRoomPadlock), typeof(ChippedKnife), typeof(PunishmentRoomKey),
        typeof(ScorchedMarks), typeof(StoreroomBulletMarks), typeof(WardenStatue),
        typeof(RedPaintedAlisaDoll), typeof(GuardRecording), typeof(RedPaintOnStoneFloor),
        typeof(BrokenElectricChair), typeof(ShatteredGlass), typeof(MeltedBottleCap),
        typeof(ApprenticeWitchFiles), typeof(Note169231), typeof(BloodstainedSafe),
        typeof(LargerDoor), typeof(ControlPanelEvidence), typeof(VentEvidence),
    ];
}

public abstract class SearchEvidenceCard : EvidenceCard
{
    protected SearchEvidenceCard(CardType type = CardType.Skill) : base(type, TargetType.Self) { }
    protected IEnumerable<Player> MatchingPlayers(params string[] ids) => CombatState?.Players
        .Where(p => p.Creature.IsAlive && ids.Any(id => p.Character.Id.Entry.EndsWith(id, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(p => p.NetId).ToArray() ?? [];
    protected IReadOnlyList<Creature> Enemies => CombatState?.GetOpponentsOf(Owner.Creature).Where(e => e.IsHittable).ToArray() ?? [];
    protected Creature? RandomEnemy()
    {
        IReadOnlyList<Creature> enemies = Enemies;
        return enemies.Count == 0 ? null : Owner.RunState.Rng.CombatTargets.NextItem(enemies);
    }
    protected async Task Apply<T>(PlayerChoiceContext context, Creature? target, decimal amount) where T : PowerModel
    {
        if (target != null) await CommonActions.Apply<T>(context, target, this, amount);
    }
    protected async Task BuffMatches<T>(PlayerChoiceContext context, string id, decimal amount) where T : PowerModel
    {
        foreach (Player player in MatchingPlayers(id)) await Apply<T>(context, player.Creature, amount);
    }
    protected async Task Hit(PlayerChoiceContext context, Creature? target, CardPlay play)
    {
        if (target is { IsAlive: true }) await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(target).Execute(context);
    }
    protected async Task Generate(CardModel canonical)
    {
        if (CombatState != null) CardCmd.PreviewCardPileAdd(await EmaCardGeneration.Add(CombatState.CreateCard(canonical, Owner), PileType.Hand, Owner));
    }
    protected async Task GenerateAndPlay(PlayerChoiceContext context, CardModel canonical, bool upgraded = false)
    {
        if (CombatState == null) return;
        CardModel card = CombatState.CreateCard(canonical, Owner);
        if (upgraded && card.IsUpgradable) CardCmd.Upgrade(card);
        CardPileAddResult result = await WitchEncyclopediaState.AddForImmediatePlay(card, Owner);
        CardCmd.PreviewCardPileAdd(result);
        if (result.success && !card.HasBeenRemovedFromState) await CardCmd.AutoPlay(context, card, null);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class FruitBasketEvidence : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<RegenPower>(2), new PowerVar<GuiltPower>(1)];
    public override string PortraitPath => "res://Manosaba/images/cards/fruit_basket_evidence.png";
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<RegenPower>(), HoverTipFactory.FromPower<GuiltPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<RegenPower>(context, Owner.Creature, DynamicVars["RegenPower"].BaseValue);
        await BuffMatches<GuiltPower>(context, "hasumi_leia", DynamicVars["GuiltPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RegenPower"].UpgradeValueBy(1);
        DynamicVars["GuiltPower"].UpgradeValueBy(1);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class WhiteSprayCanEvidence : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];
    public override string PortraitPath => "res://Manosaba/images/cards/white_spray_can_evidence.png";
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromOrb<WhitePaintOrb>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        foreach (Player player in MatchingPlayers("jogasaki_noah"))
        {
            OrbModel orb = ModelDb.Orb<WhitePaintOrb>().ToMutable();
            orb.Owner = player;
            IEnumerable<Creature> targets = await orb.Evoke(context);
            if (player.Creature.CombatState is { } combat) await Hook.AfterOrbEvoked(context, combat, orb, targets);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class BloodstainedHairRibbon : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move), new PowerVar<MajokaPower>(5)];
    public BloodstainedHairRibbon() : base(CardType.Attack) { }
    public override string PortraitPath => "res://Manosaba/images/cards/bloodstained_hair_ribbon.png";
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Hit(context, RandomEnemy(), play);
        await BuffMatches<MajokaPower>(context, "kurobe_nanoka", DynamicVars["MajokaPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["MajokaPower"].UpgradeValueBy(3);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class CocoStreamArchive : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2, ValueProp.Move), new PowerVar<LiveStreamModePower>(1)];
    public CocoStreamArchive() : base(CardType.Attack) { }
    public override string PortraitPath => "res://Manosaba/images/cards/coco_stream_archive.png";
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<LiveStreamModePower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (CombatState is { } combat && Enemies.Count > 0)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).TargetingAllOpponents(combat).Execute(context);
        await BuffMatches<LiveStreamModePower>(context, "sawatari_coco", DynamicVars["LiveStreamModePower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
        DynamicVars["LiveStreamModePower"].UpgradeValueBy(1);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class DismantledBroom : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(1)];
    public override string PortraitPath => "res://Manosaba/images/cards/dismantled_broom.png";
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<DexterityPower>(context, Owner.Creature, DynamicVars["DexterityPower"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["DexterityPower"].UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class TortureShowInvitation : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Move), new DynamicVar("AllyBlock", 3)];
    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<SusPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await Apply<SusPower>(context, Owner.Creature, 1);
        foreach (Player player in MatchingPlayers("tono_hanna", "sawatari_coco"))
            await CreatureCmd.GainBlock(player.Creature, new BlockVar(DynamicVars["AllyBlock"].BaseValue, ValueProp.Move), play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["AllyBlock"].UpgradeValueBy(2);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class BrokenPunishmentRoomPadlock : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<StrengthPower>(context, Owner.Creature, DynamicVars["StrengthPower"].BaseValue);
        await BuffMatches<StrengthPower>(context, "tachibana_sherry", DynamicVars["StrengthPower"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthPower"].UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class ChippedKnife : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BleedPower>(10), new PowerVar<GuiltPower>(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<BleedPower>(), HoverTipFactory.FromPower<GuiltPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<BleedPower>(context, RandomEnemy(), DynamicVars["BleedPower"].BaseValue);
        await BuffMatches<GuiltPower>(context, "natsume_anan", DynamicVars["GuiltPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BleedPower"].UpgradeValueBy(5);
        DynamicVars["GuiltPower"].UpgradeValueBy(1);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class PunishmentRoomKey : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<GuiltPower>(2)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<GuiltPower>(), HoverTipFactory.FromPower<SusPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<GuiltPower>(context, Owner.Creature, DynamicVars["GuiltPower"].BaseValue);
        await BuffMatches<SusPower>(context, "kurobe_nanoka", 1);
    }

    protected override void OnUpgrade() => DynamicVars["GuiltPower"].UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class ScorchedMarks : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BurnPower>(3)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<BurnPower>(), HoverTipFactory.FromPower<SusPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<BurnPower>(context, RandomEnemy(), DynamicVars["BurnPower"].BaseValue);
        await BuffMatches<SusPower>(context, "shito_alisa", 1);
    }

    protected override void OnUpgrade() => DynamicVars["BurnPower"].UpgradeValueBy(2);
}

[Pool(typeof(TokenCardPool))]
public sealed class StoreroomBulletMarks : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4, ValueProp.Move), new BlockVar(3, ValueProp.Move)];
    public StoreroomBulletMarks() : base(CardType.Attack) { }
    public override bool GainsBlock => true;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Hit(context, RandomEnemy(), play);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Block.UpgradeValueBy(2);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class WardenStatue : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WeakPower>(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<WeakPower>()];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<WeakPower>(context, RandomEnemy(), DynamicVars["WeakPower"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["WeakPower"].UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class RedPaintedAlisaDoll : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BurnPower>(2)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<BurnPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (Creature enemy in Enemies) await Apply<BurnPower>(context, enemy, DynamicVars["BurnPower"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["BurnPower"].UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class GuardRecording : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (PileType.Hand.GetPile(Owner).IsEmpty) return;
        int count = Math.Min(DynamicVars.Cards.IntValue, PileType.Hand.GetPile(Owner).Cards.Count);
        var selected = await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, count), null, this);
        foreach (var card in selected.ToArray()) await CardCmd.Exhaust(context, card);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class RedPaintOnStoneFloor : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VulnerablePower>(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<VulnerablePower>(context, RandomEnemy(), DynamicVars["VulnerablePower"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["VulnerablePower"].UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class BrokenElectricChair : SearchEvidenceCard
{
    public BrokenElectricChair() : base(CardType.Attack) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (CombatState is not { } combat || Enemies.Count == 0) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .TargetingRandomOpponents(combat).WithHitCount(5).Execute(context);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class ShatteredGlass : SearchEvidenceCard
{
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromCard<SleepingPill>()];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => GenerateAndPlay(context, ModelDb.Card<SleepingPill>(), IsUpgraded);
}

[Pool(typeof(TokenCardPool))]
public sealed class MeltedBottleCap : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromCard<ScorchingMedicineBottle>(), HoverTipFactory.FromPower<FreeScorchingBottlePower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        await Apply<FreeScorchingBottlePower>(context, Owner.Creature, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class ApprenticeWitchFiles : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<ArgumentPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        await Apply<ArgumentPower>(context, Owner.Creature, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class Note169231 : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];
    public override string PortraitPath => "res://Manosaba/images/cards/torture_show_invitation.png";
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromCard<Vote>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++) await Generate(ModelDb.Card<Vote>());
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class BloodstainedSafe : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Gold", 1), new PowerVar<GuiltPower>(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
        await BuffMatches<GuiltPower>(context, "hikami_meruru", DynamicVars["GuiltPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Gold"].UpgradeValueBy(1);
        DynamicVars["GuiltPower"].UpgradeValueBy(1);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class LargerDoor : SearchEvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PlatingPower>(2)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<PlatingPower>()];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<PlatingPower>(context, Owner.Creature, DynamicVars["PlatingPower"].BaseValue);

    protected override void OnUpgrade() => DynamicVars["PlatingPower"].UpgradeValueBy(1);
}

[Pool(typeof(TokenCardPool))]
public sealed class ControlPanelEvidence : SearchEvidenceCard
{
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromKeyword(ManosabaKeywords.Execution)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] options = ModelDb.AllCards.Where(card => card.CanBeGeneratedInCombat && ExecutionCardHelper.IsExecutionCard(card) && (!IsUpgraded || card.MaxUpgradeLevel > 0)).OrderBy(card => card.Id.Entry, StringComparer.Ordinal).ToArray();
        if (options.Length == 0) return;
        CardModel? canonical = Owner.RunState.Rng.CombatCardGeneration.NextItem(options);
        if (canonical != null) await GenerateAndPlay(context, canonical, IsUpgraded);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class VentEvidence : SearchEvidenceCard
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] cards = PileType.Discard.GetPile(Owner).Cards.ToArray();
        if (cards.Length == 0) return;
        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(context, cards, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected != null) await CardPileCmd.Add(selected, PileType.Hand);
    }
}
