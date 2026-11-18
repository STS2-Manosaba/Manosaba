using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Manosaba.Characters.JogasakiNoah.Cards
{
    internal static class OrbGapChannelHelper
    {
        private static readonly FieldInfo? OrbQueueOrbsField = AccessTools.Field(typeof(OrbQueue), "_orbs");
        private static readonly FieldInfo? OrbManagerOrbsField = AccessTools.Field(typeof(NOrbManager), "_orbs");
        private static readonly FieldInfo? OrbManagerCurTweenField = AccessTools.Field(typeof(NOrbManager), "_curTween");
        private static readonly MethodInfo? NOrbFlashMethod = AccessTools.Method(typeof(NOrb), "Flash");

        public static async Task ChannelAt(PlayerChoiceContext choiceContext, OrbModel orb, Player player, int insertIndex)
        {
            if (CombatManager.Instance.IsOverOrEnding)
            {
                return;
            }

            if (player.Creature?.CombatState is not { } combatState || player.PlayerCombatState == null)
            {
                return;
            }

            OrbQueue orbQueue = player.PlayerCombatState.OrbQueue;
            if (player.Character.BaseOrbSlotCount == 0 && orbQueue.Capacity == 0)
            {
                await OrbCmd.AddSlots(player, 1);
            }

            orb.AssertMutable();
            orb.Owner = player;

            bool wasFull = orbQueue.Orbs.Count >= orbQueue.Capacity;
            if (wasFull)
            {
                await EvokeNextWithoutVisualAnimation(choiceContext, player);
                if (insertIndex > 0)
                {
                    // We selected gap index against the pre-evoke queue.
                    // After front orb is evoked, indices shift left by one.
                    insertIndex--;
                }
            }

            await PlayPaletteGapInsertPreflight(orb, player, insertIndex);

            if (!await orbQueue.TryEnqueue(orb))
            {
                return;
            }

            insertIndex = Math.Clamp(insertIndex, 0, orbQueue.Orbs.Count - 1);
            MoveLastOrbToInsertIndex(orbQueue, insertIndex);

            CombatManager.Instance.History.OrbChanneled(combatState, orb);
            orb.PlayChannelSfx();
            if (!wasFull)
            {
                NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.AddOrbAnim();
            }
            SyncOrbVisualOrder(player);
            if (wasFull)
            {
                ApplyPartialInsertTween(player, insertIndex);
            }
            await Hook.AfterOrbChanneled(combatState, choiceContext, player, orb);
        }

        private static async Task PlayPaletteGapInsertPreflight(OrbModel orb, Player player, int insertIndex)
        {
            NCombatRoom? combatRoom = NCombatRoom.Instance;
            NCreature? creatureNode = combatRoom?.GetCreatureNode(player.Creature);
            NOrbManager? orbManager = creatureNode?.OrbManager;
            if (combatRoom == null || creatureNode == null || orbManager == null)
            {
                return;
            }

            if (OrbManagerOrbsField?.GetValue(orbManager) is not List<NOrb> orbSlots || orbSlots.Count == 0)
            {
                return;
            }

            int queueCount = player.PlayerCombatState?.OrbQueue?.Orbs?.Count ?? 0;
            int maxInsertIndex = Math.Min(queueCount, Math.Max(0, orbSlots.Count - 1));
            if (maxInsertIndex < 0)
            {
                return;
            }

            int clampedInsertIndex = Math.Clamp(insertIndex, 0, maxInsertIndex);
            Vector2 from = GetCreatureCenter(creatureNode);
            Vector2 to = GetGapPosition(orbSlots, maxInsertIndex, clampedInsertIndex);

            Node2D flyNode = orb.CreateSprite();
            flyNode.TopLevel = true;
            flyNode.GlobalPosition = from;
            flyNode.ZIndex = 800;
            combatRoom.AddChild(flyNode);

            Tween tween = combatRoom.CreateTween().SetParallel(true);
            tween
                .TweenProperty(flyNode, "global_position", to, 0.24f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            tween
                .TweenProperty(flyNode, "scale", Vector2.One * 0.9f, 0.24f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            tween
                .TweenProperty(flyNode, "modulate:a", 0.2f, 0.24f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);

            TaskCompletionSource<bool> completion = new();
            void OnFinished() => completion.TrySetResult(true);
            tween.Finished += OnFinished;
            try
            {
                await completion.Task;
            }
            finally
            {
                tween.Finished -= OnFinished;
                flyNode.QueueFree();
            }
        }

        private static void MoveLastOrbToInsertIndex(OrbQueue queue, int insertIndex)
        {
            if (OrbQueueOrbsField?.GetValue(queue) is not List<OrbModel> mutableOrbs || mutableOrbs.Count <= 1)
            {
                return;
            }

            int lastIndex = mutableOrbs.Count - 1;
            if (insertIndex >= lastIndex)
            {
                return;
            }

            OrbModel insertedOrb = mutableOrbs[lastIndex];
            mutableOrbs.RemoveAt(lastIndex);
            mutableOrbs.Insert(insertIndex, insertedOrb);
        }

        private static void SyncOrbVisualOrder(Player player)
        {
            if (player.Creature == null)
            {
                return;
            }

            NOrbManager? orbManager = NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager;
            if (orbManager == null)
            {
                return;
            }

            if (OrbManagerOrbsField?.GetValue(orbManager) is not List<NOrb> orbNodes)
            {
                return;
            }

            IReadOnlyList<OrbModel> queueOrbs = player.PlayerCombatState?.OrbQueue?.Orbs ?? [];
            int count = Math.Min(queueOrbs.Count, orbNodes.Count);
            for (int i = 0; i < count; i++)
            {
                OrbModel desired = queueOrbs[i];
                NOrb node = orbNodes[i];
                if (node.Model != desired)
                {
                    RebindOrbTriggeredHandler(node, node.Model, desired);
                    node.ReplaceOrb(desired);
                }
            }
        }

        private static void RebindOrbTriggeredHandler(NOrb node, OrbModel? oldModel, OrbModel newModel)
        {
            if (NOrbFlashMethod == null)
            {
                return;
            }

            if (Delegate.CreateDelegate(typeof(Action), node, NOrbFlashMethod, throwOnBindFailure: false) is not Action flashHandler)
            {
                return;
            }

            if (oldModel != null)
            {
                oldModel.PassiveActivated -= flashHandler;
            }

            newModel.PassiveActivated -= flashHandler;
            newModel.PassiveActivated += flashHandler;
        }

        private static Vector2 GetGapPosition(IReadOnlyList<NOrb> orbSlots, int maxInsertIndex, int insertIndex)
        {
            if (maxInsertIndex == 0)
            {
                return GetOrbSlotCenter(orbSlots[0]);
            }

            if (maxInsertIndex == 1)
            {
                Vector2 first = GetOrbSlotCenter(orbSlots[0]);
                Vector2 second = GetOrbSlotCenter(orbSlots[1]);
                Vector2 dir = (first - second).Normalized();
                return insertIndex == 0 ? first + dir * 28f : first - dir * 28f;
            }

            if (insertIndex == 0)
            {
                Vector2 first = GetOrbSlotCenter(orbSlots[0]);
                Vector2 second = GetOrbSlotCenter(orbSlots[1]);
                return first + (first - second).Normalized() * 28f;
            }

            if (insertIndex == maxInsertIndex)
            {
                Vector2 last = GetOrbSlotCenter(orbSlots[maxInsertIndex - 1]);
                Vector2 prev = GetOrbSlotCenter(orbSlots[Math.Max(0, maxInsertIndex - 2)]);
                return last + (last - prev).Normalized() * 28f;
            }

            Vector2 left = GetOrbSlotCenter(orbSlots[insertIndex - 1]);
            Vector2 right = GetOrbSlotCenter(orbSlots[insertIndex]);
            return (left + right) * 0.5f;
        }

        private static Vector2 GetOrbSlotCenter(Control slot)
        {
            return slot.GlobalPosition + slot.Size * 0.5f;
        }

        private static Vector2 GetCreatureCenter(NCreature creatureNode)
        {
            return creatureNode switch
            {
                Control control => control.GlobalPosition + control.Size * 0.5f,
                _ => Vector2.Zero
            };
        }

        private static void ApplyPartialInsertTween(Player player, int insertIndex)
        {
            NOrbManager? orbManager = NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager;
            if (orbManager == null || OrbManagerOrbsField?.GetValue(orbManager) is not List<NOrb> orbNodes || orbNodes.Count == 0)
            {
                return;
            }

            int capacity = player.PlayerCombatState?.OrbQueue?.Capacity ?? 0;
            if (capacity <= 1)
            {
                return;
            }

            int count = Math.Min(capacity, orbNodes.Count);
            int animatedCount = Math.Clamp(insertIndex, 0, count);

            (OrbManagerCurTweenField?.GetValue(orbManager) as Tween)?.Kill();
            OrbManagerCurTweenField?.SetValue(orbManager, null);

            Tween? partialTween = null;
            for (int i = 0; i < count; i++)
            {
                Vector2 target = GetOrbSlotPosition(i, capacity, orbManager.IsLocal);
                if (i < animatedCount)
                {
                    partialTween ??= orbManager.CreateTween().SetParallel();
                    partialTween
                        .TweenProperty(orbNodes[i], "position", target, 0.45f)
                        .SetEase(Tween.EaseType.InOut)
                        .SetTrans(Tween.TransitionType.Sine);
                }
                else
                {
                    orbNodes[i].Position = target;
                }
            }

            if (partialTween != null)
            {
                OrbManagerCurTweenField?.SetValue(orbManager, partialTween);
            }
        }

        private static Vector2 GetOrbSlotPosition(int index, int capacity, bool isLocal)
        {
            float angle = 125f;
            float step = angle / (capacity - 1f);
            angle -= step * index;

            float radius = Mathf.Lerp(225f, 300f, (capacity - 3f) / 7f);
            if (!isLocal)
            {
                radius *= 0.75f;
            }

            float radians = Mathf.DegToRad(-25f - angle);
            return new Vector2(-Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        private static async Task EvokeNextWithoutVisualAnimation(PlayerChoiceContext choiceContext, Player player)
        {
            if (player.Creature?.CombatState == null || player.PlayerCombatState == null)
            {
                return;
            }

            OrbQueue orbQueue = player.PlayerCombatState.OrbQueue;
            if (orbQueue.Orbs.Count == 0)
            {
                return;
            }

            OrbModel evokedOrb = orbQueue.Orbs[0];
            choiceContext.PushModel(evokedOrb);

            bool removed = orbQueue.Remove(evokedOrb);
            IEnumerable<Creature> targets = await evokedOrb.Evoke(choiceContext);

            choiceContext.PopModel(evokedOrb);
            if (player.Creature.CombatState != null)
            {
                await Hook.AfterOrbEvoked(choiceContext, player.Creature.CombatState, evokedOrb, targets);
                if (removed)
                {
                    evokedOrb.RemoveInternal();
                }
            }
        }
    }
}
