using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class TurnADeafEar : EmaTrialCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<WeakPower>(2m),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    public TurnADeafEar() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(DynamicVars.Block.BaseValue, TrialMoveValueProp), cardPlay);

        if (cardPlay.Target != null)
        {
            await CommonActions.Apply<WeakPower>(choiceContext, cardPlay.Target, this, DynamicVars["WeakPower"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }
}
