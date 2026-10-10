using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Combat;
using manosaba.Characters.SakurabaEma.Combat;
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
    private sealed class EncyclopediaDamageVar : DamageVar
    {
        public EncyclopediaDamageVar() : base(8m, ValueProp.Move)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            decimal count = ProgressReady(card) ? WitchEncyclopediaState.CardCount(card.Owner) : 0;
            decimal damagePerEvidence = card.DynamicVars["DamagePerCard"].BaseValue;
            decimal raw = Math.Max(BaseValue + count * damagePerEvidence, 0m);

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
        new EncyclopediaDamageVar(),
        new DynamicVar("DamagePerCard", 2m),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [WitchEncyclopediaState.HoverTip, HoverTipFactory.FromCard<PresentEvidence>()];

    public Rebuttal() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        await WitchEncyclopediaState.Present(choiceContext, Owner, SelectionScreenPrompt);
        if (!cardPlay.Target.IsAlive)
            return;

        decimal count = ProgressReady(this) ? WitchEncyclopediaState.CardCount(Owner) : 0;
        decimal damage = DynamicVars.Damage.BaseValue + count * DynamicVars["DamagePerCard"].BaseValue;
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, damage, TrialMoveValueProp, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    private static ValueProp GetTrialPreviewProps(CardModel card) =>
        card.Owner?.Creature?.GetPowerAmount<LawDevilPower>() > 0m
            ? ValueProp.Move
            : ValueProp.Move | ValueProp.Unpowered;
}
