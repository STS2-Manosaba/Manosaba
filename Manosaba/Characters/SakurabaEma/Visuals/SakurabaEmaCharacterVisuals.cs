using Godot;
using Manosaba.Characters.Common.Overrides;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace manosaba.Characters.SakurabaEma.Visuals;

public sealed partial class SakurabaEmaCharacterVisuals : ManosabaCharacterVisuals
{
    public override void _Ready()
    {
        base._Ready();

        var portrait = GetNode<Sprite2D>("Visuals/Majoka");
        float restingY = portrait.Position.Y;
        var hover = portrait.CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        hover.TweenProperty(portrait, "position:y", restingY - 4f, 1.4);
        hover.TweenProperty(portrait, "position:y", restingY, 1.4);
    }

    protected override bool IsCreatureVisible(Creature creature) => creature.IsAlive;

    protected override bool UsesMajokaPortrait(Creature creature) => creature.HasPower<SpecialMagicPower>();
}
