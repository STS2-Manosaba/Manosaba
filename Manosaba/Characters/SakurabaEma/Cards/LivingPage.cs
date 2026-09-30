using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class LivingPage : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
        new DynamicVar("VulnerableThreshold", 3m),
        new EnergyVar(1),
        new DynamicVar("DamagePerUse", 1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<LivingPageUseCountPower>(),
        EnergyHoverTip,
    ];

    public LivingPage() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        decimal previousUses = Owner.Creature.GetPowerAmount<LivingPageUseCountPower>();
        decimal damage = DynamicVars.Damage.BaseValue + previousUses * DynamicVars["DamagePerUse"].BaseValue;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        await CommonActions.Apply<VulnerablePower>(choiceContext, cardPlay.Target, this, DynamicVars["VulnerablePower"].BaseValue);

        if (cardPlay.Target.GetPowerAmount<VulnerablePower>() >= DynamicVars["VulnerableThreshold"].BaseValue)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }

        await CommonActions.Apply<LivingPageUseCountPower>(choiceContext, Owner.Creature, this, 1m);
    }

    protected override void OnUpgrade()
    {
    }
}
