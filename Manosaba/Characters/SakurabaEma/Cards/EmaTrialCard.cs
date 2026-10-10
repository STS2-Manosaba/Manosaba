using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Manosaba.Combat;

namespace manosaba.Characters.SakurabaEma.Cards;

public abstract class EmaTrialCard : PathCustomCardModel
{
    protected EmaTrialCard(
        int energyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary) : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public const int ProgressThreshold = 10;
    public static int Progress(CardModel card) =>
        (card.CombatState ?? card.Owner?.Creature?.CombatState) is { } field
            ? FieldPowerState.Get(field).OfType<InterrogationProgressPower>().FirstOrDefault()?.Amount ?? 0
            : 0;
    public static bool ProgressReady(CardModel card) => Progress(card) >= ProgressThreshold;

    protected ValueProp TrialMoveValueProp =>
        Owner?.Creature?.GetPowerAmount<LawDevilPower>() > 0m
            ? ValueProp.Move
            : ValueProp.Move | ValueProp.Unpowered;

    protected virtual IEnumerable<IHoverTip> TrialExtraHoverTips => [];

    // Use the same Unpowered rule for the displayed damage as for actual resolution.
    protected sealed class TrialDamageVar(decimal damage) : DamageVar(damage, ValueProp.Move | ValueProp.Unpowered)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            Props = card.Owner?.Creature?.GetPowerAmount<LawDevilPower>() > 0m
                ? ValueProp.Move
                : ValueProp.Move | ValueProp.Unpowered;
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Trial];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(ManosabaKeywords.Trial),
        HoverTipFactory.FromPower<InterrogationStartPower>(),
        HoverTipFactory.FromPower<InterrogationProgressPower>(),
        HoverTipFactory.FromPower<LawDevilPower>(),
        ..TrialExtraHoverTips,
    ];
}
