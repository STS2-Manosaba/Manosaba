using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class SpearMastery : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SpearMasteryPower>(),
        HoverTipFactory.FromCard<SimpleSpear>(true),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SpearMasteryPower>(1m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique];

    public SpearMastery()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        CommonActions.Apply<SpearMasteryPower>(choiceContext, Owner.Creature, this, DynamicVars["SpearMasteryPower"].BaseValue);

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
