using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class SillyLittleDog : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Innate] : [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<KiangPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<KiangPower>()];

    public SillyLittleDog() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CommonActions.Apply<KiangPower>(choiceContext, Owner.Creature, this, DynamicVars["KiangPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
    }
}
