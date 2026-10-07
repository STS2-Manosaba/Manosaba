using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(CommonCardPool))]
public sealed class GuiltQuestion : EmaTrialCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromCard<GuiltSuspicion>()];
    public override string PortraitPath => ModelDb.Card<FaceInCourt>().PortraitPath;

    public GuiltQuestion() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target?.Player is not { } teammate || CombatState is not { } combat)
            return;

        await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<GuiltSuspicion>(teammate), PileType.Hand, teammate);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
