using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class BurningWoundPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || cardSource is not GunBase || result.UnblockedDamage <= 0m || !props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return;

        decimal burn = Math.Floor(result.UnblockedDamage * 0.3m);
        if (burn > 0m)
            await CommonActions.Apply<BurnPower>(context, target, cardSource, burn);
    }
}
