using System;
using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.TonoHanna.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards;

/// <summary>Vanilla-style <c>Anger</c>: deal damage, add a copy to <see cref="PileType.Discard"/>.</summary>
[Pool(typeof(TonoHannaCardPool))]
public sealed class HiroPuppet : PathCustomCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [ManosabaCardTags.Puppet];

    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        IsUpgraded ? [HoverTipFactory.FromCard<EmaPuppet>()] : [];

    protected override bool ShouldGlowGoldInternal =>
        Owner?.Creature is { } ownerCreature
        && IsUpgraded
        && PuppetCollectionHelper.HasUsedInCombat<EmaPuppetCollectionPower>(ownerCreature);

    public HiroPuppet()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
        {
            return;
        }

        bool emaUsedInCombat = PuppetCollectionHelper.HasUsedInCombat<EmaPuppetCollectionPower>(ownerCreature);
        decimal damage = DynamicVars.Damage.BaseValue;

        decimal damageDealt = await CombatDamageTracker.MeasureDamageToTarget(
            target,
            () => DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .Execute(choiceContext));

        if (IsUpgraded && emaUsedInCombat && damageDealt > 0m)
        {
            await CreatureCmd.Heal(ownerCreature, damageDealt * 0.2m);
        }

        await CommonActions.Apply<HiroPuppetCollectionPower>(choiceContext, ownerCreature, this, 1m);
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner),
            2.2f);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
