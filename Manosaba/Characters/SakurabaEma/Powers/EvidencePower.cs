using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class EvidencePower : PathCustomPowerModel
{
    private const decimal RawTellOwkThreshold = 10m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;

        if (power != this || amount <= 0m || Amount < RawTellOwkThreshold || Amount - amount >= RawTellOwkThreshold)
            return;

        if (Owner.GetPowerAmount<InterrogationStartPower>() > 0m || Owner.Player == null || Owner.CombatState == null)
            return;

        Flash();
        CardModel rawTellOwk = Owner.CombatState.CreateCard<RawTellOwk>(Owner.Player);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(rawTellOwk, PileType.Hand, Owner.Player);
        CardCmd.PreviewCardPileAdd(result);
    }
}
