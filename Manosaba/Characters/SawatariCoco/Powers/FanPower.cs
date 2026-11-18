using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.SawatariCoco.Powers;

/// <summary>粉絲：對玩家造成的傷害減少 1。</summary>
public sealed class FanPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        _ = amount;
        _ = props;
        _ = cardSource;

        if (dealer != Owner || target is not { IsPlayer: true })
        {
            return 0m;
        }

        if (Applier is not { } fanOwner || target != fanOwner)
        {
            return 0m;
        }

        return -1m;
    }
}
