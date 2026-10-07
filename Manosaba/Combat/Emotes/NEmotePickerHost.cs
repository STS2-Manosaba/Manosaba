using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Combat.Emotes;

public partial class NEmotePickerHost : Control
{
    internal const float ToggleSize = 64f;
    private const float ThumbSize = 120f;
    private const float GridCellGap = 10f;
    private const float PanelPadding = 10f;
    private const float GapBetweenPanelAndToggle = 6f;
    internal const float MarginFromScreenRight = 16f;
    internal const float MarginBelowTopBar = 86f;
    private const float ExpandAnimSeconds = 0.2f;
    private const int MaxGridRows = 2;
    private const float MaxPanelHeight = 400f;

    private Button? _toggleButton;
    private PanelContainer? _panel;
    private ScrollContainer? _scroll;
    private GridContainer? _grid;
    private Tween? _expandTween;
    private bool _isExpanded;
    private Vector2 _lastToggleGlobalPos = new(float.MinValue, float.MinValue);
    private Vector2 _lastViewportSize = Vector2.Zero;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        BuildUi();
        SetExpanded(false, animate: false);
        RebuildStickerGrid();
    }

    public override void _Process(double delta)
    {
        _ = delta;
        RefreshVisibility();
        if (!Visible)
        {
            return;
        }

        SyncLayoutToViewportAnchor();
    }

    public void RebuildStickerGrid()
    {
        if (_grid == null)
        {
            return;
        }

        foreach (Node child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        EmoteCatalog.Reload();

        List<TextureButton> buttons = [];
        foreach (string stickerId in EmoteCatalog.StickerIds)
        {
            Texture2D? texture = EmoteCatalog.TryGetTexture(stickerId);
            if (texture == null)
            {
                continue;
            }

            TextureButton button = new()
            {
                CustomMinimumSize = new Vector2(ThumbSize, ThumbSize),
                TextureNormal = texture,
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                FocusMode = FocusModeEnum.None,
            };
            button.Pressed += () => OnStickerPressed(stickerId);
            buttons.Add(button);
        }

        int count = buttons.Count;
        if (count == 0)
        {
            Label hint = new()
            {
                Text = "無表情圖\n請重新匯出 pck",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(ThumbSize * 4 + GridCellGap * 3f, ThumbSize * MaxGridRows),
            };
            _grid.AddChild(hint);
        }
        else
        {
            int rows = GetLayoutRows(count);
            int columns = GetLayoutColumns(count);
            _grid.Columns = columns;

            // 最多 2 行；直向優先填欄 → 01 03 05 07 / 02 04 06 08
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int index = row + (col * rows);
                    if (index < count)
                    {
                        _grid.AddChild(buttons[index]);
                    }
                }
            }
        }

        ApplyPanelLayoutForExpandedState();
        RefreshChildLayout();
    }

    public void Collapse()
    {
        SetExpanded(false, animate: true);
    }

    internal void RefreshVisibility()
    {
        bool shouldShow = EmotePickerUi.ShouldBeVisibleOnCombat();
        if (Visible == shouldShow)
        {
            return;
        }

        Visible = shouldShow;
        if (!shouldShow && _isExpanded)
        {
            SetExpanded(false, animate: false);
        }
    }

    private void BuildUi()
    {
        _toggleButton = new Button
        {
            CustomMinimumSize = new Vector2(ToggleSize, ToggleSize),
            Size = new Vector2(ToggleSize, ToggleSize),
            Text = "☺",
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
            ActionMode = BaseButton.ActionModeEnum.Press,
        };
        _toggleButton.AddThemeFontSizeOverride("font_size", 32);
        _toggleButton.Pressed += OnTogglePressed;
        AddChild(_toggleButton);

        _panel = new PanelContainer
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        _scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        _grid = new GridContainer { Columns = 4 };
        _grid.AddThemeConstantOverride("h_separation", (int)GridCellGap);
        _grid.AddThemeConstantOverride("v_separation", (int)GridCellGap);

        _scroll.AddChild(_grid);
        _panel.AddChild(_scroll);
        AddChild(_panel);
    }

    private void OnTogglePressed()
    {
        SetExpanded(!_isExpanded, animate: true);
    }

    private void OnStickerPressed(string stickerId)
    {
        CombatEmoteService.SendLocal(stickerId);
    }

    private void SetExpanded(bool expanded, bool animate)
    {
        _isExpanded = expanded;
        if (_toggleButton != null)
        {
            _toggleButton.Text = expanded ? "<" : "☺";
        }

        if (_panel == null)
        {
            return;
        }

        _expandTween?.Kill();
        ApplyPanelLayoutForExpandedState();

        if (!animate)
        {
            ApplyExpandedLayout(expanded);
            SyncLayoutToViewportAnchor();
            return;
        }

        float startWidth = _panel.Size.X;
        ApplyExpandedLayout(expanded);
        SyncLayoutToViewportAnchor();
        float endWidth = _panel.Size.X;

        if (Mathf.IsEqualApprox(startWidth, endWidth))
        {
            return;
        }

        _panel.Visible = true;
        _panel.Size = new Vector2(startWidth, _panel.Size.Y);
        _expandTween = CreateTween();
        _expandTween.TweenProperty(_panel, "size:x", endWidth, ExpandAnimSeconds)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _expandTween.TweenCallback(Callable.From(() =>
        {
            if (!expanded)
            {
                _panel.Visible = false;
            }

            SyncLayoutToViewportAnchor();
        }));
    }

    private void ApplyExpandedLayout(bool expanded)
    {
        if (_panel == null || _toggleButton == null)
        {
            return;
        }

        if (!expanded)
        {
            _panel.Visible = false;
            _panel.MouseFilter = MouseFilterEnum.Ignore;
            _panel.Size = Vector2.Zero;
            _panel.CustomMinimumSize = Vector2.Zero;
            return;
        }

        _panel.Visible = true;
        _panel.MouseFilter = MouseFilterEnum.Stop;
        RefreshChildLayout();
    }

    private void ApplyPanelLayoutForExpandedState()
    {
        if (_panel == null || _grid == null)
        {
            return;
        }

        if (!_isExpanded)
        {
            _panel.CustomMinimumSize = Vector2.Zero;
            _panel.Size = Vector2.Zero;
            return;
        }

        int count = _grid.GetChildCount();
        int rows = count == 0 ? MaxGridRows : GetLayoutRows(count);
        int columns = count == 0 ? 2 : GetLayoutColumns(count);
        float contentHeight = rows * (ThumbSize + GridCellGap);
        float panelHeight = Mathf.Max(ToggleSize, Mathf.Min(MaxPanelHeight, contentHeight + PanelPadding * 2f));
        float panelWidth = columns * ThumbSize + (columns + 1) * GridCellGap + PanelPadding * 2f;

        _panel.CustomMinimumSize = new Vector2(panelWidth, panelHeight);
        _panel.Size = _panel.CustomMinimumSize;
        if (_scroll != null)
        {
            _scroll.CustomMinimumSize = new Vector2(panelWidth - PanelPadding * 2f, panelHeight - PanelPadding * 2f);
            _scroll.Size = _scroll.CustomMinimumSize;
        }
    }

    private void RefreshChildLayout()
    {
        if (_toggleButton == null || _panel == null)
        {
            return;
        }

        if (!_isExpanded)
        {
            Size = new Vector2(ToggleSize, ToggleSize);
            _toggleButton.Position = Vector2.Zero;
            _toggleButton.Size = new Vector2(ToggleSize, ToggleSize);
            return;
        }

        ApplyPanelLayoutForExpandedState();
        Vector2 panelSize = _panel.Size;
        float hostWidth = panelSize.X + GapBetweenPanelAndToggle + ToggleSize;
        float hostHeight = Mathf.Max(panelSize.Y, ToggleSize);
        Size = new Vector2(hostWidth, hostHeight);
        _panel.Position = Vector2.Zero;
        _panel.Size = panelSize;
        _toggleButton.Position = new Vector2(panelSize.X + GapBetweenPanelAndToggle, 0f);
        _toggleButton.Size = new Vector2(ToggleSize, ToggleSize);
    }

    private void SyncLayoutToViewportAnchor()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 toggleGlobalTopLeft = new(
            viewportSize.X - MarginFromScreenRight - ToggleSize,
            MarginBelowTopBar);

        RefreshChildLayout();

        Vector2 hostTopLeft = _isExpanded
            ? toggleGlobalTopLeft - new Vector2(Size.X - ToggleSize, 0f)
            : toggleGlobalTopLeft;

        if (hostTopLeft == _lastToggleGlobalPos && viewportSize == _lastViewportSize)
        {
            return;
        }

        _lastToggleGlobalPos = hostTopLeft;
        _lastViewportSize = viewportSize;

        SetAnchorsPreset(LayoutPreset.TopLeft);
        GlobalPosition = hostTopLeft;
    }

    /// <summary>最多 2 行；欄數 = ceil(數量 / 2)。</summary>
    private static int GetLayoutRows(int count)
    {
        return count <= 1 ? 1 : MaxGridRows;
    }

    private static int GetLayoutColumns(int count)
    {
        if (count <= 1)
        {
            return 1;
        }

        return (count + MaxGridRows - 1) / MaxGridRows;
    }
}

