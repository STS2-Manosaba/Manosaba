using Godot;
using HarmonyLib;
using Manosaba.Extensions;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Orbs;

namespace Manosaba.Patches;

/// <summary>
/// v0.107 beta <see cref="NOrb.UpdateVisuals"/> expects a SpineSkeleton child on orb sprites.
/// Manosaba paint orbs use Sprite2D-only scenes — skip Spine and use the pre-beta scale tween path.
/// </summary>
[HarmonyPatch(typeof(NOrb), nameof(NOrb.UpdateVisuals))]
public static class Patch_NOrb_UpdateVisuals_ManosabaOrb
{
    private static readonly AccessTools.FieldRef<NOrb, Node2D?> SpriteRef =
        AccessTools.FieldRefAccess<NOrb, Node2D?>("_sprite");

    private static readonly AccessTools.FieldRef<NOrb, Control> VisualContainerRef =
        AccessTools.FieldRefAccess<NOrb, Control>("_visualContainer");

    private static readonly AccessTools.FieldRef<NOrb, MegaLabel> PassiveLabelRef =
        AccessTools.FieldRefAccess<NOrb, MegaLabel>("_passiveLabel");

    private static readonly AccessTools.FieldRef<NOrb, MegaLabel> EvokeLabelRef =
        AccessTools.FieldRefAccess<NOrb, MegaLabel>("_evokeLabel");

    private static readonly AccessTools.FieldRef<NOrb, TextureRect> OutlineRef =
        AccessTools.FieldRefAccess<NOrb, TextureRect>("_outline");

    private static readonly AccessTools.FieldRef<NOrb, Control> LabelContainerRef =
        AccessTools.FieldRefAccess<NOrb, Control>("_labelContainer");

    private static readonly AccessTools.FieldRef<NOrb, bool> IsLocalRef =
        AccessTools.FieldRefAccess<NOrb, bool>("_isLocal");

    private static readonly AccessTools.FieldRef<NOrb, Tween?> CurTweenRef =
        AccessTools.FieldRefAccess<NOrb, Tween?>("_curTween");

    [HarmonyPrefix]
    private static bool Prefix(NOrb __instance, bool isEvoking)
    {
        OrbModel? model = __instance.Model;
        if (model is not ManosabaOrbModel)
        {
            return true;
        }

        if (!__instance.IsNodeReady() || !CombatManager.Instance.IsInProgress)
        {
            return false;
        }

        Node2D? sprite = SpriteRef(__instance);
        MegaLabel passiveLabel = PassiveLabelRef(__instance);
        MegaLabel evokeLabel = EvokeLabelRef(__instance);
        TextureRect outline = OutlineRef(__instance);
        Control labelContainer = LabelContainerRef(__instance);
        bool isLocal = IsLocalRef(__instance);

        if (sprite == null)
        {
            sprite = model.CreateSprite();
            VisualContainerRef(__instance).AddChildSafely(sprite);
            sprite.Position = Vector2.Zero;
            SpriteRef(__instance) = sprite;

            CurTweenRef(__instance)?.Kill();
            Tween tween = __instance.CreateTween();
            CurTweenRef(__instance) = tween;
            tween.TweenProperty(sprite, "scale", Vector2.One, 0.5)
                .From(Vector2.Zero)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
        }

        outline.Visible = false;
        labelContainer.Visible = isLocal;

        if (!isLocal)
        {
            __instance.Modulate = model.DarkenedColor;
        }

        if (model is PlasmaOrb)
        {
            passiveLabel.Visible = false;
            evokeLabel.Visible = false;
        }
        else if (model is DarkOrb)
        {
            passiveLabel.Visible = true;
            evokeLabel.Visible = true;
            passiveLabel.SetTextAutoSize(model.PassiveVal.ToString("0"));
            evokeLabel.SetTextAutoSize(model.EvokeVal.ToString("0"));
        }
        else if (model is GlassOrb)
        {
            passiveLabel.Visible = !isEvoking;
            evokeLabel.Visible = isEvoking;
            sprite!.Modulate = model.PassiveVal == 0m ? model.DarkenedColor : Colors.White;
            passiveLabel.SetTextAutoSize(model.PassiveVal.ToString("0"));
            evokeLabel.SetTextAutoSize(model.EvokeVal.ToString("0"));
        }
        else
        {
            passiveLabel.Visible = !isEvoking;
            evokeLabel.Visible = isEvoking;
            passiveLabel.SetTextAutoSize(model.PassiveVal.ToString("0"));
            evokeLabel.SetTextAutoSize(model.EvokeVal.ToString("0"));
        }

        return false;
    }
}
