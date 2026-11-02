using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Characters.NikaidoHiro.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class GazeGuiding : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [ HoverTipFactory.FromPower<MajokaPower>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Mahou, CardKeyword.Eternal];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(50m),
        new CalculationExtraVar(50m),
        new CalculatedVar("DamageReductionPercent").WithMultiplier(GetMajokaFactor),
    ];

    public GazeGuiding()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
            return Task.CompletedTask;

        decimal reduction = ((CalculatedVar)DynamicVars["DamageReductionPercent"]).Calculate(null);
        return CommonActions.Apply<GazeGuidingPower>(choiceContext, target, this, reduction);
    }

    protected override void OnUpgrade()
    {
        base.AddKeyword(CardKeyword.Retain);
    }

    private static decimal GetMajokaFactor(CardModel card, Creature? _)
        => Math.Min(card.Owner?.Creature?.GetPowerAmount<MajokaPower>() / 100m ?? 0m, 1m);
}

