using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.SawatariCoco.Powers;

/// <summary>百寶袋／四次元ポケット：最多疊 2 層。差 1 件時補齊 1 件；疊到 2 層時可直接補齊差 2 件的系列並清空層數。多個系列可觸發時隨機挑選。</summary>
public sealed class FourDPocketPower : PathCustomPowerModel
{
    public const int MaxStacks = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power.Amount > MaxStacks)
        {
            power.SetAmount(MaxStacks);
        }

        // Gaining stacks (not spending) may already satisfy a near-complete set that was assembled before this power existed.
        if (amount > 0m)
        {
            await SawatariCocoEquipmentHelper.TryTriggerTreasureBagAsync(choiceContext, power.Owner);
        }
    }
}
