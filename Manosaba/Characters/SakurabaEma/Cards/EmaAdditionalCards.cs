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
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class EndlessEvidence : PathCustomCardModel
{
    public override string PortraitPath => ModelDb.Card<SearchSakurabaEma>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<EndlessEvidencePower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<EndlessEvidencePower>(1), WitchEncyclopediaState.HoverTip];
    public EndlessEvidence() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
        => await CommonActions.Apply<EndlessEvidencePower>(context, Owner.Creature, this, 1m);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class EnoughRedTape : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [WitchEncyclopediaState.HoverTip, HoverTipFactory.FromCard<PresentEvidence>()];
    public EnoughRedTape() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        for (int i = 0; i < 3; i++)
            await WitchEncyclopediaState.Present(context, Owner, SelectionScreenPrompt);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class DeductionAfterDinner : PathCustomCardModel
{
    public override string PortraitPath => ModelDb.Card<HeartyMeal>().PortraitPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<HeartyMeal>(), HoverTipFactory.FromCard<Statement>()];
    public DeductionAfterDinner() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<HeartyMeal>(Owner), PileType.Hand, Owner));
        await CommonActions.Apply<AfterDinnerStatementPower>(context, Owner.Creature, this, 2m);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class WhatIsEmasMagic : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<GiganticMagic>(), HoverTipFactory.FromCard<FireBreathingMagic>(), HoverTipFactory.FromCard<GlowingMagic>()];
    public WhatIsEmasMagic() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (CombatState is not { } combat) return;
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(context,
            [combat.CreateCard<GiganticMagic>(Owner), combat.CreateCard<FireBreathingMagic>(Owner), combat.CreateCard<GlowingMagic>(Owner)], Owner);
        if (selected != null) await CardCmd.AutoPlay(context, selected, null);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public abstract class EmaMagicOption : PathCustomCardModel
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override int MaxUpgradeLevel => 0;
    public override string PortraitPath => "res://Manosaba/images/cards/what_is_emas_magic_options.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Mahou, CardKeyword.Eternal];
    protected EmaMagicOption(CardType type, TargetType target) : base(0, type, CardRarity.Token, target, true) { }
    protected override void OnUpgrade() { }
}

[Pool(typeof(TokenCardPool))]
public sealed class GiganticMagic : EmaMagicOption
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GigantificationPower>()];
    public GiganticMagic() : base(CardType.Power, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
        => await CommonActions.Apply<GigantificationPower>(context, Owner.Creature, this, 1m);
}

[Pool(typeof(TokenCardPool))]
public sealed class FireBreathingMagic : EmaMagicOption
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [..base.CanonicalKeywords, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20m, ValueProp.Move), new PowerVar<BurnPower>(10m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BurnPower>()];
    public FireBreathingMagic() : base(CardType.Skill, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target is not { } target) return;
        await CreatureCmd.Damage(context, target, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, this);
        if (target.IsAlive) await CommonActions.Apply<BurnPower>(context, target, this, DynamicVars["BurnPower"].BaseValue);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class GlowingMagic : EmaMagicOption
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [..base.CanonicalKeywords, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(10)];
    public GlowingMagic() : base(CardType.Skill, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.ToList())
            await CardCmd.Exhaust(context, card);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
}
