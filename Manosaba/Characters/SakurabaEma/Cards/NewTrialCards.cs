using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class ExhaustiveMethod : EmaTrialCard
{
    private sealed class ProgressDamageVar() : DamageVar(12m, ValueProp.Move | ValueProp.Unpowered)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            decimal raw = BaseValue + (ProgressReady(card) ? Progress(card) : 0);
            Props = card.Owner?.Creature?.HasPower<LawDevilPower>() == true ? ValueProp.Move : ValueProp.Move | ValueProp.Unpowered;
            PreviewValue = runGlobalHooks && card.Owner != null && (card.CombatState ?? card.Owner.Creature.CombatState) is { } field
                ? Hook.ModifyDamage(card.Owner.RunState, field, target, card.Owner.Creature, raw, Props, card,
                    ModifyDamageHookType.All, previewMode, out _) : raw;
        }
    }
    public override string PortraitPath => ModelDb.Card<Rebuttal>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ProgressDamageVar()];
    public ExhaustiveMethod() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is { IsAlive: true } target)
            await CreatureCmd.Damage(choiceContext, target, DynamicVars.Damage.BaseValue + (ProgressReady(this) ? Progress(this) : 0), TrialMoveValueProp, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}

public abstract class AccUhEartsBase : EmaTrialCard
{
    private sealed class HitsVar() : DynamicVar("Hits", 1m)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks) =>
            PreviewValue = card is AccUhEartsBase acc ? acc.HitCount : 1;
    }
    public abstract int MaximumHits { get; }
    public int HitCount => ProgressReady(this) ? Math.Min(MaximumHits, 1 + WitchEncyclopediaState.CardCount(Owner)) : 1;
    public override string PortraitPath => ModelDb.Card<Rebuttal>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new TrialDamageVar(5m), new HitsVar(), new DynamicVar("MaxHits", MaximumHits), ..AccVars];
    protected virtual IEnumerable<DynamicVar> AccVars => [];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [WitchEncyclopediaState.HoverTip];
    protected AccUhEartsBase(CardRarity rarity) : base(1, CardType.Attack, rarity, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
            return;
        int hits = HitCount;
        for (int i = 0; i < hits && target.IsAlive; i++)
            await CreatureCmd.Damage(choiceContext, target, DynamicVars.Damage.BaseValue, TrialMoveValueProp, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class AccUhEartsIndictment : AccUhEartsBase
{
    private int _battlesCompleted;
    public const int BattlesToTransform = 13;
    [SavedProperty]
    public int BattlesCompleted
    {
        get => _battlesCompleted;
        set
        {
            AssertMutable();
            _battlesCompleted = Math.Clamp(value, 0, BattlesToTransform);
            DynamicVars["BattlesRemaining"].BaseValue = BattlesToTransform - _battlesCompleted;
        }
    }
    public override int MaximumHits => 5;
    protected override IEnumerable<DynamicVar> AccVars => [new DynamicVar("BattlesRemaining", BattlesToTransform)];
    public AccUhEartsIndictment() : base(CardRarity.Rare) { }
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        // Run and combat copies both receive this callback. Count only the
        // persistent deck instance, regardless of whether it was drawn/played.
        if (Owner == null || CardScope == null || !Owner.Deck.Cards.Contains(this))
            return;
        BattlesCompleted++;
        if (BattlesCompleted < BattlesToTransform)
            return;
        var replacement = Owner.RunState.CreateCard<AccUhEartsRevelation>(Owner);
        if (IsUpgraded)
        {
            replacement.UpgradeInternal();
            replacement.FinalizeUpgradeInternal();
        }
        await CardCmd.Transform(this, replacement, CardPreviewStyle.None);
    }
}

// Only the indictment's scripted transformation creates this Ancient card;
// it is outside reward/shop/character pools and all random generation pools.
[Pool(typeof(TokenCardPool))]
public sealed class AccUhEartsRevelation : AccUhEartsBase
{
    public override int MaximumHits => 10;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<SakurabaEmaCardPool>();
    public AccUhEartsRevelation() : base(CardRarity.Ancient) { }
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class RequestRetrial : EmaTrialCard
{
    public override string PortraitPath => ModelDb.Card<RawTellOwk>().PortraitPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("RequiredSuspicion", 10m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromPower<SusPower>(), HoverTipFactory.FromPower<RetrialExtraTurnPower>()];
    protected override bool IsPlayable => base.IsPlayable && Owner?.Creature?.GetPowerAmount<SusPower>() >= DynamicVars["RequiredSuspicion"].BaseValue;
    public RequestRetrial() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature.GetPower<SusPower>() is not { } suspicion || suspicion.Amount < DynamicVars["RequiredSuspicion"].BaseValue)
            return;
        await PowerCmd.Remove(suspicion);
        await CommonActions.Apply<RetrialExtraTurnPower>(choiceContext, Owner.Creature, this, 1m);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Scabbard : PathCustomCardModel
{
    public override string PortraitPath => ModelDb.Card<RawTellOwk>().PortraitPath;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(ManosabaKeywords.Unique), HoverTipFactory.FromPower<ScabbardPower>()];
    // Its defensive effect works against teammates; solo acquisition is kept
    // available as requested, even though solo has no ally damage to intercept.
    public Scabbard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => CommonActions.Apply<ScabbardPower>(choiceContext, Owner.Creature, this, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}