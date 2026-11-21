using Godot;
using Manosaba.Config;
using Manosaba.Multiplayer;
using Manosaba.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using System.Runtime.CompilerServices;

namespace Manosaba.Patches;

/// <summary>Panel rectangle relative to <see cref="Control.SetAnchorsPreset"/> corner.</summary>
public readonly record struct LobbyDifficultyPanelLayout(
    Control.LayoutPreset Anchor,
    float OffsetLeft,
    float OffsetRight,
    float OffsetTop,
    float OffsetBottom)
{
    public static LobbyDifficultyPanelLayout CharacterSelectDefault { get; } = new(
        Control.LayoutPreset.TopRight,
        OffsetLeft: -430f,
        OffsetRight: -12f,
        OffsetTop: 72f,
        OffsetBottom: 400f);

    /// <summary>Top-left strip so it does not cover vanilla top-right multiplayer UI on custom run.</summary>
    public static LobbyDifficultyPanelLayout CustomRunDefault { get; } = new(
        Control.LayoutPreset.TopLeft,
        OffsetLeft: 12f,
        OffsetRight: 430f,
        OffsetTop: 72f,
        OffsetBottom: 400f);
}

public enum LobbyDifficultyUiEnterKind
{
    /// <summary>First lobby UI setup: reset lobby defaults from config.</summary>
    FirstOpen,
    /// <summary>Submenu reopened: clear run snapshot only (standard/custom character flow).</summary>
    SubmenuReopened,
    /// <summary>Daily lobby became ready after async init (no reset defaults — same session).</summary>
    DailyLobbyReady,
    /// <summary>Load-run lobby: do not reset lobby state; UI is read-only (values from save / host sync only).</summary>
    LoadRunLobbyOpen,
}

/// <summary>Minimal lobby surface for difficulty UI (<see cref="StartRunLobby"/> or <see cref="LoadRunLobby"/>).</summary>
public readonly record struct LobbyDifficultyUiNetContext(INetGameService NetService, int PlayerCount)
{
    public static LobbyDifficultyUiNetContext? From(StartRunLobby? lobby) =>
        lobby == null ? null : new LobbyDifficultyUiNetContext(lobby.NetService, lobby.Players.Count);

    public static LobbyDifficultyUiNetContext? From(LoadRunLobby? lobby) =>
        lobby == null ? null : new LobbyDifficultyUiNetContext(lobby.NetService, lobby.Run.Players.Count);
}

public static class ManosabaLobbyDifficultyUiHost
{
    private sealed class DifficultyUiState
    {
        public PanelContainer? Root;
        public Label? TitleLabel;
        public Label? EnemyHpRowLabel;
        public HSlider? EnemyHpSlider;
        public Label? EnemyAttackRowLabel;
        public HSlider? EnemyAttackSlider;
        public HSlider? MurderousSlider;
        public Label? MurderousRowLabel;
        public Label? EnemyHpValueLabel;
        public Label? EnemyAttackValueLabel;
        public Label? MurderousValueLabel;
        public Label? RandomPoolRowLabel;
        public OptionButton? RandomPoolOption;

        public MessageHandlerDelegate<ManosabaDifficultySettingsMessage>? Handler;
        public bool HandlerRegistered;

        /// <summary>
        /// Net service the handler is registered on. A client that is disconnected and rejoins keeps the same screen
        /// (owner) but gets a new net service, so the handler must be moved to it.
        /// </summary>
        public INetGameService? RegisteredNetService;

        public int LastSentPlayerCount = int.MinValue;
        public double LastSentEnemyHpPercent;
        public double LastSentEnemyAttackPercent;
        public double LastSentMurderousPercent;
        public byte LastSentRandomPool;

        /// <summary>True on load-run lobbies: sliders disabled so host cannot diverge from synchronized save settings.</summary>
        public bool LoadRunLobbyReadOnly;
    }

    private static readonly ConditionalWeakTable<object, DifficultyUiState> States = new();

    private static DifficultyUiState GetState(object owner) => States.GetOrCreateValue(owner);

