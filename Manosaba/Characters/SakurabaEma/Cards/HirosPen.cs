using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using Manosaba.Characters.Common.Overrides;
using manosaba.Characters.SakurabaEma.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class HirosPen : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Evidence, CardKeyword.Eternal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new PowerVar<VigorPower>(3m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromKeyword(ManosabaKeywords.Evidence),
        WitchEncyclopediaState.HoverTip,
        HoverTipFactory.FromKeyword(CardKeyword.Eternal),
    ];

    public HirosPen() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self, shouldShowInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await CommonActions.Apply<VigorPower>(choiceContext, Owner.Creature, this, DynamicVars[nameof(VigorPower)].BaseValue);
    }

    protected override PileType GetResultPileTypeForCardPlay() => WitchEncyclopediaState.PileType;

    protected override void OnUpgrade() => DynamicVars[nameof(VigorPower)].UpgradeValueBy(2m);
}
