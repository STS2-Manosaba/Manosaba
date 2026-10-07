using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(CommonCardPool))]
public sealed class GuiltConclusive : EmaTrialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<GuiltPower>(1m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
        [HoverTipFactory.FromPower<SusPower>(), HoverTipFactory.FromPower<GuiltPower>()];
    protected override bool IsPlayable => base.IsPlayable && Owner?.Creature?.GetPowerAmount<SusPower>() > 0;
    public override string PortraitPath => ModelDb.Card<Rebuttal>().PortraitPath;

    public GuiltConclusive() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature.GetPowerAmount<SusPower>() < 1)
            return;

        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, -1m);
        await CommonActions.Apply<GuiltPower>(choiceContext, Owner.Creature, this, DynamicVars["GuiltPower"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["GuiltPower"].UpgradeValueBy(1m);
}
