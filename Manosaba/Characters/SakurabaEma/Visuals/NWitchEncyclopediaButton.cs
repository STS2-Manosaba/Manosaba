using Godot;
using manosaba.Characters.SakurabaEma.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace manosaba.Characters.SakurabaEma.Visuals;

public sealed partial class NWitchEncyclopediaButton : TextureButton
{
    private Player _player = null!;
    private NCombatUi _combatUi = null!;
    private Label _count = null!;
    private NCardPileScreen? _openScreen;

    public void Initialize(Player player, NCombatUi ui)
    {
        _player = player;
        _combatUi = ui;
    }

    public override void _Ready()
    {
        TextureNormal = ResourceLoader.Load<Texture2D>("res://Manosaba/images/ui/combat/witch_encyclopedia.png");
        IgnoreTextureSize = true;
        StretchMode = StretchModeEnum.KeepAspectCentered;
        Size = new Vector2(80, 80);
        CustomMinimumSize = Size;
        FocusMode = FocusModeEnum.All;
        _count = new Label
        {
            Position = new Vector2(-14, 42), Size = new Vector2(48, 38),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore,
        };
        _count.AddThemeFontSizeOverride("font_size", 26);
        _count.AddThemeColorOverride("font_color", new Color("fff6e2"));
        _count.AddThemeColorOverride("font_outline_color", new Color("883146"));
        _count.AddThemeConstantOverride("outline_size", 8);
        AddChild(_count);
        Pressed += OpenPile;
        MouseEntered += ShowTip;
        MouseExited += HideTip;
        FocusEntered += ShowTip;
        FocusExited += HideTip;
        Refresh();
    }

    public override void _Process(double delta) => Refresh();

    private void Refresh()
    {
        if (_player.Creature.IsDead || CombatManager.Instance.IsOverOrEnding)
        {
            ClosePile();
            Visible = false;
            return;
        }
        var state = WitchEncyclopediaState.Get(_player);
        Visible = state?.HasReceivedEvidence == true && _combatUi.DiscardPile.Visible;
        if (!Visible)
            return;
        _count.Text = state!.Pile.Cards.Count.ToString();
        // Between the discard (bottom) and exhaust (185 units above it).
        GlobalPosition = _combatUi.DiscardPile.GlobalPosition + new Vector2(0, -92);
    }

    private void OpenPile()
    {
        HideTip();
        if (_player.Creature.IsDead || CombatManager.Instance.IsOverOrEnding)
            return;
        if (WitchEncyclopediaState.Get(_player) is { } state)
        {
            var screen = NCardPileScreen.ShowScreen(state.Pile, []);
            _openScreen = screen;
            var title = new Label
            {
                Text = new LocString("static_hover_tips", "MANOSABA-WITCH_ENCYCLOPEDIA.title").GetFormattedText(),
                Position = new Vector2(0, 20), Size = new Vector2(screen.Size.X, 44),
                HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore,
            };
            title.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
            title.OffsetTop = 20;
            title.OffsetBottom = 64;
            title.AddThemeFontSizeOverride("font_size", 32);
            screen.AddChild(title);
        }
    }

    private void ShowTip()
    {
        // MouseEntered and FocusEntered can both fire for the same button.
        // The tooltip registry permits only one entry per owner.
        NHoverTipSet.Remove(this);
        var tip = NHoverTipSet.CreateAndShow(this, WitchEncyclopediaState.HoverTip);
        if (tip != null)
            tip.GlobalPosition = _combatUi.DiscardPile.GlobalPosition + new Vector2(-320f, -370f);
    }
    private void HideTip() => NHoverTipSet.Remove(this);
    private void ClosePile()
    {
        // Close through the container so its backstop, focus and pause state reset.
        // Never close a different screen that replaced our encyclopedia.
        if (_openScreen != null && NCapstoneContainer.Instance is { } container
            && ReferenceEquals(container.CurrentCapstoneScreen, _openScreen))
            container.Close();
        _openScreen = null;
    }

    public override void _ExitTree()
    {
        ClosePile();
        HideTip();
    }
}
