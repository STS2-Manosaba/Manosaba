using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.SawatariCoco.Powers;

/// <summary>直播模式：回合結束時對隨機敵人造成等同層數的傷害後，減少 1 層。</summary>
public sealed class LiveStreamModePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side || !creatures.Contains(Owner) || Amount <= 0m)
        {
            return;
        }

        decimal damage = Amount;
        if (Owner.CombatState is { } combatState
            && Owner.Player is { } player
            && player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies) is { } target)
        {
            await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered, Owner, null, null);
        }

        await CommonActions.Apply<LiveStreamModePower>(choiceContext, Owner, null, -1m);
    }
}