    public static void OnEnter(
        object owner,
        Node ownerNode,
        Control attachParent,
        Func<LobbyDifficultyUiNetContext?> getLobby,
        LobbyDifficultyPanelLayout layout,
        LobbyDifficultyUiEnterKind kind)
    {
        switch (kind)
        {
            case LobbyDifficultyUiEnterKind.FirstOpen:
                ManosabaLobbyDifficultyState.ResetToLobbyDefaults();
                ManosabaLobbyDifficultyState.SetLobbySessionActive(true);
                break;
            case LobbyDifficultyUiEnterKind.SubmenuReopened:
                ManosabaLobbyDifficultyState.ClearRunSnapshot();
                break;
            case LobbyDifficultyUiEnterKind.DailyLobbyReady:
                ManosabaLobbyDifficultyState.ResetToLobbyDefaults();
                ManosabaLobbyDifficultyState.SetLobbySessionActive(true);
                break;
            case LobbyDifficultyUiEnterKind.LoadRunLobbyOpen:
                break;
        }

        GetState(owner).LoadRunLobbyReadOnly = kind == LobbyDifficultyUiEnterKind.LoadRunLobbyOpen;

        EnsurePanel(owner, ownerNode, attachParent, getLobby, layout);
        ApplyStaticTexts(owner);
        RefreshUiFromState(owner, ownerNode, getLobby, pushLocalToRuntime: false);
        TryEarlyLobbyHook(owner, ownerNode, getLobby);
    }

    public static void TryEarlyLobbyHook(object owner, Node ownerNode, Func<LobbyDifficultyUiNetContext?> getLobby)
    {
        LobbyDifficultyUiNetContext? maybe = getLobby();
        if (!maybe.HasValue || !maybe.Value.NetService.IsConnected)
        {
            return;
        }

        LobbyDifficultyUiNetContext lobby = maybe.Value;
        DifficultyUiState state = GetState(owner);
        TryRegisterHandler(owner, ownerNode, lobby, state, getLobby);

        if (lobby.NetService.Type == NetGameType.Host)
        {
            TryHostWatchdogBroadcast(lobby, state, force: true);
        }
    }

    public static void OnProcess(object owner, Node ownerNode, Func<LobbyDifficultyUiNetContext?> getLobby)
    {
        LobbyDifficultyUiNetContext? maybe = getLobby();
        if (!maybe.HasValue || !maybe.Value.NetService.IsConnected)
        {
            return;
        }

        LobbyDifficultyUiNetContext lobby = maybe.Value;
        DifficultyUiState state = GetState(owner);
        TryRegisterHandler(owner, ownerNode, lobby, state, getLobby);

        if (CanEditDifficulty(lobby) && !state.LoadRunLobbyReadOnly)
        {
            PushUiToRuntime(owner, getLobby, state);
        }

        TryHostWatchdogBroadcast(lobby, state);
    }

    public static void OnCleanup(object owner, Func<LobbyDifficultyUiNetContext?> getLobby)
    {
        UnregisterHandlerIfNeeded(owner, getLobby);
        ManosabaLobbyDifficultyState.SetLobbySessionActive(false);
        if (States.TryGetValue(owner, out DifficultyUiState? st))
        {
            PanelContainer? root = st.Root;
            st.Root = null;
            QueueFreeIfValid(root);
            st.TitleLabel = null;
            st.EnemyHpRowLabel = null;
            st.EnemyHpSlider = null;
            st.EnemyAttackRowLabel = null;
            st.EnemyAttackSlider = null;
            st.MurderousSlider = null;
            st.MurderousRowLabel = null;
            st.EnemyHpValueLabel = null;
            st.EnemyAttackValueLabel = null;
            st.MurderousValueLabel = null;
            st.RandomPoolRowLabel = null;
            st.RandomPoolOption = null;
            st.HandlerRegistered = false;
            st.LoadRunLobbyReadOnly = false;
        }
    }

    private static void QueueFreeIfValid(Node? node)
    {
        if (node == null)
        {
            return;
        }

        try
        {
            if (GodotObject.IsInstanceValid(node))
            {
                node.QueueFree();
            }
        }
        catch (ObjectDisposedException)
        {
            // Node wrapper was already disposed; treat as already cleaned up.
        }
    }

