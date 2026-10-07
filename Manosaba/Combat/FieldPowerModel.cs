using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Combat;

/// <summary>A power owned by a combat, never added to a creature's Powers collection.</summary>
public abstract class FieldPowerModel : PathCustomPowerModel
{
    public ICombatState Field { get; private set; } = null!;
    public virtual bool ShowAtZero => false;

    internal void BindToField(ICombatState field) => Field = field;
    internal void NotifyAmountChanged() => InvokeDisplayAmountChanged();

    // Creature power commands and smart HoverTips assume Owner exists. Field powers use
    // FieldPowerState and their own field tooltip instead of those APIs.
    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource) => Task.CompletedTask;
}
