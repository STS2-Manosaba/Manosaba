using Manosaba.Characters.KurobeNanoka.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Powers;

public sealed class CampingPower : PathCustomPowerModel
{
    private int _appliedRound;
    private bool _active;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => _active ? 1 : 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _appliedRound = Owner.CombatState?.RoundNumber ?? 0;
        _active = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        return _active && target == Owner ? 0m : 1m;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!_active || card.Owner != Owner.Player || card is not GunBase || card.Type != CardType.Attack)
            return false;

        modifiedCost = originalCost + 1m;
        return true;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side == CombatSide.Player && combatState.RoundNumber == _appliedRound + 1)
        {
            _active = true;
            InvokeDisplayAmountChanged();
        }
        else if (side == CombatSide.Player && combatState.RoundNumber > _appliedRound + 1)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side == CombatSide.Player && _active)
            await PowerCmd.Remove(this);
    }
}
