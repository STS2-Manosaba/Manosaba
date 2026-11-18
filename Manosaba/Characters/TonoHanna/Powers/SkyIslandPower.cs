using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Powers;

/// <summary>Incoming damage reduction for any damage type (same hook shape as <see cref="Manosaba.Characters.JogasakiNoah.Powers.LiquidManipulationPower"/>). Stacks decay by 1 when your side starts a turn.</summary>
public sealed class SkyIslandPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => false;

    public override LocString Title => new LocString("powers", Id.Entry + ".title");

    public override LocString Description => new LocString("powers", Id.Entry + ".description");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageDecrease", 50m)];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        if (target != Owner || Amount <= 0m)
        {
            return 1m;
        }

        return DynamicVars["DamageDecrease"].BaseValue / 100m;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        _ = combatState;
        if (side != Owner.Side || Amount <= 0m)
        {
            return;
        }

        Flash();
        await CommonActions.Apply<SkyIslandPower>(new ThrowingPlayerChoiceContext(), Owner, null, -1m, silent: true);
    }
}
