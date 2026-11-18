using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public sealed class FlameBait : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const decimal liveModeBonusDamage = 20m;

    private sealed class FlameBaitCalculatedDamageVar : CalculatedDamageVar
    {
        public FlameBaitCalculatedDamageVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(LiveModeBonusMultiplier);
        }

        private static decimal LiveModeBonusMultiplier(CardModel card, Creature? _)
        {
            if (card.Owner?.Creature == null)
            {
                return 0m;
            }

            return SawatariCocoHelper.IsInLiveStreamMode(card.Owner.Creature) ? 1m : 0m;
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

            decimal num = GetBaseVar().BaseValue + GetExtraVar().BaseValue * LiveModeBonusMultiplier(card, target);
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
                        card.Owner.Creature,
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
        new HpLossVar(2m),
        new CalculationBaseVar(10m),
        new ExtraDamageVar(liveModeBonusDamage),
        new FlameBaitCalculatedDamageVar(),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LiveStreamModePower>()];

    public FlameBait() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            ownerCreature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this, null);

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
