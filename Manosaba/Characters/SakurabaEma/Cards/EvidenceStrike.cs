using BaseLib.Utils;
using manosaba.Characters.SakurabaEma.Combat;
using Manosaba.Extensions;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class EvidenceStrike : EmaTrialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new TrialDamageVar(3m),
        new CardsVar(1),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [HoverTipFactory.FromCard<LivingPage>(), HoverTipFactory.FromCard<PresentEvidence>(), WitchEncyclopediaState.HoverTip];

    public EvidenceStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null || CombatState == null)
        {
            return;
        }

        await WitchEncyclopediaState.Present(choiceContext, Owner, SelectionScreenPrompt);
        if (cardPlay.Target.IsAlive)
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage.BaseValue, TrialMoveValueProp, Owner.Creature, this);

        List<CardModel> cards = [];
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            cards.Add(CombatState.CreateCard<LivingPage>(Owner));
        }

        IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(results);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
