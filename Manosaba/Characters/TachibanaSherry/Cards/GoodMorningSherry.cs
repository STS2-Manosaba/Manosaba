using System;
using System.Collections.Generic;
using BaseLib.Utils;
using manosaba.Characters.TachibanaSherry;
using Manosaba.Characters.TachibanaSherry.Powers;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TachibanaSherry.Cards;

[Pool(typeof(TachibanaSherryCardPool))]
public sealed class GoodMorningSherry : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    /// <summary>
    /// Vanilla <see cref="CalculatedVar.Calculate"/> gates the multiplier on <c>card.CombatState</c>;
    /// <see cref="SherryDetectiveRewardPower"/> should affect hand previews without that gate.
    /// </summary>
    private sealed class GoodMorningSherryCalculatedDamageVar : CalculatedDamageVar
    {
        public GoodMorningSherryCalculatedDamageVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(DetectiveBonusMultiplier);
        }

        private static decimal DetectiveBonusMultiplier(CardModel card, Creature? _)
        {
            if (card.Owner?.Creature == null)
            {
                return 0m;
            }

            return card.Owner.Creature.GetPowerAmount<SherryDetectiveRewardPower>() > 0 ? 1m : 0m;
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

            decimal num = GetBaseVar().BaseValue + GetExtraVar().BaseValue * DetectiveBonusMultiplier(card, target);
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SherryDetectiveRewardPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(15m),
        new ExtraDamageVar(5m),
        new GoodMorningSherryCalculatedDamageVar(),
    ];

    public GoodMorningSherry() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, ownerCreature, 2m, ValueProp.Unpowered, ownerCreature);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(5m);
    }
}
