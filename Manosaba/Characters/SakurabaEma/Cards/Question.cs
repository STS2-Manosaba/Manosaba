using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using manosaba.Characters.SakurabaEma.Combat;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Question : EmaTrialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromKeyword(ManosabaKeywords.Testimony)];

    public Question() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (CombatState == null)
        {
            return;
        }

        await EmaTrialPresentation.Play(this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        CardModel testimony = TestimonyCardHelper.CreateRandom(CombatState, Owner);
        if (IsUpgraded) CardCmd.Upgrade(testimony);
        CardCmd.PreviewCardPileAdd(await EmaCardGeneration.Add(testimony, PileType.Hand, Owner));
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
