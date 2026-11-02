using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.TonoHanna.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Characters.TonoHanna.Combat;

public sealed class HighSenseSkillGameAction : GameAction
{
    public override ulong OwnerId => Player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public Player Player { get; }

    public HighSenseSkillGameAction(Player player) => Player = player;

    protected override async Task ExecuteAction()
    {
        if (Player.Creature is not { } creature)
        {
            Cancel();
            return;
        }

        HighSensePower? power = creature.GetPower<HighSensePower>();
        if (power == null || !power.IsReady)
        {
            Cancel();
            return;
        }

        PlayerChoiceContext choiceContext = new GameActionPlayerChoiceContext(this);
        power.MarkUsed();
        power.ActivateDiceBonus();
        await PowerCmd.Apply<HannaPuppetPower>(choiceContext, creature, 1m, creature, null);
    }

    protected override void CancelAction() => HighSenseSkillActivation.ClearActivationPending(Player);

    public override INetAction ToNetAction() => new NetHighSenseSkillGameAction();

    public override string ToString() => $"{nameof(HighSenseSkillGameAction)} for player {Player.NetId}";
}

public struct NetHighSenseSkillGameAction : INetAction, IPacketSerializable
{
    public GameAction ToGameAction(Player player) => new HighSenseSkillGameAction(player);
    public void Serialize(PacketWriter writer) { }
    public void Deserialize(PacketReader reader) { }
    public override string ToString() => nameof(NetHighSenseSkillGameAction);
}

public static class HighSenseSkillActivation
{
    private static ActionExecutor? _subscribedExecutor;
    private static ulong? _pendingPlayerNetId;

    public static bool IsActivationPending(Player player) => _pendingPlayerNetId == player.NetId;

    public static bool CanActivate(Player player)
    {
        if (IsActivationPending(player)
            || player.Creature?.GetPower<HighSensePower>() is not { IsReady: true }
            || !LocalContext.IsMe(player))
        {
            return false;
        }

        NCombatRoom? room = NCombatRoom.Instance;
        NCombatUi? combatUi = room?.Ui;
        if (room == null || combatUi == null || room.Mode != CombatRoomMode.ActiveCombat
            || !ActiveScreenContext.Instance.IsCurrent(room)
            || combatUi.Hand.IsInCardSelection || combatUi.Hand.InCardPlay
            || combatUi.Hand.CurrentMode != NPlayerHand.Mode.Play
            || player.Creature?.CombatState is not { CurrentSide: CombatSide.Player }
            || RunManager.Instance.ActionQueueSynchronizer.CombatState != ActionSynchronizerCombatState.PlayPhase
            || CombatManager.Instance.PlayerActionsDisabled
            || CombatManager.Instance.PlayersTakingExtraTurn.Count > 0
                && !CombatManager.Instance.PlayersTakingExtraTurn.Contains(player)
            || CombatManager.Instance.IsPlayerReadyToEndTurn(player))
        {
            return false;
        }

        return player.Creature is { IsAlive: true };
    }

    public static void TryEnqueue(Player player)
    {
        if (!CanActivate(player))
        {
            return;
        }

        EnsurePendingTrackerSubscribed();
        _pendingPlayerNetId = player.NetId;
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new HighSenseSkillGameAction(player));
    }

    public static void ClearActivationPending(Player player)
    {
        if (_pendingPlayerNetId == player.NetId)
        {
            _pendingPlayerNetId = null;
        }
    }

    public static void ClearAllActivationPending() => _pendingPlayerNetId = null;

    private static void EnsurePendingTrackerSubscribed()
    {
        ActionExecutor executor = RunManager.Instance.ActionExecutor;
        if (_subscribedExecutor == executor)
        {
            return;
        }

        if (_subscribedExecutor != null)
        {
            _subscribedExecutor.AfterActionExecuted -= OnAfterActionExecuted;
        }

        _subscribedExecutor = executor;
        _subscribedExecutor.AfterActionExecuted += OnAfterActionExecuted;
    }

    private static void OnAfterActionExecuted(GameAction action)
    {
        if (action is HighSenseSkillGameAction && action.OwnerId == _pendingPlayerNetId)
        {
            _pendingPlayerNetId = null;
        }
    }
}
