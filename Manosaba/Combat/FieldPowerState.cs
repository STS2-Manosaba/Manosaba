using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Combat;

public static class FieldPowerState
{
    // Identity-scoped state: a fresh combat starts empty; this never keeps an old combat alive.
    private static readonly ConditionalWeakTable<ICombatState, List<FieldPowerModel>> Powers = new();

    public static IReadOnlyList<FieldPowerModel> Get(ICombatState field) =>
        Powers.TryGetValue(field, out var powers) ? powers : Array.Empty<FieldPowerModel>();

    public static bool Has<T>(ICombatState? field) where T : FieldPowerModel =>
        field != null && Get(field).OfType<T>().Any(power => power.Amount > 0);

    public static T Ensure<T>(ICombatState field) where T : FieldPowerModel, new()
    {
        var powers = Powers.GetValue(field, _ => []);
        var power = powers.OfType<T>().FirstOrDefault();
        if (power == null)
        {
            power = (T)ModelDb.Power<T>().ToMutable();
            power.BindToField(field);
            powers.Add(power);
        }
        return power;
    }

    public static T Apply<T>(ICombatState field, int amount) where T : FieldPowerModel, new()
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        var power = Ensure<T>(field);
        power.SetAmount(power.StackType == PowerStackType.Single ? 1 : checked(power.Amount + amount));
        return power;
    }
}
