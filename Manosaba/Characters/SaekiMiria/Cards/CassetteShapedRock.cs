using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.SaekiMiria.Cards;

public sealed class CassetteShapedRock : MovieBase
{
    protected override IEnumerable<DynamicVar> MovieVars => [new DamageVar(10m, ValueProp.Move)];

    public CassetteShapedRock()
        : base(TargetType.AnyEnemy, CardType.Attack)
    {
    }

    protected override async Task OnMovieEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
    }
}
