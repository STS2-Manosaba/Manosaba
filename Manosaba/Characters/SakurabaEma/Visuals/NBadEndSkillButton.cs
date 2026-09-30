using Godot;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Rooms;

namespace manosaba.Characters.SakurabaEma.Visuals;

public sealed partial class NBadEndSkillButton : Control
{
    private const float GapLeftOfEndTurn = 10f;
    private static readonly Vector2 ButtonSize = new(220f, 90f);
    private static readonly StringName VShaderParam = new("v");
    private static readonly Vector2 HoverTipOffset = new(-76f, -210f);
    private static readonly StringName ButtonTexturePath = "res://images/packed/combat_ui/end_turn_button.png";

    private Player? _player;
    private Control? _visuals;
    private TextureRect? _image;
    private ShaderMaterial? _hsv;
    private MegaLabel? _label;
    private HoverTip? _hoverTip;
    private Tween? _hoverTween;
    private bool _isHovered;
    private Vector2 _lastEndTurnGlobalPos = new(float.MinValue, float.MinValue);
    private Vector2 _lastEndTurnSize = Vector2.Zero;
    private string _cachedActivateLabel = string.Empty;
    private int _cachedCooldown = int.MinValue;

    public void Initialize(Player player)
    {
        _player = player;
        if (IsNodeReady())
        {
            Refresh(force: true);
        }
    }

    public override void _Ready()
    {
        CustomMinimumSize = ButtonSize;
        Size = ButtonSize;
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        BuildVisuals();
        Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(OnGuiInput));
        Connect(Control.SignalName.MouseEntered, Callable.From(OnMouseEntered));
        Connect(Control.SignalName.MouseExited, Callable.From(OnMouseExited));
        Refresh(force: true);
    }

    public override void _Process(double delta)
    {
        _ = delta;
        SyncLayoutToEndTurnIfNeeded();
        Refresh(force: false);
    }

    public void SyncLayoutToEndTurn(NEndTurnButton endTurnButton)
    {
        _lastEndTurnGlobalPos = new Vector2(float.MinValue, float.MinValue);
        ApplyLayoutFrom(endTurnButton);
    }

    private void SyncLayoutToEndTurnIfNeeded()
    {
        NEndTurnButton? endTurnButton = NCombatRoom.Instance?.Ui?.EndTurnButton;
        if (endTurnButton == null)
        {
            return;
        }

        Vector2 globalPos = endTurnButton.GlobalPosition;
        Vector2 size = endTurnButton.Size;
        if (globalPos == _lastEndTurnGlobalPos && size == _lastEndTurnSize)
        {
            return;
        }

        ApplyLayoutFrom(endTurnButton);
    }

    private void ApplyLayoutFrom(NEndTurnButton endTurnButton)
    {
        _lastEndTurnGlobalPos = endTurnButton.GlobalPosition;
        _lastEndTurnSize = endTurnButton.Size;

        Vector2 size = _lastEndTurnSize;
        if (size.X <= 0f || size.Y <= 0f)
        {
            size = ButtonSize;
        }

        SetAnchorsPreset(LayoutPreset.TopLeft);
        GlobalPosition = _lastEndTurnGlobalPos + new Vector2(-(size.X + GapLeftOfEndTurn), 0f);
        Size = size;
        CustomMinimumSize = size;
        PivotOffset = endTurnButton.PivotOffset;
    }

    private void BuildVisuals()
    {
        _cachedActivateLabel = new LocString("powers", "MANOSABA-BAD_END_SKILL_POWER.activateLabel").GetFormattedText();
        _hoverTip = new HoverTip(
            new LocString("powers", "MANOSABA-BAD_END_SKILL_POWER.title"),
            new LocString("powers", "MANOSABA-BAD_END_SKILL_POWER.description"));

        Control visuals = new()
        {
            Name = "Visuals",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        visuals.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(visuals);
        _visuals = visuals;

        Shader? hsvShader = GD.Load<Shader>("res://shaders/hsv.gdshader");
        _hsv = new ShaderMaterial
        {
            ResourceLocalToScene = true,
            Shader = hsvShader,
        };
        _hsv.SetShaderParameter("h", 1f);
        _hsv.SetShaderParameter("s", 1f);
        _hsv.SetShaderParameter("v", 1f);

        TextureRect image = new()
        {
            Name = "Image",
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = PreloadManager.Cache.GetCompressedTexture2D(ButtonTexturePath),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Scale = new Vector2(0.5f, 0.5f),
            PivotOffset = new Vector2(256f, 128f),
            Material = _hsv,
        };
        image.SetAnchorsPreset(LayoutPreset.Center);
        image.OffsetLeft = -256f;
        image.OffsetTop = -128f;
        image.OffsetRight = 256f;
        image.OffsetBottom = 128f;
        image.GrowHorizontal = GrowDirection.Both;
        image.GrowVertical = GrowDirection.Both;
        visuals.AddChild(image);
        _image = image;

        _label = new MegaLabel
        {
            Name = "Label",
            MouseFilter = MouseFilterEnum.Ignore,
            Text = _cachedActivateLabel,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MaxFontSize = 26,
        };
        _label.AddThemeColorOverride("font_color", StsColors.cream);
        _label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.188235f));
        _label.AddThemeColorOverride("font_outline_color", new Color(0.0756f, 0.12084f, 0.18f));
        _label.AddThemeConstantOverride("line_spacing", -4);
        _label.AddThemeConstantOverride("shadow_offset_x", 3);
        _label.AddThemeConstantOverride("shadow_offset_y", 2);
        _label.AddThemeConstantOverride("outline_size", 12);
        _label.AddThemeConstantOverride("shadow_outline_size", 12);
        _label.AddThemeFontSizeOverride("font_size", 26);
        _label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _label.OffsetLeft = 16f;
        _label.OffsetTop = 11f;
        _label.OffsetRight = -16f;
        _label.OffsetBottom = -7f;
        visuals.AddChild(_label);
    }

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (_player == null || !BadEndSkillActivation.CanActivate(_player))
        {
            return;
        }

        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            BadEndSkillActivation.TryEnqueue(_player);
            Refresh(force: true);
            AcceptEvent();
        }
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        if (_player != null && BadEndSkillActivation.CanActivate(_player))
        {
            ApplyHoverVisuals();
        }

        if (_hoverTip == null || !Visible)
        {
            return;
        }

        NHoverTipSet.CreateAndShow(this, _hoverTip)?.SetGlobalPosition(GlobalPosition + HoverTipOffset);
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        NHoverTipSet.Remove(this);
        RestoreIdleVisuals();
    }

    private void ApplyHoverVisuals()
    {
        _hoverTween?.Kill();
        _hsv?.SetShaderParameter(VShaderParam, 1.5f);
        if (_visuals != null)
        {
            _visuals.Position = new Vector2(0f, -2f);
        }
    }

    private void RestoreIdleVisuals()
    {
        _hoverTween?.Kill();
        if (_hsv == null)
        {
            return;
        }

        bool canClick = _player != null && BadEndSkillActivation.CanActivate(_player);
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenMethod(Callable.From<float>(UpdateShaderV), _hsv.GetShaderParameter(VShaderParam), 1f, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);

        if (_visuals != null)
        {
            _hoverTween.TweenProperty(_visuals, "position", Vector2.Zero, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        }

        if (_label != null)
        {
            _hoverTween.TweenProperty(_label, "modulate", canClick ? StsColors.cream : StsColors.gray, 0.5)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        }
    }

    private void UpdateShaderV(float value)
    {
        _hsv?.SetShaderParameter(VShaderParam, value);
    }

    private void Refresh(bool force)
    {
        if (_player == null)
        {
            Visible = false;
            return;
        }

        BadEndSkillPower? skillPower = _player.Creature?.GetPower<BadEndSkillPower>();
        bool hasPower = skillPower != null;
        Visible = hasPower && ShouldBeVisibleOnCombatUi();
        if (!hasPower || !Visible)
        {
            if (!Visible && _isHovered)
            {
                _isHovered = false;
                NHoverTipSet.Remove(this);
                RestoreIdleVisuals();
            }

            return;
        }

        int cooldown = skillPower!.CooldownRemaining;
        bool canClick = BadEndSkillActivation.CanActivate(_player);

        if (!canClick && _isHovered)
        {
            _isHovered = false;
            NHoverTipSet.Remove(this);
            RestoreIdleVisuals();
        }

        if (_image != null)
        {
            _image.Modulate = canClick ? Colors.White : StsColors.gray;
        }

        if (_label != null)
        {
            if (force || cooldown != _cachedCooldown)
            {
                _cachedCooldown = cooldown;
                _label.Text = cooldown <= 0
                    ? _cachedActivateLabel
                    : cooldown.ToString();
            }

            _label.Modulate = canClick ? StsColors.cream : StsColors.gray;
        }

        MouseFilter = canClick ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        _ = force;
    }

    private static bool ShouldBeVisibleOnCombatUi()
    {
        NCombatRoom? combatRoom = NCombatRoom.Instance;
        if (combatRoom == null || combatRoom.Mode != CombatRoomMode.ActiveCombat)
        {
            return false;
        }

        return ActiveScreenContext.Instance.IsCurrent(combatRoom);
    }
}

