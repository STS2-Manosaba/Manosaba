using BaseLib.Utils;
using Manosaba.Characters.Common;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class PresentEvidence : EmaTrialCard
{
    public override string PortraitPath => "res://Manosaba/images/cards/search_sakuraba_ema.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ArgumentPower>(0m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips => [WitchEncyclopediaState.HoverTip, HoverTipFactory.FromPower<ArgumentPower>()];
    public PresentEvidence() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await WitchEncyclopediaState.Present(choiceContext, Owner, SelectionScreenPrompt);
        if (IsUpgraded)
            await CommonActions.Apply<ArgumentPower>(choiceContext, Owner.Creature, this, DynamicVars["ArgumentPower"].BaseValue);
    }
    protected override void OnUpgrade() => DynamicVars["ArgumentPower"].UpgradeValueBy(1m);
}