public static class EmotePickerUi
{
    private const string HostNodeName = "ManosabaEmotePickerInjected";

    internal static Vector2 GetPositionBelowPicker(Control combatUi, Vector2 displaySize, float gap)
    {
        NEmotePickerHost? host = FindHost(combatUi);
        if (host != null && !host.IsQueuedForDeletion() && host.Visible)
        {
            Rect2 rect = host.GetGlobalRect();
            return new Vector2(rect.End.X - displaySize.X, rect.End.Y + gap);
        }

        // Solo combat has no picker. Reserve its normal collapsed location anyway.
        Vector2 viewportSize = combatUi.GetViewport().GetVisibleRect().Size;
        return new Vector2(
            viewportSize.X - NEmotePickerHost.MarginFromScreenRight - displaySize.X,
            NEmotePickerHost.MarginBelowTopBar + NEmotePickerHost.ToggleSize + gap);
    }

    public static void EnsureShown()
    {
        if (!ShouldShowEmotePicker())
        {
            Remove();
            return;
        }

        NCombatUi? combatUi = NCombatRoom.Instance?.Ui;
        if (combatUi == null)
        {
            return;
        }

        NEmotePickerHost? existing = FindHost(combatUi);
        if (existing != null)
        {
            existing.RebuildStickerGrid();
            existing.RefreshVisibility();
            return;
        }

        NEmotePickerHost host = new() { Name = HostNodeName };
        combatUi.AddChild(host);
        host.ZIndex = 120;
        host.MouseFilter = Control.MouseFilterEnum.Stop;
        host.RefreshVisibility();
    }

