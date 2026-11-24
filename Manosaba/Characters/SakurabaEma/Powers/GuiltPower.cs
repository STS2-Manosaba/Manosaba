using Manosaba.Extensions;
using Manosaba.Characters.Common.Overrides;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class GuiltPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner.Creature != Owner || play.Card.Type != CardType.Attack ||
            !play.Card.Keywords.Contains(ManosabaKeywords.Trial)) return;

        Flash();
        if (Amount <= 1) await PowerCmd.Remove(this);
        else await PowerCmd.ModifyAmount(context, this, Amount / 2 - Amount, Owner, play.Card);
    }
}
