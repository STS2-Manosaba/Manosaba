using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Runs;
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
