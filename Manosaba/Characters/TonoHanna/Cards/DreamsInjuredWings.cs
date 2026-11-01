using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class DreamsInjuredWings : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FloatingPower>(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(ManosabaKeywords.Floating)];

    public DreamsInjuredWings() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CommonActions.Apply<FloatingPower>(choiceContext, ownerCreature, this, DynamicVars["FloatingPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FloatingPower"].UpgradeValueBy(1m);
    }
}
