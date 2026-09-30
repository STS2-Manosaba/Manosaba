using BaseLib.Utils;
using Manosaba.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class GuardTestimony : TestimonyCardBase
{
    protected override IEnumerable<DynamicVar> TestimonyVars => [new PowerVar<StrengthPower>(2m)];

    protected override IEnumerable<IHoverTip> TestimonyExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    protected override async Task OnAfterTestimony(PlayerChoiceContext choiceContext) =>
        await CommonActions.Apply<StrengthPower>(choiceContext, Owner.Creature, this, DynamicVars["StrengthPower"].BaseValue);
}
