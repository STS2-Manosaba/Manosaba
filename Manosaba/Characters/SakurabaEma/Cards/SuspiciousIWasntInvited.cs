using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class SuspiciousIWasntInvited : EmaTrialCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(10m, ValueProp.Move), new DynamicVar("AllyBlock", 3m), new CardsVar(1), new PowerVar<SusPower>(1m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromPower<SusPower>()];
    public SuspiciousIWasntInvited() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer, true) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(DynamicVars.Block.BaseValue, TrialMoveValueProp), cardPlay);
        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, DynamicVars["SusPower"].BaseValue);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        if (ProgressReady(this) && cardPlay.Target?.GetPowerAmount<SusPower>() > 0m)
        {
            decimal allyBlock = DynamicVars["AllyBlock"].BaseValue;
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(allyBlock, TrialMoveValueProp), cardPlay);
            if (cardPlay.Target != Owner.Creature)
                await CreatureCmd.GainBlock(cardPlay.Target, new BlockVar(allyBlock, TrialMoveValueProp), cardPlay);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["AllyBlock"].UpgradeValueBy(1m);
    }
}