using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class RawTellOwk : PathCustomCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<InterrogationStartPower>()];

    public RawTellOwk() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CommonActions.Apply<InterrogationStartPower>(choiceContext, Owner.Creature, this, 1m);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
