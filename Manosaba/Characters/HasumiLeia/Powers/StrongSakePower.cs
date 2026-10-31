using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.HasumiLeia.Powers;

public sealed class StrongSakePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        _ = context;

        if (Owner.Player == null || cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        if (!cardPlay.Card.CanonicalKeywords.Contains(ManosabaKeywords.SwordTechnique))
        {
            return;
        }

        Flash();
await CommonActions.Apply<EnergyNextTurnPower>(context, Owner, cardPlay.Card, Amount);
    }
}