    private static void ApplyStaticTexts(object owner)
    {
        if (!States.TryGetValue(owner, out DifficultyUiState? state))
        {
            return;
        }

        if (state.TitleLabel != null)
        {
            state.TitleLabel.Text = T("MANOSABA-LOBBY_SETTINGS.title");
        }

        if (state.EnemyHpRowLabel != null)
        {
            state.EnemyHpRowLabel.Text = T("MANOSABA-LOBBY_SETTINGS.enemy_hp_percent");
        }

        if (state.EnemyAttackRowLabel != null)
        {
            state.EnemyAttackRowLabel.Text = T("MANOSABA-LOBBY_SETTINGS.enemy_attack_percent");
        }

        if (state.MurderousRowLabel != null)
        {
            state.MurderousRowLabel.Text = T("MANOSABA-LOBBY_SETTINGS.murderous_percent");
        }

        if (state.RandomPoolRowLabel != null)
        {
            state.RandomPoolRowLabel.Text = T("MANOSABA-LOBBY_SETTINGS.random_pool");
        }

        if (state.RandomPoolOption != null)
        {
            state.RandomPoolOption.Clear();
            state.RandomPoolOption.AddItem(T("MANOSABA-LOBBY_SETTINGS.random_pool_manosaba_only"));
            state.RandomPoolOption.AddItem(T("MANOSABA-LOBBY_SETTINGS.random_pool_all"));
        }
    }

    private static string T(string key) => new LocString("settings_ui", key).GetRawText() ?? key;

