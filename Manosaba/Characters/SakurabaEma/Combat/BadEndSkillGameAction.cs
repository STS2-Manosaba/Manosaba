using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace manosaba.Characters.SakurabaEma.Combat;

public sealed class BadEndSkillGameAction : GameAction
{
    public override ulong OwnerId => Player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;
    public Player Player { get; }

    public BadEndSkillGameAction(Player player)
    {
        Player = player;
    }

    protected override async Task ExecuteAction()
    {
        if (Player.Creature?.CombatState is not { } combatState)
        {
            Cancel();
            return;
        }

        BadEndSkillPower? skillPower = Player.Creature.GetPower<BadEndSkillPower>();
        if (skillPower == null || !skillPower.IsReady)
        {
            Cancel();
            return;
        }

        List<CardModel> options = EmaBadEndingCards.CreateRandomOptions(combatState, Player, 2);
        CardModel? selected = options.Count == 1
            ? options[0]
            : await CardSelectCmd.FromChooseACardScreen(new GameActionPlayerChoiceContext(this), options, Player);

        if (selected == null)
        {
            Cancel();
            return;
        }

        PlayerChoiceContext choiceContext = new GameActionPlayerChoiceContext(this);
        skillPower.MarkUsed();
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Player);
        CardCmd.PreviewCardPileAdd(result);

        List<Player> hiros = combatState.Players
            .Where(player => player.Creature.IsAlive && player.Character.Id.Entry.EndsWith(global::manosaba.Characters.NikaidoHiro.NikaidoHiro.CharacterId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(player => player.NetId)
            .ToList();
        if (hiros.Count > 0)
        {
            Player? hiro = Player.RunState.Rng.CombatCardSelection.NextItem(hiros);
            if (hiro == null)
                return;

            CardModel emaIsEvil = combatState.CreateCard<EmaIsEvil>(hiro);
            CardPileAddResult hiroResult = await CardPileCmd.AddGeneratedCardToCombat(emaIsEvil, PileType.Hand, hiro);
            CardCmd.PreviewCardPileAdd(hiroResult);
        }
    }

    protected override void CancelAction()
    {
        BadEndSkillActivation.ClearActivationPending(Player);
    }

    public override INetAction ToNetAction() => new NetBadEndSkillGameAction();

    public override string ToString() => $"{nameof(BadEndSkillGameAction)} for player {Player.NetId}";
}

public struct NetBadEndSkillGameAction : INetAction, IPacketSerializable
{
    public GameAction ToGameAction(Player player) => new BadEndSkillGameAction(player);
    public void Serialize(PacketWriter writer) { }
    public void Deserialize(PacketReader reader) { }
    public override string ToString() => nameof(NetBadEndSkillGameAction);
}

public static class BadEndSkillActivation
{
    private static ActionExecutor? _subscribedExecutor;
    private static ulong? _pendingPlayerNetId;

    public static bool IsActivationPending(Player player) => _pendingPlayerNetId == player.NetId;

    public static bool CanActivate(Player player)
    {
        if (IsActivationPending(player))
            return false;

        if (player.Creature?.GetPower<BadEndSkillPower>() is not { IsReady: true })
            return false;

        if (!LocalContext.IsMe(player))
            return false;

        NCombatRoom? combatRoom = NCombatRoom.Instance;
        NCombatUi? combatUi = combatRoom?.Ui;
        if (combatUi == null || combatRoom == null || combatRoom.Mode != CombatRoomMode.ActiveCombat)
            return false;

        if (!ActiveScreenContext.Instance.IsCurrent(combatRoom))
            return false;

        if (combatUi.Hand.IsInCardSelection || combatUi.Hand.InCardPlay || combatUi.Hand.CurrentMode != NPlayerHand.Mode.Play)
            return false;

        if (player.Creature?.CombatState is not { CurrentSide: CombatSide.Player })
            return false;

        if (RunManager.Instance.ActionQueueSynchronizer.CombatState != ActionSynchronizerCombatState.PlayPhase
            || CombatManager.Instance.PlayerActionsDisabled)
            return false;

        if (CombatManager.Instance.PlayersTakingExtraTurn.Count > 0 &&
            !CombatManager.Instance.PlayersTakingExtraTurn.Contains(player))
            return false;

        if (CombatManager.Instance.IsPlayerReadyToEndTurn(player))
            return false;

        return player.Creature is { IsAlive: true };
    }

    public static void TryEnqueue(Player player)
    {
        if (!CanActivate(player))
            return;

        EnsurePendingTrackerSubscribed();
        _pendingPlayerNetId = player.NetId;
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new BadEndSkillGameAction(player));
    }

    public static void ClearActivationPending(Player player)
    {
        if (_pendingPlayerNetId == player.NetId)
            _pendingPlayerNetId = null;
    }

    public static void ClearAllActivationPending() => _pendingPlayerNetId = null;

    private static void EnsurePendingTrackerSubscribed()
    {
        ActionExecutor executor = RunManager.Instance.ActionExecutor;
        if (_subscribedExecutor == executor)
            return;

        if (_subscribedExecutor != null)
            _subscribedExecutor.AfterActionExecuted -= OnAfterActionExecuted;

        _subscribedExecutor = executor;
        _subscribedExecutor.AfterActionExecuted += OnAfterActionExecuted;
    }

    private static void OnAfterActionExecuted(GameAction action)
    {
        if (action is BadEndSkillGameAction && action.OwnerId == _pendingPlayerNetId)
            _pendingPlayerNetId = null;
    }
}
