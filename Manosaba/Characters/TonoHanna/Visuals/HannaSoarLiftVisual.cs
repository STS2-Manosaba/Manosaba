using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Manosaba.Characters.TonoHanna.Visuals;

/// <summary>Lifts character art when they receive Hanna's soar-like buff from a same-side source (self or ally), e.g. <see cref="Cards.FlyingBroom"/>.</summary>
internal static class HannaSoarLiftVisual
{
    private const float BaseLiftPixels = 72f;
    public const float FullLiftPixels = BaseLiftPixels * 3f;
    public const float HalfLiftPixels = FullLiftPixels / 2f;
    private const float DurationSeconds = 0.28f;

    private static readonly Dictionary<Creature, LiftState> s_liftByCreature = new();
    private static readonly object s_lock = new();

    public static bool TryGetLiftedY(Creature creature, out float liftedY)
    {
        lock (s_lock)
        {
            if (s_liftByCreature.TryGetValue(creature, out LiftState? state))
            {
                liftedY = state.BaseY - state.CurrentLiftPixels;
                return true;
            }
        }

        liftedY = 0f;
        return false;
    }

    public static async Task RegisterLiftAsync(Creature owner, Creature? applier, PowerModel source, float liftPixels)
    {
        if (applier == null || applier.Side != owner.Side)
            return;

        NCombatRoom? room = NCombatRoom.Instance;
        NCreature? node = room?.GetCreatureNode(owner);
        Node2D? visuals = node?.Visuals;
        if (room == null || visuals == null)
            return;

        float targetY;
        lock (s_lock)
        {
            if (!s_liftByCreature.TryGetValue(owner, out LiftState? state))
            {
                state = new LiftState(visuals.Position.Y);
                s_liftByCreature[owner] = state;
            }

            state.LiftPixelsBySource[source] = liftPixels;
            targetY = state.BaseY - state.CurrentLiftPixels;
        }

        Tween tween = room.CreateTween();
        tween.TweenProperty(visuals, "position:y", targetY, DurationSeconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        await room.ToSignal(tween, Tween.SignalName.Finished);
    }

    public static async Task RemoveLiftAsync(Creature oldOwner, PowerModel source)
    {
        float targetY;
        lock (s_lock)
        {
            if (!s_liftByCreature.TryGetValue(oldOwner, out LiftState? state)
                || !state.LiftPixelsBySource.Remove(source))
            {
                return;
            }

            targetY = state.BaseY - state.CurrentLiftPixels;
            if (state.LiftPixelsBySource.Count == 0)
                s_liftByCreature.Remove(oldOwner);
        }

        NCombatRoom? room = NCombatRoom.Instance;
        NCreature? creatureNode = room?.GetCreatureNode(oldOwner);
        Node2D? visuals = creatureNode?.Visuals;
        if (room == null || visuals == null)
            return;

        Tween tween = room.CreateTween();
        tween.TweenProperty(visuals, "position:y", targetY, DurationSeconds * 0.9f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        await room.ToSignal(tween, Tween.SignalName.Finished);
    }

    private sealed class LiftState(float baseY)
    {
        public float BaseY { get; } = baseY;

        public Dictionary<PowerModel, float> LiftPixelsBySource { get; } = new();

        public float CurrentLiftPixels => LiftPixelsBySource.Count == 0 ? 0f : LiftPixelsBySource.Values.Max();
    }
}