public static class BadEndSkillButtonUi
{
    private const string ButtonNodeName = "BadEndSkillButtonInjected";

    public static void EnsureShown(Player player)
    {
        NCombatUi? combatUi = NCombatRoom.Instance?.Ui;
        if (combatUi == null || player.Creature?.GetPower<BadEndSkillPower>() == null)
        {
            return;
        }

        if (!LocalContext.IsMe(player))
        {
            return;
        }

        NEndTurnButton endTurnButton = combatUi.EndTurnButton;
        NBadEndSkillButton? existing = FindButton(combatUi);
        if (existing != null)
        {
            existing.Initialize(player);
            existing.SyncLayoutToEndTurn(endTurnButton);
            return;
        }

        NBadEndSkillButton button = new();
        button.Name = ButtonNodeName;
        button.Initialize(player);
        combatUi.AddChild(button);
        combatUi.MoveChild(button, endTurnButton.GetIndex());
        button.ZIndex = endTurnButton.ZIndex + 1;
        button.SyncLayoutToEndTurn(endTurnButton);
    }

    public static void RemoveFrom(NCombatUi combatUi)
    {
        FindButton(combatUi)?.QueueFree();
    }

    public static void RefreshIfPresent(NCombatUi combatUi, ICombatState state)
    {
        Player? me = TryResolveLocalPlayer(state);
        if (me?.Creature?.GetPower<BadEndSkillPower>() == null)
        {
            RemoveFrom(combatUi);
            return;
        }

        EnsureShown(me);
    }

    private static NBadEndSkillButton? FindButton(NCombatUi combatUi)
    {
        Node? node = combatUi.GetNodeOrNull(ButtonNodeName);
        if (node is NBadEndSkillButton button)
        {
            return button;
        }

        foreach (Node child in combatUi.GetChildren())
        {
            if (child is NBadEndSkillButton match)
            {
                return match;
            }
        }

        return null;
    }

    private static Player? TryResolveLocalPlayer(ICombatState state)
    {
        try
        {
            return LocalContext.GetMe(state);
        }
        catch (Exception)
        {
            return state.Players.Count == 1 ? state.Players[0] : null;
        }
    }
}

