using BaseLib.Utils;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.Common.Overrides;
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
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

public abstract class EvidenceCard : EncyclopediaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Evidence];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected virtual IEnumerable<IHoverTip> EvidenceHoverTips => [];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(ManosabaKeywords.Evidence), WitchEncyclopediaState.HoverTip, ..EvidenceHoverTips];

    protected EvidenceCard(CardType type, TargetType target) : base(0, type, CardRarity.Token, target, true) { }
    protected override MegaCrit.Sts2.Core.Entities.Cards.CardLocation GetResultLocationForCardPlay() => new(Owner, WitchEncyclopediaState.PileType, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Bottom);
    protected override void OnUpgrade() { }
}

[Pool(typeof(TokenCardPool))]
public sealed class BrokenCrossbowEvidence : SearchEvidenceCard
{
    public BrokenCrossbowEvidence() : base(CardType.Attack) { }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move), new PowerVar<FleetingStrengthPower>(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<FleetingStrengthPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Hit(context, RandomEnemy(), play);
        await Apply<FleetingStrengthPower>(context, Owner.Creature, DynamicVars["FleetingStrengthPower"].BaseValue);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["FleetingStrengthPower"].UpgradeValueBy(1);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class NoahsButterflyDrawing : SearchEvidenceCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Move), new PowerVar<FleetingDexterityPower>(1)];
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromPower<FleetingDexterityPower>()];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await Apply<FleetingDexterityPower>(context, Owner.Creature, DynamicVars["FleetingDexterityPower"].BaseValue);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["FleetingDexterityPower"].UpgradeValueBy(1);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class WitchIslandPrisonBlueprint : SearchEvidenceCard
{
    protected override IEnumerable<IHoverTip> EvidenceHoverTips =>
        [HoverTipFactory.FromCard<HouseKeeping>(), HoverTipFactory.FromPower<GuiltPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GuiltPower>(1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await GenerateAndPlay(context, ModelDb.Card<HouseKeeping>());
        await BuffMatches<GuiltPower>(context, "tachibana_sherry", DynamicVars["GuiltPower"].BaseValue);
    }
    protected override void OnUpgrade() => DynamicVars["GuiltPower"].UpgradeValueBy(1);
}