    public static void Remove()
    {
        NCombatUi? combatUi = NCombatRoom.Instance?.Ui;
        if (combatUi != null)
        {
            FindHost(combatUi)?.QueueFree();
        }

        NGlobalUi? globalUi = NRun.Instance?.GlobalUi;
        if (globalUi != null)
        {
            FindHost(globalUi.TopBar)?.QueueFree();
            FindHost(globalUi)?.QueueFree();
        }

        CombatEmoteService.ClearActivePopups();
    }

    public static bool ShouldShowEmotePicker()
    {
        RunManager runManager = RunManager.Instance;
        if (!runManager.IsInProgress || runManager.NetService?.Type.IsMultiplayer() != true)
        {
            return false;
        }

        NCombatRoom? combatRoom = NCombatRoom.Instance;
        if (combatRoom == null || combatRoom.Mode != CombatRoomMode.ActiveCombat)
        {
            return false;
        }

        return true;
    }

    public static bool ShouldBeVisibleOnCombat()
    {
        if (!ShouldShowEmotePicker())
        {
            return false;
        }

        NCombatRoom? combatRoom = NCombatRoom.Instance;
        if (combatRoom == null)
        {
            return false;
        }

        if (!ActiveScreenContext.Instance.IsCurrent(combatRoom))
        {
            return false;
        }

        if (NCapstoneContainer.Instance?.InUse == true)
        {
            return false;
        }

        if (combatRoom.Ui.Hand.IsInCardSelection)
        {
            return false;
        }

        return true;
    }

    public static void CollapseIfPresent()
    {
        NCombatUi? combatUi = NCombatRoom.Instance?.Ui;
        FindHost(combatUi)?.Collapse();

        NGlobalUi? globalUi = NRun.Instance?.GlobalUi;
        if (globalUi == null)
        {
            return;
        }

        FindHost(globalUi.TopBar)?.Collapse();
        FindHost(globalUi)?.Collapse();
    }

    private static NEmotePickerHost? FindHost(Node? root)
    {
        if (root == null)
        {
            return null;
        }

        Node? node = root.GetNodeOrNull(HostNodeName);
        if (node is NEmotePickerHost host)
        {
            return host;
        }

        foreach (Node child in root.GetChildren())
        {
            if (child is NEmotePickerHost match)
            {
                return match;
            }
        }

        return null;
    }
}
