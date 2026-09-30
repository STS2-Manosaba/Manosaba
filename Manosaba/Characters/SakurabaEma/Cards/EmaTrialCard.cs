using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

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

    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner?.Creature?.GetPowerAmount<InterrogationStartPower>() > 0m;

    protected ValueProp TrialMoveValueProp =>
        Owner?.Creature?.GetPowerAmount<LawDevilPower>() > 0m
            ? ValueProp.Move
            : ValueProp.Move | ValueProp.Unpowered;

    protected virtual IEnumerable<IHoverTip> TrialExtraHoverTips => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Trial];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(ManosabaKeywords.Trial),
        HoverTipFactory.FromPower<InterrogationStartPower>(),
        HoverTipFactory.FromPower<LawDevilPower>(),
        ..TrialExtraHoverTips,
    ];
}
