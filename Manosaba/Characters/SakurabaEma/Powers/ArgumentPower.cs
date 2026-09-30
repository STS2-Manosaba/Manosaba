using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class ArgumentPower : PathCustomPowerModel
{
    private const decimal EnergyThreshold = 10m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;

        if (player != Owner.Player || Amount < EnergyThreshold)
            return;

        Flash();
        await PlayerCmd.GainEnergy(1m, player);
    }
}
