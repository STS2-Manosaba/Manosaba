using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class WardenTestimony : TestimonyCardBase
{
    protected override IEnumerable<DynamicVar> TestimonyVars => [new PowerVar<SusPower>(1m)];

    protected override IEnumerable<IHoverTip> TestimonyExtraHoverTips => [HoverTipFactory.FromPower<SusPower>()];

    protected override async Task OnAfterTestimony(PlayerChoiceContext choiceContext) =>
        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, DynamicVars["SusPower"].BaseValue);
}
