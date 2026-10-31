using BaseLib.Utils;
using Manosaba.Characters.Common.Monsters;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class ActingCoolPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || Amount <= 0m)
            return;

        Player? ownerPlayer = Owner.Player;
        Creature? warden = ownerPlayer?.PlayerCombatState?.GetPet<Jailer>()
            ?? Owner.CombatState?.Allies.FirstOrDefault(c => c.Monster is Jailer && c.PetOwner == ownerPlayer);
        if (warden is not { IsAlive: true })
            return;

        await CommonActions.Apply<AccuratePower>(context, Owner, null, Amount);
    }
}
