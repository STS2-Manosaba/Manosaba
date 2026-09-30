using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class EnemyMajokaPower : PathCustomPowerModel
{
    private const decimal NarehateThreshold = 200m;
    private const decimal DamagePerStackDivisor = 20m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamagePerStackDivisor", DamagePerStackDivisor),
        new DynamicVar("NarehateThreshold", NarehateThreshold),
    ];

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _ = target;
        _ = amount;
        _ = cardSource;

        if (dealer != Owner || !props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
            return 0m;

        return Math.Floor(Amount / DamagePerStackDivisor);
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this || Amount < NarehateThreshold || Owner.GetPowerAmount<NarehatePower>() > 0m)
            return;

        await CommonActions.Apply<NarehatePower>(choiceContext, Owner, cardSource, 1m);
    }
}
