using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class LawDevilPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        Flash();
        await CommonActions.Apply<GuiltPower>(choiceContext, Owner, null, 1m);
        await CommonActions.Apply<ArgumentPower>(choiceContext, Owner, null, 1m);
    }
}
