using Godot;
using Manosaba.Combat.Emotes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Manosaba.Combat;

public sealed partial class NFieldPowerDisplay : VBoxContainer
{
    private const float GapBelowEmotePicker = 12f;
    private ICombatState _field = null!;
    private readonly Dictionary<FieldPowerModel, (HBoxContainer Row, Label Amount)> _rows = [];

    public void Initialize(ICombatState field) => _field = field;

    public override void _Ready()
    {
        // Follow the sticker picker at the top right, including its expanded panel.
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        CustomMinimumSize = new Vector2(96f, 48f);
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 8);
        Refresh();
    }

    private void AddPowerRow(FieldPowerModel power)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(96f, 48f),
            MouseFilter = MouseFilterEnum.Stop
        };
        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(48f, 48f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        icon.Texture = power.Icon;
        var amount = new Label { MouseFilter = MouseFilterEnum.Ignore };
        amount.AddThemeFontSizeOverride("font_size", 28);
        row.AddChild(icon);
        row.AddChild(amount);
        row.MouseEntered += () => ShowTooltip(row, power);
        row.MouseExited += () => NHoverTipSet.Remove(row);
        AddChild(row);
        _rows.Add(power, (row, amount));
    }

    public override void _Process(double delta) => Refresh();

    private void Refresh()
    {
        var powers = FieldPowerState.Get(_field);
        Visible = powers.Any(power => power.Amount > 0 || power.ShowAtZero);
        if (!Visible)
            return;
        foreach (var power in powers)
        {
            if (!_rows.ContainsKey(power))
                AddPowerRow(power);
            var (row, amount) = _rows[power];
            row.Visible = power.Amount > 0 || power.ShowAtZero;
            amount.Text = power.StackType == PowerStackType.Single ? "" : power.DisplayAmount.ToString();
        }
        if (GetParent() is Control combatUi)
            GlobalPosition = EmotePickerUi.GetPositionBelowPicker(combatUi, Size, GapBelowEmotePicker);
    }

    private static void ShowTooltip(Control row, FieldPowerModel power)
    {
        // PowerModel.HoverTips accesses Owner. Build the field tooltip directly instead.
        var description = new LocString("powers", $"{power.Id.Entry}.smartDescription");
        description.Add("Amount", power.Amount);
        var tip = new HoverTip(new LocString("powers", $"{power.Id.Entry}.title"), description, power.Icon);
        var node = NHoverTipSet.CreateAndShow(row, tip);
        if (node != null)
            node.GlobalPosition = new Vector2(
                Mathf.Max(0f, row.GlobalPosition.X + row.Size.X - node.Size.X),
                row.GlobalPosition.Y + row.Size.Y + 12f);
    }

    public override void _ExitTree()
    {
        foreach (var (row, _) in _rows.Values)
            NHoverTipSet.Remove(row);
    }
}
