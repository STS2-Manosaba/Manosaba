using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
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

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Rebuttal : EmaTrialCard
{
    private sealed class EvidenceDamageVar : DamageVar
    {
        public EvidenceDamageVar() : base(8m, ValueProp.Move)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            decimal evidence = card.Owner?.Creature?.GetPowerAmount<EvidencePower>() ?? 0m;
            decimal damagePerEvidence = card.DynamicVars["DamagePerEvidence"].BaseValue;
            decimal raw = Math.Max(BaseValue + evidence * damagePerEvidence, 0m);

            ValueProp props = GetTrialPreviewProps(card);
            if (runGlobalHooks)
            {
                ICombatState? combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
                if (combatState == null || card.Owner == null)
                {
                    PreviewValue = raw;
                }
                else
                {
                    PreviewValue = Hook.ModifyDamage(
                        card.Owner.RunState,
                        combatState,
                        target,
                        card.Owner.Creature,
                        raw,
                        props,
                        card,
                        ModifyDamageHookType.All,
                        previewMode,
                        out IEnumerable<AbstractModel> _);
                }
            }
            else if (!card.IsEnchantmentPreview)
            {
                PreviewValue = raw;
            }

            PreviewValue = Math.Max(PreviewValue, 0m);
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EvidenceDamageVar(),
        new DynamicVar("DamagePerEvidence", 2m),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromPower<EvidencePower>()];

    public Rebuttal() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        decimal evidence = Owner.Creature.GetPowerAmount<EvidencePower>();
        decimal damage = DynamicVars.Damage.BaseValue + evidence * DynamicVars["DamagePerEvidence"].BaseValue;
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, damage, TrialMoveValueProp, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    private static ValueProp GetTrialPreviewProps(CardModel card) =>
        card.Owner?.Creature?.GetPowerAmount<LawDevilPower>() > 0m
            ? ValueProp.Move
            : ValueProp.Move | ValueProp.Unpowered;
}
