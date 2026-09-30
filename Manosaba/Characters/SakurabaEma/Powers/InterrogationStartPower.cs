using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class InterrogationStartPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await EnsureProgressPower(new ThrowingPlayerChoiceContext(), cardSource);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = amount;
        _ = applier;

        if (power == this)
            await EnsureProgressPower(choiceContext, cardSource);
    }

    private async Task EnsureProgressPower(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        if (Owner.HasPower<InterrogationProgress1Power>() ||
            Owner.HasPower<InterrogationProgress2Power>() ||
            Owner.HasPower<InterrogationProgress3Power>() ||
            Owner.HasPower<InterrogationProgress4Power>())
        {
            return;
        }

        await CommonActions.Apply<InterrogationProgress1Power>(choiceContext, Owner, cardSource, InterrogationProgressPowerBase.InitialAmount);
    }
}
