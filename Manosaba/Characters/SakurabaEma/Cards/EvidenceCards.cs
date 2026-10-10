using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

public abstract class EvidenceCard : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Evidence];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override int MaxUpgradeLevel => 0;
    // Temporary art until individual evidence portraits are supplied.
    public override string PortraitPath => "res://Manosaba/images/cards/search_sakuraba_ema.png";
    protected virtual IEnumerable<IHoverTip> EvidenceHoverTips => [];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(ManosabaKeywords.Evidence), WitchEncyclopediaState.HoverTip, ..EvidenceHoverTips];

    protected EvidenceCard(CardType type, TargetType target) : base(0, type, CardRarity.Token, target, true) { }
    protected override PileType GetResultPileTypeForCardPlay() => WitchEncyclopediaState.PileType;
    // Evidence has no upgrade effects by design; it is a reusable generated token.
    protected override void OnUpgrade() { }
}

[Pool(typeof(TokenCardPool))]
public sealed class BrokenCrossbowEvidence : EvidenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move)];
    public BrokenCrossbowEvidence() : base(CardType.Attack, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is { } target)
            await CreatureCmd.Damage(choiceContext, target, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, this);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class NoahsButterflyDrawing : EvidenceCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3m, ValueProp.Move)];
    public NoahsButterflyDrawing() : base(CardType.Skill, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
}

[Pool(typeof(TokenCardPool))]
public sealed class WitchIslandPrisonBlueprint : EvidenceCard
{
    protected override IEnumerable<IHoverTip> EvidenceHoverTips => [HoverTipFactory.FromCard<HouseKeeping>(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.FromPower<GuiltPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GuiltPower>(1m)];
    // The self effect remains useful in solo; the optional Sherry bonus must also be
    // available there, so this card intentionally has no multiplayer-only constraint.
    public WitchIslandPrisonBlueprint() : base(CardType.Skill, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combat)
            return;
        var housekeeping = combat.CreateCard<HouseKeeping>(Owner);
        CardCmd.ApplyKeyword(housekeeping, CardKeyword.Exhaust);
        await CardCmd.AutoPlay(choiceContext, housekeeping, null);
        var sherry = combat.Players.Where(player => player.Creature.IsAlive &&
                player.Character.Id.Entry.EndsWith("TACHIBANA_SHERRY", StringComparison.OrdinalIgnoreCase))
            .OrderBy(player => player.NetId).FirstOrDefault();
        if (sherry != null)
            await CommonActions.Apply<GuiltPower>(choiceContext, sherry.Creature, this, DynamicVars["GuiltPower"].BaseValue);
    }
}
