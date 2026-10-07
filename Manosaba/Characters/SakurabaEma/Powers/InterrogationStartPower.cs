using BaseLib.Utils;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class InterrogationStartPower : FieldPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuiltPower>()];

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        // Trial attacks retain Unpowered to exclude Strength/Majoka, but this field
        // explicitly allows their source's Guilt. Never add it to non-attack damage.
        bool trialAttack = props.HasFlag(ValueProp.Move) && cardSource?.Type == CardType.Attack &&
            cardSource.Keywords.Contains(ManosabaKeywords.Trial);
        return dealer != null && (props.IsPoweredAttack() || trialAttack)
            ? dealer.GetPowerAmount<GuiltPower>()
            : 0m;
    }

    public static Task ApplyToField(PlayerChoiceContext choiceContext, Creature applier, CardModel? cardSource)
    {
        var field = applier.CombatState
            ?? throw new InvalidOperationException("Interrogation Start requires an active combat.");
        FieldPowerState.Apply<InterrogationStartPower>(field, 1);

        FieldPowerState.Ensure<InterrogationProgressPower>(field);
        return Task.CompletedTask;
    }
}
