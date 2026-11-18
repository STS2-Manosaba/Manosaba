using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.SawatariCoco.Equipment;
using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public sealed class PunkCatHeadwear : EquipmentCardModel
{
    protected override EquipmentSlot Slot => EquipmentSlot.Headwear;
    protected override EquipmentSeries Series => EquipmentSeries.PunkCat;
    protected override int EquipmentScore => 2000;
    protected override CardTag SeriesTag => ManosabaCardTags.PunkCatEquipment;

    protected override IEnumerable<DynamicVar> CanonicalVars => WithEquipmentScoreFanCountDamage();

    public PunkCatHeadwear() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies, true)
    {
    }

    protected override async Task PlayEquipmentEffectAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (CombatState is not { } combatState || Owner.Creature is not { } dealer)
        {
            return;
        }

        decimal damage = SawatariCocoHelper.GetTotalFanCount(Owner);

        if (damage <= 0m)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, combatState.HittableEnemies, damage, ValueProp.Move, dealer, this, null);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[Pool(typeof(SawatariCocoCardPool))]
public sealed class PunkCatTop : EquipmentCardModel
{
    protected override EquipmentSlot Slot => EquipmentSlot.Top;
    protected override EquipmentSeries Series => EquipmentSeries.PunkCat;
    protected override int EquipmentScore => 5000;
    protected override CardTag SeriesTag => ManosabaCardTags.PunkCatEquipment;
    protected override IEnumerable<IHoverTip> CardExtraHoverTips => [HoverTipFactory.FromPower<LiveStreamModePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => WithEquipmentScore(
        new DamageVar(5, ValueProp.Move),
        new SawatariCocoCardDynamicVars.LiveStreamHitCountVar());

    public PunkCatTop() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task PlayEquipmentEffectAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        int hitCount = DynamicVars.Repeat is SawatariCocoCardDynamicVars.LiveStreamHitCountVar hitCountVar
            ? hitCountVar.GetHitCount(this)
            : 1 + GetLiveStreamHitBonus(Owner.Creature);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

[Pool(typeof(SawatariCocoCardPool))]
public sealed class PunkCatSkirt : EquipmentCardModel
{
    private const decimal liveModeBonusDamage = 4m;

    protected override EquipmentSlot Slot => EquipmentSlot.Skirt;
    protected override EquipmentSeries Series => EquipmentSeries.PunkCat;
    protected override int EquipmentScore => 2000;
    protected override CardTag SeriesTag => ManosabaCardTags.PunkCatEquipment;
    protected override IEnumerable<IHoverTip> CardExtraHoverTips =>
    [
        HoverTipFactory.FromPower<LiveStreamModePower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => WithEquipmentScoreLiveStreamBonusDamage(8m, liveModeBonusDamage)
        .Concat([new PowerVar<VulnerablePower>(1m)]);

    public PunkCatSkirt() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task PlayEquipmentEffectAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        await CommonActions.Apply<VulnerablePower>(choiceContext, target, this, DynamicVars["VulnerablePower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
    }
}

[Pool(typeof(SawatariCocoCardPool))]
public sealed class PunkCatShoes : EquipmentCardModel
{
    private const decimal liveModeBonusDamage = 4m;

    protected override EquipmentSlot Slot => EquipmentSlot.Shoes;
    protected override EquipmentSeries Series => EquipmentSeries.PunkCat;
    protected override int EquipmentScore => 1000;
    protected override CardTag SeriesTag => ManosabaCardTags.PunkCatEquipment;
    protected override IEnumerable<IHoverTip> CardExtraHoverTips => [HoverTipFactory.FromPower<LiveStreamModePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => WithEquipmentScoreLiveStreamBonusDamage(9m, liveModeBonusDamage);

    public PunkCatShoes() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task PlayEquipmentEffectAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4m);
    }
}
