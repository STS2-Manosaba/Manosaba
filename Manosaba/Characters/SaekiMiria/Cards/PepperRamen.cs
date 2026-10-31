using BaseLib.Utils;
using manosaba.Characters.SaekiMiria;
using Manosaba.Extensions;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.SaekiMiria.Cards;

[Pool(typeof(SaekiMiriaCardPool))]
public sealed class PepperRamen : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HealingPower>(),
        HoverTipFactory.FromPower<PoisonPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<HealingPower>(3m),
        new PowerVar<PoisonPower>(1m),
    ];

    public PepperRamen()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        _ = cardPlay;

        await CommonActions.Apply<HealingPower>(choiceContext, Owner.Creature, this, DynamicVars["HealingPower"].BaseValue);
        await CommonActions.Apply<PoisonPower>(choiceContext, Owner.Creature, this, DynamicVars.Poison.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HealingPower"].UpgradeValueBy(3m);
        DynamicVars["PoisonPower"].UpgradeValueBy(2m);
    }
}
