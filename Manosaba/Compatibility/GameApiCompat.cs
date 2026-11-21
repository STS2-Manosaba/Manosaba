using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Concurrent;
using System.Reflection;

namespace Manosaba.Compatibility;

/// <summary>Reflection adapters for game APIs that moved or were renamed between main and beta.</summary>
internal static class GameApiCompat
{
    private static readonly PropertyInfo? CardSelectorStackProperty =
        AccessTools.Property(typeof(RunManager), "CardSelectorStack");
    private static readonly MethodInfo? OldPushSelector =
        AccessTools.Method(typeof(CardSelectCmd), "PushSelector");
    private static readonly MethodInfo? ScaleOstyMethod =
        AccessTools.Method(typeof(NCreature), "ScaleAndPositionOsty", [typeof(float), typeof(double)])
        ?? AccessTools.Method(typeof(NCreature), "OstyScaleToSize", [typeof(float), typeof(double)]);
    private static readonly MethodInfo? SetInteractableMethod =
        AccessTools.Method(typeof(NCreature), "SetIsInteractable", [typeof(bool), typeof(bool)])
        ?? AccessTools.Method(typeof(NCreature), "ToggleIsInteractable", [typeof(bool)]);

    internal static IDisposable PushSelector(Player player, ICardSelector selector)
    {
        object? stack = CardSelectorStackProperty?.GetValue(RunManager.Instance);
        MethodInfo? push = stack == null ? null : AccessTools.Method(stack.GetType(), "PushSelector");
        if (push != null)
        {
            return (IDisposable)push.Invoke(stack, [player, selector])!;
        }

        if (OldPushSelector == null)
        {
            throw new MissingMethodException(typeof(CardSelectCmd).FullName, "PushSelector");
        }

        object?[] args = OldPushSelector.GetParameters().Length == 1
            ? [selector]
            : [selector, false];
        return (IDisposable)OldPushSelector.Invoke(null, args)!;
    }

    internal static void ScaleOstyToSize(NCreature creatureNode, float hp, double duration)
    {
        if (ScaleOstyMethod == null)
        {
            throw new MissingMethodException(typeof(NCreature).FullName, "ScaleAndPositionOsty/OstyScaleToSize");
        }

        ScaleOstyMethod.Invoke(creatureNode, [hp, duration]);
    }

    private static readonly ConcurrentDictionary<string, MethodBase> OptionalTailCache = new();

    /// <summary>
    /// Finds an overload/ctor whose leading parameters exactly match <paramref name="leading"/> and whose remaining
    /// parameters are all optional (beta appends defaulted params such as <c>forceIfDead</c> or <c>selectorStack</c>).
    /// </summary>
    private static MethodBase ResolveOptionalTail(Type type, string? methodName, Type[] leading)
    {
        string key = $"{type.FullName}::{methodName ?? ".ctor"}({string.Join(",", leading.Select(t => t.FullName))})";
        return OptionalTailCache.GetOrAdd(key, _ =>
        {
            IEnumerable<MethodBase> candidates = methodName == null
                ? type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                : type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .Where(m => m.Name == methodName);
            MethodBase? best = candidates
                .Where(m =>
                {
                    ParameterInfo[] ps = m.GetParameters();
                    return ps.Length >= leading.Length
                        && ps.Take(leading.Length).Select(p => p.ParameterType).SequenceEqual(leading)
                        && ps.Skip(leading.Length).All(p => p.IsOptional);
                })
                .OrderBy(m => m.GetParameters().Length)
                .FirstOrDefault();
            return best ?? throw new MissingMethodException(type.FullName, methodName ?? ".ctor");
        });
    }

    private static object?[] FillOptionalTail(MethodBase method, object?[] args)
    {
        ParameterInfo[] ps = method.GetParameters();
        if (ps.Length == args.Length)
        {
            return args;
        }

        object?[] full = new object?[ps.Length];
        Array.Copy(args, full, args.Length);
        for (int i = args.Length; i < ps.Length; i++)
        {
            full[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : Type.Missing;
        }

        return full;
    }

    private static object? InvokeOptionalTail(Type type, string methodName, Type[] leading, object? instance, object?[] args)
    {
        MethodBase method = ResolveOptionalTail(type, methodName, leading);
        return method.Invoke(instance, BindingFlags.DoNotWrapExceptions, null, FillOptionalTail(method, args), null);
    }

    private static T ConstructOptionalTail<T>(Type[] leading, object?[] args)
    {
        var ctor = (ConstructorInfo)ResolveOptionalTail(typeof(T), null, leading);
        return (T)ctor.Invoke(BindingFlags.DoNotWrapExceptions, null, FillOptionalTail(ctor, args), null);
    }

    /// <summary>main: <c>PlayOnCreature(Creature, string)</c>; beta adds <c>bool forceIfDead = false</c>.</summary>
    internal static void PlayVfxOnCreature(Creature target, string path) =>
        InvokeOptionalTail(typeof(VfxCmd), nameof(VfxCmd.PlayOnCreature), [typeof(Creature), typeof(string)], null, [target, path]);

    /// <summary>main: <c>PlayOnCreatureCenter(Creature, string)</c>; beta adds <c>bool forceIfDead = false</c>.</summary>
    internal static void PlayVfxOnCreatureCenter(Creature target, string path) =>
        InvokeOptionalTail(typeof(VfxCmd), nameof(VfxCmd.PlayOnCreatureCenter), [typeof(Creature), typeof(string)], null, [target, path]);

    /// <summary>
    /// Multi-target damage with card source. main: 7 params; beta adds <c>ModifyDamageHookType = All</c>.
    /// </summary>
    internal static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature>? targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        (Task<IEnumerable<DamageResult>>)InvokeOptionalTail(
            typeof(CreatureCmd),
            nameof(CreatureCmd.Damage),
            [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)],
            null,
            [choiceContext, targets, amount, props, dealer, cardSource, cardPlay])!;

    /// <summary>beta adds <c>CardSelectorStack? selectorStack = null</c>.</summary>
    internal static CardReward NewCardReward(
        IEnumerable<CardModel> cardsToOffer,
        CardCreationSource source,
        Player player,
        CardCreationOptions rerollOptions) =>
        ConstructOptionalTail<CardReward>(
            [typeof(IEnumerable<CardModel>), typeof(CardCreationSource), typeof(Player), typeof(CardCreationOptions)],
            [cardsToOffer, source, player, rerollOptions]);

    /// <summary>beta adds <c>Rng? rngOverride = null</c>.</summary>
    internal static RelicReward NewRelicReward(RelicModel relic, Player player) =>
        ConstructOptionalTail<RelicReward>([typeof(RelicModel), typeof(Player)], [relic, player]);

    internal static void SetCreatureInteractable(NCreature creatureNode, bool interactable)
    {
        if (SetInteractableMethod == null)
        {
            throw new MissingMethodException(typeof(NCreature).FullName, "SetIsInteractable/ToggleIsInteractable");
        }

        object?[] args = SetInteractableMethod.GetParameters().Length == 1
            ? [interactable]
            : [interactable, true];
        SetInteractableMethod.Invoke(creatureNode, args);
    }
}