    private static void EnsurePanel(
        object owner,
        Node ownerNode,
        Control attachParent,
        Func<LobbyDifficultyUiNetContext?> getLobby,
        LobbyDifficultyPanelLayout layout)
    {
        DifficultyUiState state = GetState(owner);
        if (state.Root != null && GodotObject.IsInstanceValid(state.Root))
        {
            return;
        }

        var panel = new PanelContainer { Name = "ManosabaLobbyDifficultyPanel" };
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        panel.SetAnchorsPreset(layout.Anchor);
        panel.OffsetLeft = layout.OffsetLeft;
        panel.OffsetRight = layout.OffsetRight;
        panel.OffsetTop = layout.OffsetTop;
        panel.OffsetBottom = layout.OffsetBottom;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        var title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        vbox.AddChild(title);

        var enemyRow = new HBoxContainer();
        enemyRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(enemyRow);
        var enemyLbl = new Label { CustomMinimumSize = new Vector2(140, 0) };
        enemyRow.AddChild(enemyLbl);
        var enemySlider = new HSlider
        {
            MinValue = 100,
            MaxValue = 400,
            Step = 5,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        enemySlider.ValueChanged += _ => MarkHostDirty(owner, ownerNode, getLobby);
        enemyRow.AddChild(enemySlider);
        var enemyVal = new Label { CustomMinimumSize = new Vector2(48, 0) };
        enemyRow.AddChild(enemyVal);

        var attackRow = new HBoxContainer();
        attackRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(attackRow);
        var attackLbl = new Label { CustomMinimumSize = new Vector2(140, 0) };
        attackRow.AddChild(attackLbl);
        var attackSlider = new HSlider
        {
            MinValue = 100,
            MaxValue = 400,
            Step = 5,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        attackSlider.ValueChanged += _ => MarkHostDirty(owner, ownerNode, getLobby);
        attackRow.AddChild(attackSlider);
        var attackVal = new Label { CustomMinimumSize = new Vector2(48, 0) };
        attackRow.AddChild(attackVal);

        var murderRow = new HBoxContainer();
        murderRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(murderRow);
        var murderLbl = new Label { CustomMinimumSize = new Vector2(140, 0) };
        murderRow.AddChild(murderLbl);
        var murderSlider = new HSlider
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 5,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        murderSlider.ValueChanged += _ => MarkHostDirty(owner, ownerNode, getLobby);
        murderRow.AddChild(murderSlider);
        var murderVal = new Label { CustomMinimumSize = new Vector2(48, 0) };
        murderRow.AddChild(murderVal);

        var randomRow = new HBoxContainer();
        randomRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(randomRow);
        var randomLbl = new Label { CustomMinimumSize = new Vector2(140, 0) };
        randomRow.AddChild(randomLbl);
        var randomOpt = new OptionButton
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        randomOpt.ItemSelected += _ => MarkHostDirty(owner, ownerNode, getLobby);
        randomRow.AddChild(randomOpt);

        attachParent.AddChild(panel);
        state.Root = panel;
        state.TitleLabel = title;
        state.EnemyHpRowLabel = enemyLbl;
        state.EnemyHpSlider = enemySlider;
        state.EnemyAttackRowLabel = attackLbl;
        state.EnemyAttackSlider = attackSlider;
        state.MurderousRowLabel = murderLbl;
        state.MurderousSlider = murderSlider;
        state.EnemyHpValueLabel = enemyVal;
        state.EnemyAttackValueLabel = attackVal;
        state.MurderousValueLabel = murderVal;
        state.RandomPoolRowLabel = randomLbl;
        state.RandomPoolOption = randomOpt;

        ApplyStaticTexts(owner);
        ApplyEditableMask(owner, getLobby, state);
    }

    private static void MarkHostDirty(object owner, Node ownerNode, Func<LobbyDifficultyUiNetContext?> getLobby)
    {
        DifficultyUiState st = GetState(owner);
        if (st.LoadRunLobbyReadOnly)
        {
            return;
        }

        LobbyDifficultyUiNetContext? maybe = getLobby();
        if (!maybe.HasValue || !CanEditDifficulty(maybe.Value))
        {
            return;
        }

        LobbyDifficultyUiNetContext lobby = maybe.Value;
        PushUiToRuntime(owner, getLobby, st);
        ManosabaLobbyDifficultyState.SaveLobbySnapshotAsDefaults(flushImmediately: true);
        ManosabaPerSaveDifficultyStore.TryPersistActiveRun(flushImmediately: true);
        TryHostWatchdogBroadcast(lobby, st, force: true);
    }

    private static void ApplyEditableMask(object owner, Func<LobbyDifficultyUiNetContext?> getLobby, DifficultyUiState state)
    {
        LobbyDifficultyUiNetContext? maybe = getLobby();
        bool editable = !state.LoadRunLobbyReadOnly && maybe.HasValue && CanEditDifficulty(maybe.Value);
        if (state.EnemyHpSlider != null)
        {
            state.EnemyHpSlider.Editable = editable;
        }

        if (state.EnemyAttackSlider != null)
        {
            state.EnemyAttackSlider.Editable = editable;
        }

        if (state.MurderousSlider != null)
        {
            state.MurderousSlider.Editable = editable;
        }

        if (state.RandomPoolOption != null)
        {
            state.RandomPoolOption.Disabled = !editable;
        }
    }

    private static bool CanEditDifficulty(LobbyDifficultyUiNetContext lobby)
    {
        NetGameType t = lobby.NetService.Type;
        return t == NetGameType.Singleplayer || t == NetGameType.Host;
    }

    private static void TryRegisterHandler(
        object owner,
        Node ownerNode,
        LobbyDifficultyUiNetContext lobby,
        DifficultyUiState state,
        Func<LobbyDifficultyUiNetContext?> getLobby)
    {
        if (state.HandlerRegistered && ReferenceEquals(state.RegisteredNetService, lobby.NetService))
        {
            return;
        }

        if (state.HandlerRegistered && state.RegisteredNetService != null && state.Handler != null)
        {
            // Rejoined on a new connection: drop the handler from the stale service before registering on the new one.
            state.RegisteredNetService.UnregisterMessageHandler<ManosabaDifficultySettingsMessage>(state.Handler);
        }

        state.Handler ??= delegate (ManosabaDifficultySettingsMessage message, ulong _)
        {
            ManosabaLobbyDifficultyState.ApplyFromHost(message);
            if (GodotObject.IsInstanceValid(ownerNode))
            {
                RefreshUiFromState(owner, ownerNode, getLobby, pushLocalToRuntime: false);
            }
        };

        lobby.NetService.RegisterMessageHandler<ManosabaDifficultySettingsMessage>(state.Handler);
        state.HandlerRegistered = true;
        state.RegisteredNetService = lobby.NetService;
    }

    private static void UnregisterHandlerIfNeeded(object owner, Func<LobbyDifficultyUiNetContext?> getLobby)
    {
        LobbyDifficultyUiNetContext? maybe = getLobby();
        if (!States.TryGetValue(owner, out DifficultyUiState? state))
        {
            return;
        }

        INetGameService? service = state.RegisteredNetService ?? (maybe.HasValue ? maybe.Value.NetService : null);
        if (state.HandlerRegistered && service != null && state.Handler != null)
        {
            service.UnregisterMessageHandler<ManosabaDifficultySettingsMessage>(state.Handler);
        }

        state.HandlerRegistered = false;
        state.RegisteredNetService = null;
    }

    private static ManosabaDifficultySettingsMessage BuildMessageFromLobby()
    {
        (double hpPct, double atkPct, double murPct, RandomCharacterPoolMode pool) = ManosabaLobbyDifficultyState.GetLobbySnapshot();
        return new ManosabaDifficultySettingsMessage
        {
            enemyHpMultiplierPercent = hpPct,
            enemyAttackDamageMultiplierPercent = atkPct,
            murderousImpulseAllyDamageMultiplierPercent = murPct,
            randomCharacterPoolMode = (byte)pool,
        };
    }

    private static void TryHostWatchdogBroadcast(LobbyDifficultyUiNetContext lobby, DifficultyUiState state, bool force = false)
    {
        if (lobby.NetService.Type == NetGameType.Client)
        {
            return;
        }

        int players = lobby.PlayerCount;
        (double hpPct, double atkPct, double murPct, RandomCharacterPoolMode pool) = ManosabaLobbyDifficultyState.GetLobbySnapshot();
        byte poolByte = (byte)pool;
        bool changed = force
            || players != state.LastSentPlayerCount
            || Math.Abs(hpPct - state.LastSentEnemyHpPercent) > 0.0001d
            || Math.Abs(atkPct - state.LastSentEnemyAttackPercent) > 0.0001d
            || Math.Abs(murPct - state.LastSentMurderousPercent) > 0.0001d
            || poolByte != state.LastSentRandomPool;

        if (!changed)
        {
            return;
        }

        state.LastSentPlayerCount = players;
        state.LastSentEnemyHpPercent = hpPct;
        state.LastSentEnemyAttackPercent = atkPct;
        state.LastSentMurderousPercent = murPct;
        state.LastSentRandomPool = poolByte;

        ManosabaDifficultySettingsMessage message = BuildMessageFromLobby();

        if (lobby.NetService.Type == NetGameType.Singleplayer)
        {
            ManosabaLobbyDifficultyState.ApplyFromHost(message);
            return;
        }

        lobby.NetService.SendMessage(message);
    }

    private static void PushUiToRuntime(object owner, Func<LobbyDifficultyUiNetContext?> getLobby, DifficultyUiState state)
    {
        if (state.EnemyHpSlider == null || state.EnemyAttackSlider == null || state.MurderousSlider == null)
        {
            return;
        }

        RandomCharacterPoolMode pool = RandomCharacterPoolMode.ManosabaCharactersOnly;
        if (state.RandomPoolOption != null)
        {
            int sel = state.RandomPoolOption.Selected;
            pool = sel <= 0 ? RandomCharacterPoolMode.ManosabaCharactersOnly : RandomCharacterPoolMode.AllCharacters;
        }

        ManosabaLobbyDifficultyState.ApplyFromHost(
            state.EnemyHpSlider.Value,
            state.EnemyAttackSlider.Value,
            state.MurderousSlider.Value,
            pool);

        if (state.EnemyHpValueLabel != null)
        {
            state.EnemyHpValueLabel.Text = $"{state.EnemyHpSlider.Value:0}%";
        }

        if (state.EnemyAttackValueLabel != null)
        {
            state.EnemyAttackValueLabel.Text = $"{state.EnemyAttackSlider.Value:0}%";
        }

        if (state.MurderousValueLabel != null)
        {
            state.MurderousValueLabel.Text = $"{state.MurderousSlider.Value:0}%";
        }
    }

    private static void RefreshUiFromState(object owner, Node ownerNode, Func<LobbyDifficultyUiNetContext?> getLobby, bool pushLocalToRuntime)
    {
        if (!States.TryGetValue(owner, out DifficultyUiState? state)
            || state.EnemyHpSlider == null
            || state.EnemyAttackSlider == null)
        {
            return;
        }

        (double hpPct, double atkPct, double murPct, RandomCharacterPoolMode pool) = ManosabaLobbyDifficultyState.GetLobbySnapshot();
        state.EnemyHpSlider.Value = hpPct;
        state.EnemyAttackSlider.Value = atkPct;
        if (state.MurderousSlider != null)
        {
            state.MurderousSlider.Value = murPct;
        }

        if (state.RandomPoolOption != null)
        {
            state.RandomPoolOption.SetBlockSignals(true);
            state.RandomPoolOption.Selected = pool == RandomCharacterPoolMode.AllCharacters ? 1 : 0;
            state.RandomPoolOption.SetBlockSignals(false);
        }

        if (state.EnemyHpValueLabel != null)
        {
            state.EnemyHpValueLabel.Text = $"{state.EnemyHpSlider.Value:0}%";
        }

        if (state.EnemyAttackValueLabel != null)
        {
            state.EnemyAttackValueLabel.Text = $"{state.EnemyAttackSlider.Value:0}%";
        }

        if (state.MurderousValueLabel != null && state.MurderousSlider != null)
        {
            state.MurderousValueLabel.Text = $"{state.MurderousSlider.Value:0}%";
        }

        ApplyEditableMask(owner, getLobby, state);

        if (pushLocalToRuntime)
        {
            PushUiToRuntime(owner, getLobby, state);
        }
    }
}
