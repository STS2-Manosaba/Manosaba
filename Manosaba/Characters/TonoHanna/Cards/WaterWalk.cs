using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class WaterWalk : PathCustomCardModel
{
    private const int energyCost = 2;
    private const CardType cardType = CardType.Skill;
    private const CardRarity cardRarity = CardRarity.Uncommon;
    private const TargetType playTarget = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BufferPower>(1m),
        new PowerVar<FloatingPower>(1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BufferPower>(),
        HoverTipFactory.FromKeyword(ManosabaKeywords.Floating),
    ];

    public WaterWalk()
        : base(energyCost, cardType, cardRarity, playTarget, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CommonActions.Apply<BufferPower>(choiceContext, Owner.Creature, this, DynamicVars["BufferPower"].BaseValue);
        await CommonActions.Apply<FloatingPower>(choiceContext, Owner.Creature, this, DynamicVars["FloatingPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FloatingPower"].UpgradeValueBy(1m);
    }
}
