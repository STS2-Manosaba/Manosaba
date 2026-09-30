using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Powers;

public sealed class WitchLanguageMasteryPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => ModelDb.Power<GuiltPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0m || Owner.CombatState is not { } combat) return;
        Flash();
        decimal loss = -delta * Amount;
        if (loss <= 0m) return;
        foreach (Creature enemy in combat.GetOpponentsOf(Owner).Where(c => c.IsAlive).ToList())
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), enemy, loss, DamageProps.nonCardHpLoss, null, null);
    }
}
