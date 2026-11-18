using Godot;
using HarmonyLib;
using manosaba.Characters.HikamiMeruru;
using Manosaba.Characters.HikamiMeruru.PotionCraft;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(NPotionHolder), "OnFocus")]
public static class Patch_NPotionHolder_HikamiPotionCraft_OnFocus
{
    private static void Postfix(NPotionHolder __instance)
    {
        PotionCraftTargetingService.OnPotionHolderFocused(__instance);
    }
}

[HarmonyPatch(typeof(NPotionHolder), "OnUnfocus")]
public static class Patch_NPotionHolder_HikamiPotionCraft_OnUnfocus
{
    private static void Postfix(NPotionHolder __instance)
    {
        PotionCraftTargetingService.OnPotionHolderUnfocused(__instance);
    }
}

[HarmonyPatch(typeof(NPotionHolder), "OnPress")]
public static class Patch_NPotionHolder_HikamiPotionCraft_OnPress
{
    private static bool Prefix()
    {
        return !PotionCraftTargetingService.IsTargeting;
    }

    private static void Postfix(NPotionHolder __instance)
    {
        PotionCraftTargetingService.BeginLongPress(__instance);
    }
}

[HarmonyPatch(typeof(NPotionHolder), "OnRelease")]
public static class Patch_NPotionHolder_HikamiPotionCraft_OnRelease
{
    private static bool Prefix(NPotionHolder __instance)
    {
        return !PotionCraftTargetingService.OnPotionHolderReleased(__instance);
    }
}

public static class PotionCraftTargetingService
{
    private const int LongPressDelayMs = 350;

    private static NPotionHolder? _sourceHolder;
    private static NPotionHolder? _hoveredHolder;
    private static NPotionHolder? _pendingHolder;
    private static CancellationTokenSource? _pendingLongPress;
    private static bool _longPressConsumed;

    public static bool IsTargeting => _sourceHolder != null;

    public static void BeginLongPress(NPotionHolder sourceHolder)
    {
        if (IsTargeting)
        {
            return;
        }

        CancelPendingLongPress();

        if (!CanStartCraft(sourceHolder))
        {
            return;
        }

        _pendingHolder = sourceHolder;
        _longPressConsumed = false;
        _pendingLongPress = new CancellationTokenSource();
        TaskHelper.RunSafely(StartTargetingAfterLongPress(sourceHolder, _pendingLongPress.Token));
    }

    public static bool OnPotionHolderReleased(NPotionHolder holder)
    {
        if (IsTargeting)
        {
            return true;
        }

        bool consumed = _pendingHolder == holder && _longPressConsumed;
        if (_pendingHolder == holder)
        {
            CancelPendingLongPress();
        }

        return consumed;
    }

    public static bool CanStartCraft(NPotionHolder sourceHolder)
    {
        PotionModel? sourcePotion = sourceHolder.Potion?.Model;
        Player? owner = sourcePotion?.Owner;
        if (sourcePotion == null || owner == null)
        {
            return false;
        }

        if (owner.Character is not HikamiMeruru || !LocalContext.IsMe(owner))
        {
            return false;
        }

        if (sourcePotion.IsQueued || owner.Creature.IsDead || !owner.CanUseOrRemovePotions)
        {
            return false;
        }

        if (CombatManager.Instance.IsInProgress &&
            (owner.Creature.CombatState?.CurrentSide != owner.Creature.Side || CombatManager.Instance.PlayerActionsDisabled))
        {
            return false;
        }

        return owner.PotionSlots
            .Where(potion => potion != null && potion != sourcePotion && !potion.IsQueued)
            .Any(potion => PotionCraftService.FindRecipeForPair(sourcePotion, potion) != null);
    }

    public static void OnPotionHolderFocused(NPotionHolder holder)
    {
        if (!IsTargeting || !IsValidTargetHolder(holder))
        {
            return;
        }

        _hoveredHolder = holder;
        NTargetManager.Instance.OnNodeHovered(holder);
    }

