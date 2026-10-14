using BaseLib.Utils;
using Manosaba.Extensions;
using Manosaba.Characters.Common.Overrides;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class LaVIBavellabion : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<LawDevilPower>(), HoverTipFactory.FromKeyword(ManosabaKeywords.Unique)];

    public LaVIBavellabion() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CommonActions.Apply<LawDevilPower>(choiceContext, Owner.Creature, this, 1m);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
