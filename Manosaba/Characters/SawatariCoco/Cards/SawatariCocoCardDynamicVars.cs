using System;
using System.Collections.Generic;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Characters.SawatariCoco.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SawatariCoco.Cards;

/// <summary>Shared dynamic vars for Coco cards whose combat preview must reflect live conditions.</summary>
internal static class SawatariCocoCardDynamicVars
{
    internal static IEnumerable<DynamicVar> FanCountDamage()
        =>
        [
            new CalculationBaseVar(0m),
            new ExtraDamageVar(1m),
            new FanCountCalculatedDamageVar(),
        ];

    internal static IEnumerable<DynamicVar> FanCountBlock()
        =>
        [
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new FanCountCalculatedBlockVar(),
        ];

    internal static IEnumerable<DynamicVar> LiveStreamBonusDamage(decimal baseDamage, decimal bonusDamage)
        =>
        [
            new CalculationBaseVar(baseDamage),
            new ExtraDamageVar(bonusDamage),
            new LiveStreamBonusCalculatedDamageVar(),
        ];

    internal static IEnumerable<DynamicVar> LiveStreamBonusBlock(decimal baseBlock, decimal bonusBlock)
        =>
        [
            new CalculationBaseVar(baseBlock),
            new CalculationExtraVar(bonusBlock),
            new LiveStreamBonusCalculatedBlockVar(),
        ];

    internal static IEnumerable<DynamicVar> HidingBonusBlock(decimal baseBlock)
        =>
        [
            new CalculationBaseVar(baseBlock),
            new CalculationExtraVar(1m),
            new HidingBonusCalculatedBlockVar(),
        ];

    private static decimal LiveStreamBonusMultiplier(CardModel card, Creature? _)
        => card.Owner?.Creature != null && SawatariCocoHelper.IsInLiveStreamMode(card.Owner.Creature) ? 1m : 0m;

    private static decimal HidingAmountMultiplier(CardModel card, Creature? _)
        => card.Owner?.Creature?.GetPowerAmount<HidingPower>() ?? 0m;

    private static decimal FanCountMultiplier(CardModel card, Creature? _)
        => card.Owner is null ? 0m : SawatariCocoHelper.GetTotalFanCount(card.Owner);

    private static int CountEnemyFans(CardModel card)
    {
        ICombatState? combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
        Creature? ownerCreature = card.Owner?.Creature;
        if (combatState == null || ownerCreature == null)
        {
            return 0;
        }

        return SawatariCocoHelper.CountFansOf(ownerCreature, combatState);
    }

    internal sealed class FanCountCalculatedDamageVar : CalculatedDamageVar
    {
        public FanCountCalculatedDamageVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(FanCountMultiplier);
        }

        public new decimal Calculate(Creature? target)
        {
            if (_owner is not CardModel card)
            {
                return 0m;
            }

            return GetBaseVar().BaseValue + GetExtraVar().BaseValue * FanCountMultiplier(card, target);
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
            => CocoCalculatedPreviewHelper.UpdateDamage(this, card, previewMode, target, runGlobalHooks, () => Calculate(target));
    }

    internal sealed class FanCountCalculatedBlockVar : CalculatedBlockVar
    {
        public FanCountCalculatedBlockVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(FanCountMultiplier);
        }

        public new decimal Calculate(Creature? target)
        {
            if (_owner is not CardModel card)
            {
                return 0m;
            }

            return GetBaseVar().BaseValue + GetExtraVar().BaseValue * FanCountMultiplier(card, target);
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
            => CocoCalculatedPreviewHelper.UpdateBlock(this, card, previewMode, target, runGlobalHooks, () => Calculate(target));
    }

    internal sealed class LiveStreamBonusCalculatedDamageVar : CalculatedDamageVar
    {
        public LiveStreamBonusCalculatedDamageVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(LiveStreamBonusMultiplier);
        }

