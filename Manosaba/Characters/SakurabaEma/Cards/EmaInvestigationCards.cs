using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class InformationSharing : EmaTrialCard
{
    protected override bool HasProgressEffect => true;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new TrialBlockVar(10m), new CardsVar(1)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [WitchEncyclopediaState.HoverTip, HoverTipFactory.FromCard<PresentEvidence>()];
    public InformationSharing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target?.Player is not { } player || CombatState is not { } combat) return;
        CardModel? evidence = await WitchEncyclopediaState.Present(context, Owner, SelectionScreenPrompt);
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(DynamicVars.Block.BaseValue, TrialMoveValueProp), play);
        if (player != Owner)
            await CreatureCmd.GainBlock(player.Creature, new BlockVar(DynamicVars.Block.BaseValue, TrialMoveValueProp), play);
        if (evidence != null)
        {
            // Each player keeps an independent encyclopedia copy, using normal hand-entry capture.
            CardModel copy = combat.CreateCard(evidence.CanonicalInstance, player);
            if (evidence.IsUpgraded) CardCmd.Upgrade(copy);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player));
        }
        if (ProgressReady(this))
        {
            foreach (var recipient in new[] { Owner, player }.Distinct().OrderBy(p => p.NetId))
                for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
                {
                    CardModel present = combat.CreateCard<PresentEvidence>(recipient);
                    CardCmd.ApplyKeyword(present, CardKeyword.Ethereal);
                    CardCmd.ApplyKeyword(present, CardKeyword.Exhaust);
                    CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(present, PileType.Hand, recipient));
                }
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class InformationNetwork : EmaTrialCard
{
    public override string PortraitPath => ModelDb.Card<SearchSakurabaEma>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<InformationNetworkPower>(1m), new DynamicVar("TriggerDamage", InformationNetworkPower.DamagePerStack)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromPower<InformationNetworkPower>(), WitchEncyclopediaState.HoverTip];
    public InformationNetwork() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
        => await CommonActions.Apply<InformationNetworkPower>(context, Owner.Creature, this, DynamicVars["InformationNetworkPower"].BaseValue);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class JointInvestigation : PathCustomCardModel
{
    // Deliberately works in solo: the card affects all players, including its owner.
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new DynamicVar("Evidence", 2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [WitchEncyclopediaState.HoverTip];
    public JointInvestigation() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        var players = combat.Players.Where(p => p.Creature.IsAlive).OrderBy(p => p.NetId).ToList();
        foreach (var player in players) await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, player);
        foreach (var player in players)
            for (int i = 0; i < DynamicVars["Evidence"].IntValue; i++)
                await WitchEncyclopediaState.GenerateRandomEvidence(player);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class JudgmentHour : EmaTrialCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [..base.CanonicalKeywords, CardKeyword.Exhaust];
    public override string PortraitPath => ModelDb.Card<FaceInCourt>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(5)];
    public JudgmentHour() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (CardModel card in PileType.Draw.GetPile(Owner).Cards
            .Where(c => !c.Keywords.Contains(ManosabaKeywords.Trial)).Take(DynamicVars.Cards.IntValue).ToList())
            CardCmd.ApplyKeyword(card, ManosabaKeywords.Trial);
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Omurice : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override string PortraitPath => ModelDb.Card<HeartyMeal>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Heal", 5m), new PowerVar<VigorPower>(4m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>()];
    public Omurice() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        await CommonActions.Apply<VigorPower>(context, Owner.Creature, this, DynamicVars["VigorPower"].BaseValue);
        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type == CardType.Status).ToList())
            await CardCmd.Exhaust(context, card);
    }
    protected override void OnUpgrade() => DynamicVars["Heal"].UpgradeValueBy(2m);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class WitchHammer : EmaTrialCard
{
    protected override bool HasProgressEffect => true;
    public override string PortraitPath => ModelDb.Card<Rebuttal>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new TrialDamageVar(8m), new DynamicVar("ExtraDamage", 13m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
        [WitchEncyclopediaState.HoverTip, HoverTipFactory.FromCard<PresentEvidence>(), HoverTipFactory.FromPower<BurnPower>(),
            HoverTipFactory.FromPower<MajokaPower>(), HoverTipFactory.FromPower<VulnerablePower>(),
            HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<BleedPower>()];
    public WitchHammer() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target is not { } target) return;
        await WitchEncyclopediaState.Present(context, Owner, SelectionScreenPrompt);
        if (!target.IsAlive) return;
        await CreatureCmd.Damage(context, target, DynamicVars.Damage.BaseValue, TrialMoveValueProp, Owner.Creature, this);
        if (!ProgressReady(this)) return;
        // Snapshot after the opening hit, so its damage-triggered effects are included.
        int hits = (target.HasPower<BurnPower>() ? 1 : 0) + (target.HasPower<MajokaPower>() ? 1 : 0)
            + (target.HasPower<VulnerablePower>() ? 1 : 0) + (target.HasPower<WeakPower>() ? 1 : 0)
            + (target.HasPower<BleedPower>() ? 1 : 0);
        for (int i = 0; i < hits && target.IsAlive; i++)
            await CreatureCmd.Damage(context, target, DynamicVars["ExtraDamage"].BaseValue, TrialMoveValueProp, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class ValentineGift : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SakurabaEmeowPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<SakurabaEmeowPower>()];
    public ValentineGift() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
        => await CommonActions.Apply<SakurabaEmeowPower>(context, Owner.Creature, this, DynamicVars["SakurabaEmeowPower"].BaseValue);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Gluttony : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique];
    public override string PortraitPath => ModelDb.Card<HeartyMeal>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GluttonyPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GluttonyPower>(), HoverTipFactory.FromKeyword(ManosabaKeywords.Unique)];
    public Gluttony() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
        => await CommonActions.Apply<GluttonyPower>(context, Owner.Creature, this, DynamicVars["GluttonyPower"].BaseValue);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
