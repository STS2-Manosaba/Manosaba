using BaseLib.Utils;
using manosaba.Characters.ShitoAlisa;
using Manosaba.Characters.ShitoAlisa.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.ShitoAlisa.Cards;

[Pool(typeof(ShitoAlisaCardPool))]
public class YouAreWeakEma : ShitoAlisaCardModel
{
    private sealed class WeakEmaCalculatedDamageVar : CalculatedDamageVar
    {
        public WeakEmaCalculatedDamageVar()
            : base(ValueProp.Move)
        {
            WithMultiplier(CalculateFireballStacks);
        }

        private static decimal CalculateFireballStacks(CardModel card, Creature? target)
        {
            if (card.Owner?.Creature == null)
                return 0m;
            return card.Owner.Creature.GetPowerAmount<FireballSwarmPower>();
        }
    }

    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        WithCombust(0, new CalculationBaseVar(5m), new ExtraDamageVar(3m), new WeakEmaCalculatedDamageVar());
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FireballSwarmPower>()];

    public YouAreWeakEma() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        decimal totalDamage = DynamicVars.CalculatedDamage.Calculate(cardPlay.Target);
        if (totalDamage <= 0m)
            return;

        await DamageCmd.Attack(totalDamage).FromCard(this, cardPlay).Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(2m);
    }
}
