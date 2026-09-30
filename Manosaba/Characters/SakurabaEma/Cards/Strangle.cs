using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Strangle : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<StranglePower>(2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StranglePower>()];

    public Strangle() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (cardPlay.Target.IsAlive)
        {
            await CommonActions.Apply<StranglePower>(choiceContext, cardPlay.Target, this, DynamicVars["StranglePower"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["StranglePower"].UpgradeValueBy(1m);
    }
}
