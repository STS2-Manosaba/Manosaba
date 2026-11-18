using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.TonoHanna.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards
{
    [Pool(typeof(TonoHannaCardPool))]
    public class NanokaPuppet : PathCustomCardModel
    {
        protected override HashSet<CardTag> CanonicalTags => [ManosabaCardTags.Puppet];

        private const int energyCost = 2;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Common;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;

        /// <summary>
        /// Vanilla <see cref="CalculatedVar.Calculate"/> only applies the multiplier when <c>card.CombatState != null</c>;
        /// cards in hand often have null <see cref="CardModel.CombatState"/>, so the printed damage was only the base (6).
        /// </summary>
        private sealed class NanokaPuppetCalculatedDamageVar : CalculatedDamageVar
        {
            public NanokaPuppetCalculatedDamageVar()
                : base(ValueProp.Move)
            {
                WithMultiplier(PuppetMultiplier);
            }

            private static decimal PuppetMultiplier(CardModel card, Creature? _)
            {
                if (!CombatManager.Instance.IsInProgress || card.Owner?.PlayerCombatState == null)
                {
                    return 0m;
                }

                return card.Owner.PlayerCombatState.AllCards.Count(c => c.Tags.Contains(ManosabaCardTags.Puppet));
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

                decimal num = GetBaseVar().BaseValue + GetExtraVar().BaseValue * PuppetMultiplier(card, target);
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

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new CalculationBaseVar(6m),
            new ExtraDamageVar(2m),
            new NanokaPuppetCalculatedDamageVar(),
            new PowerVar<NanokaPuppetCollectionPower>(1),
        ];

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromCard<EmaPuppet>(),
        ];

        protected override bool ShouldGlowGoldInternal =>
            Owner?.Creature is { } ownerCreature
            && PuppetCollectionHelper.HasUsedInCombat<EmaPuppetCollectionPower>(ownerCreature);

        public NanokaPuppet() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (Owner?.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
            {
                return;
            }

            bool emaUsed = PuppetCollectionHelper.HasUsedInCombat<EmaPuppetCollectionPower>(ownerCreature);
            int puppetCount = Owner.PlayerCombatState?.AllCards.Count(c => c.Tags.Contains(ManosabaCardTags.Puppet)) ?? 0;
            decimal damage = DynamicVars.CalculationBase.BaseValue + DynamicVars.ExtraDamage.BaseValue * puppetCount;
            decimal damageDealt = await CombatDamageTracker.MeasureDamageToTarget(
                target,
                () => DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(target).Execute(choiceContext));

            if (emaUsed && damageDealt > 0m)
            {
                await CreatureCmd.GainBlock(ownerCreature, damageDealt * 0.2m, ValueProp.Move, cardPlay);
            }

            await CommonActions.Apply<NanokaPuppetCollectionPower>(choiceContext, ownerCreature, this, DynamicVars["NanokaPuppetCollectionPower"].BaseValue);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.CalculationBase.UpgradeValueBy(3m);
        }
    }
}