    public static void OnPotionHolderUnfocused(NPotionHolder holder)
    {
        if (!IsTargeting || _hoveredHolder != holder)
        {
            return;
        }

        NTargetManager.Instance.OnNodeUnhovered(holder);
        _hoveredHolder = null;
    }

    private static async Task StartTargetingAfterLongPress(NPotionHolder sourceHolder, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(LongPressDelayMs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested || _pendingHolder != sourceHolder || !CanStartCraft(sourceHolder))
        {
            return;
        }

        _longPressConsumed = true;
        await StartTargeting(sourceHolder);
    }

    private static async Task StartTargeting(NPotionHolder sourceHolder)
    {
        NTargetManager targetManager = NTargetManager.Instance;
        if (targetManager.IsInSelection)
        {
            targetManager.CancelTargeting();
        }

        _sourceHolder = sourceHolder;
        _pendingHolder = null;
        _pendingLongPress?.Dispose();
        _pendingLongPress = null;
        RunManager.Instance.HoveredModelTracker.OnLocalPotionSelected(sourceHolder.Potion!.Model);

        Vector2 startPosition = sourceHolder.GlobalPosition + sourceHolder.Size * 0.5f;
        targetManager.StartTargeting(
            TargetType.AnyPlayer,
            startPosition,
            TargetMode.ReleaseMouseToTarget,
            ShouldCancelTargeting,
            IsValidTargetNode);

        try
        {
            Node? selectedNode = await targetManager.SelectionFinished();
            if (selectedNode is NPotionHolder targetHolder)
            {
                await Craft(sourceHolder, targetHolder);
            }
        }
        finally
        {
            RunManager.Instance.HoveredModelTracker.OnLocalPotionDeselected();
            _sourceHolder = null;
            _hoveredHolder = null;
            _longPressConsumed = false;
            sourceHolder.TryGrabFocus();
        }
    }

    private static bool IsValidTargetNode(Node node)
    {
        return node is NPotionHolder holder && IsValidTargetHolder(holder);
    }

    private static bool IsValidTargetHolder(NPotionHolder targetHolder)
    {
        if (_sourceHolder == null || targetHolder == _sourceHolder)
        {
            return false;
        }

        PotionModel? sourcePotion = _sourceHolder.Potion?.Model;
        PotionModel? targetPotion = targetHolder.Potion?.Model;
        if (sourcePotion == null || targetPotion == null || sourcePotion.Owner != targetPotion.Owner)
        {
            return false;
        }

        if (sourcePotion.IsQueued || targetPotion.IsQueued || !sourcePotion.Owner.CanUseOrRemovePotions)
        {
            return false;
        }

        return PotionCraftService.FindRecipeForPair(sourcePotion, targetPotion) != null;
    }

    private static async Task Craft(NPotionHolder sourceHolder, NPotionHolder targetHolder)
    {
        PotionModel? sourcePotion = sourceHolder.Potion?.Model;
        PotionModel? targetPotion = targetHolder.Potion?.Model;
        Player? owner = sourcePotion?.Owner;
        if (sourcePotion == null || targetPotion == null || owner == null)
        {
            return;
        }

        int sourceSlotIndex = owner.PotionSlots.IndexOf(sourcePotion);
        int targetSlotIndex = owner.PotionSlots.IndexOf(targetPotion);
        if (sourceSlotIndex < 0 || targetSlotIndex < 0)
        {
            return;
        }

        sourceHolder.DisableUntilPotionRemoved();
        targetHolder.DisableUntilPotionRemoved();
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
            new PotionCraftGameAction(
                owner,
                (uint)sourceSlotIndex,
                (uint)targetSlotIndex,
                CombatManager.Instance.IsInProgress));

        await Task.CompletedTask;
    }

    private static bool ShouldCancelTargeting()
    {
        return _sourceHolder?.Potion?.Model == null || !CanStartCraft(_sourceHolder);
    }

    private static void CancelPendingLongPress()
    {
        _pendingLongPress?.Cancel();
        _pendingLongPress?.Dispose();
        _pendingLongPress = null;
        _pendingHolder = null;
        _longPressConsumed = false;
    }
}