        public new decimal Calculate(Creature? target)
        {
            if (_owner is not CardModel card)
            {
                return 0m;
            }

            return GetBaseVar().BaseValue + GetExtraVar().BaseValue * LiveStreamBonusMultiplier(card, target);
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
            => CocoCalculatedPreviewHelper.UpdateDamage(this, card, previewMode, target, runGlobalHooks, () => Calculate(target));
    }

    internal sealed class LiveStreamBonusCalculatedBlockVar : CalculatedBlockVar
    {
        public LiveStreamBonusCalculatedBlockVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(LiveStreamBonusMultiplier);
        }

        public new decimal Calculate(Creature? target)
        {
            if (_owner is not CardModel card)
            {
                return 0m;
            }

            return GetBaseVar().BaseValue + GetExtraVar().BaseValue * LiveStreamBonusMultiplier(card, target);
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
            => CocoCalculatedPreviewHelper.UpdateBlock(this, card, previewMode, target, runGlobalHooks, () => Calculate(target));
    }

    internal sealed class HidingBonusCalculatedBlockVar : CalculatedBlockVar
    {
        public HidingBonusCalculatedBlockVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(HidingAmountMultiplier);
        }

        public new decimal Calculate(Creature? target)
        {
            if (_owner is not CardModel card)
            {
                return 0m;
            }

            return GetBaseVar().BaseValue + GetExtraVar().BaseValue * HidingAmountMultiplier(card, target);
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
            => CocoCalculatedPreviewHelper.UpdateBlock(this, card, previewMode, target, runGlobalHooks, () => Calculate(target));
    }

    /// <summary>1 + LiveStreamMode stacks; reuses Repeat var name for card text.</summary>
    internal sealed class LiveStreamHitCountVar : RepeatVar
    {
        public LiveStreamHitCountVar()
            : base(1)
        {
        }

        public int GetHitCount(CardModel card)
        {
            if (card.Owner?.Creature is not { } creature)
            {
                return 1;
            }

            return 1 + (int)creature.GetPowerAmount<LiveStreamModePower>();
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            _ = previewMode;
            _ = target;
            _ = runGlobalHooks;
            PreviewValue = GetHitCount(card);
        }
    }

    private static decimal EnemyFanCountMultiplier(CardModel card, Creature? _)
        => CountEnemyFans(card);

    /// <summary>Cards drawn total = per-fan draw count × fan count; must be <see cref="CalculatedVar"/> for :diff() and :energyIcons().</summary>
    internal sealed class EnemyFanTotalDrawsVar : CalculatedVar
    {
        public const string VarName = "TotalDraws";

        public EnemyFanTotalDrawsVar()
            : base(VarName)
        {
            WithMultiplier(EnemyFanCountMultiplier);
        }

        protected override DynamicVar GetExtraVar()
            => ((CardModel)_owner!).DynamicVars.Cards;

        public new decimal Calculate(Creature? target)
        {
            if (_owner is not CardModel card || !CombatManager.Instance.IsInProgress)
            {
                return 0m;
            }

            return GetBaseVar().BaseValue + GetExtraVar().BaseValue * EnemyFanCountMultiplier(card, target);
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            _ = previewMode;
            _ = runGlobalHooks;
            PreviewValue = Calculate(target);
        }
    }

    /// <summary>Energy gained total = per-fan energy × fan count; must be <see cref="CalculatedVar"/> for :energyIcons().</summary>
    internal sealed class EnemyFanTotalEnergyVar : CalculatedVar
    {
        public const string VarName = "TotalEnergyGain";

        public EnemyFanTotalEnergyVar()
            : base(VarName)
        {
            WithMultiplier(EnemyFanCountMultiplier);
        }

        protected override DynamicVar GetExtraVar()
            => ((CardModel)_owner!).DynamicVars.Energy;

