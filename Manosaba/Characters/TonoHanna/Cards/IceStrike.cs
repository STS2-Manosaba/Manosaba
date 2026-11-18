using System;
using System.Collections.Generic;
using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.TonoHanna.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public class IceStrike : PathCustomCardModel
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    private const int energyCost = 3;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    /// <summary>
    /// Vanilla <see cref="CalculatedVar.Calculate"/> only applies the multiplier when <c>card.CombatState != null</c>;
    /// hand previews need <see cref="PuppetCollectionSummaryPower"/> stacks without that gate.
    /// </summary>
    private sealed class IceStrikeCalculatedDamageVar : CalculatedDamageVar
    {
        public IceStrikeCalculatedDamageVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(CollectionMultiplier);
        }

        private static decimal CollectionMultiplier(CardModel card, Creature? _)
        {
            if (card.Owner?.Creature == null)
            {
                return 0m;
            }

            return card.Owner.Creature.GetPower<PuppetCollectionSummaryPower>()?.Amount ?? 0;
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            EnchantmentModel? enchantment = card.Enchantment;
            if (enchantment != null)
            {
                decimal baseValue = GetBaseVar().BaseValue;
                baseValue += enchantment.EnchantDamageAdditive(baseValue, Props);
                baseValue *= enchantment.EnchantDamageMultiplicative(baseValue, Props);
                baseValue = Math.Max(baseValue, 0m);
                if (card.IsEnchantmentPreview)
                {
                    PreviewValue = baseValue;
                }
                else
                {
                    EnchantedValue = baseValue;
                }
            }

            decimal num = GetBaseVar().BaseValue + GetExtraVar().BaseValue * CollectionMultiplier(card, target);
            if (runGlobalHooks)
            {
                ICombatState? combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
                if (combatState == null || card.Owner == null)
                {
                    PreviewValue = Math.Max(num, 0m);
                }
                else
                {
                    PreviewValue = Hook.ModifyDamage(
                        card.Owner.RunState,
                        combatState,
                        target,
                        IsFromOsty ? card.Owner.Osty : card.Owner.Creature,
                        num,
                        Props,
                        card, null,
                        ModifyDamageHookType.All,
                        previewMode,
                        out IEnumerable<AbstractModel> _);
                }
            }
            else if (!card.IsEnchantmentPreview)
            {
                if (enchantment != null)
                {
                    num += enchantment.EnchantDamageAdditive(num, Props);
                    num *= enchantment.EnchantDamageMultiplicative(num, Props);
                }

                PreviewValue = num;
            }

            PreviewValue = Math.Max(PreviewValue, 0m);
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(25m),
        new ExtraDamageVar(5m),
        new IceStrikeCalculatedDamageVar(),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PuppetCollectionSummaryPower>()];

    public IceStrike() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature is null || cardPlay.Target is not { } target)
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
        DynamicVars.CalculationBase.UpgradeValueBy(5m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