        public new decimal Calculate(Creature? target)
        {
            if (_owner is not CardModel card || !CombatManager.Instance.IsInProgress)
            {
                return 0m;
            }

            return GetBaseVar().BaseValue + GetExtraVar().BaseValue * EnemyFanCountMultiplier(card, target);
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            _ = previewMode;
            _ = runGlobalHooks;
            PreviewValue = Calculate(target);
        }
    }

    private static class CocoCalculatedPreviewHelper
    {
        public static void UpdateDamage(
            CalculatedDamageVar calculatedVar,
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks,
            Func<decimal> calculateRaw)
        {
            EnchantmentModel? enchantment = card.Enchantment;
            if (enchantment != null)
            {
                decimal baseValue = card.DynamicVars.CalculationBase.BaseValue;
                baseValue += enchantment.EnchantDamageAdditive(baseValue, calculatedVar.Props);
                baseValue *= enchantment.EnchantDamageMultiplicative(baseValue, calculatedVar.Props);
                baseValue = Math.Max(baseValue, 0m);
                if (card.IsEnchantmentPreview)
                {
                    calculatedVar.PreviewValue = baseValue;
                }
                else
                {
                    calculatedVar.EnchantedValue = baseValue;
                }
            }

            decimal num = Math.Max(calculateRaw(), 0m);
            if (runGlobalHooks)
            {
                ICombatState? combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
                if (combatState == null || card.Owner == null)
                {
                    calculatedVar.PreviewValue = num;
                }
                else
                {
                    calculatedVar.PreviewValue = Hook.ModifyDamage(
                        card.Owner.RunState,
                        combatState,
                        target,
                        calculatedVar.IsFromOsty ? card.Owner.Osty : card.Owner.Creature,
                        num,
                        calculatedVar.Props,
                        card,
                        ModifyDamageHookType.All,
                        previewMode,
                        out IEnumerable<AbstractModel> _);
                }
            }
            else if (!card.IsEnchantmentPreview)
            {
                if (enchantment != null)
                {
                    num += enchantment.EnchantDamageAdditive(num, calculatedVar.Props);
                    num *= enchantment.EnchantDamageMultiplicative(num, calculatedVar.Props);
                }

                calculatedVar.PreviewValue = num;
            }

            calculatedVar.PreviewValue = Math.Max(calculatedVar.PreviewValue, 0m);
        }

        public static void UpdateBlock(
            CalculatedBlockVar calculatedVar,
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks,
            Func<decimal> calculateRaw)
        {
            EnchantmentModel? enchantment = card.Enchantment;
            if (enchantment != null)
            {
                decimal baseValue = card.DynamicVars.CalculationBase.BaseValue;
                baseValue += enchantment.EnchantBlockAdditive(baseValue);
                baseValue *= enchantment.EnchantBlockMultiplicative(baseValue);
                if (card.IsEnchantmentPreview)
                {
                    calculatedVar.PreviewValue = baseValue;
                }
                else
                {
                    calculatedVar.EnchantedValue = baseValue;
                }
            }

            decimal num = Math.Max(calculateRaw(), 0m);
            if (runGlobalHooks)
            {
                ICombatState? combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
                if (combatState == null || card.Owner?.Creature == null)
                {
                    calculatedVar.PreviewValue = num;
                }
                else
                {
                    calculatedVar.PreviewValue = Hook.ModifyBlock(
                        combatState,
                        card.Owner.Creature,
                        num,
                        calculatedVar.Props,
                        card,
                        null,
                        out IEnumerable<AbstractModel> _);
                }
            }
            else if (!card.IsEnchantmentPreview)
            {
                if (enchantment != null)
                {
                    num += enchantment.EnchantBlockAdditive(num);
                    num *= enchantment.EnchantBlockMultiplicative(num);
                }

                calculatedVar.PreviewValue = num;
            }

            calculatedVar.PreviewValue = Math.Max(calculatedVar.PreviewValue, 0m);
        }
    }
}
