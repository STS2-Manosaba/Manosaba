using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Manosaba.Compatibility;

/// <summary>
/// Applies Manosaba's hidden-character rules to screens that build their own character buttons.
/// BaseLib currently handles visibility on NCharacterSelectScreen, but not CustomRun, Blitz, or Dread.
/// </summary>
internal static class BetaCharacterSelectPatches
{
    private static readonly string[] ScreenTypeNames =
    [
        "MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzScreen",
        "MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen",
    ];

    private const string CustomRunScreenTypeName =
        "MegaCrit.Sts2.Core.Nodes.Screens.CustomRun.NCustomRunScreen";

    private const string AscensionHelperTypeName =
        "MegaCrit.Sts2.Core.Helpers.AscensionHelper";

    private static readonly MethodInfo AllCharactersGetter =
        AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));

    private static readonly MethodInfo VisibleCharactersMethod =
        AccessTools.DeclaredMethod(typeof(BetaCharacterSelectPatches), nameof(GetVisibleCharacters));

    private static readonly MethodInfo RandomEligibleCharactersMethod =
        AccessTools.DeclaredMethod(typeof(BetaCharacterSelectPatches), nameof(GetRandomEligibleCharacters));

    internal static void Install(Harmony harmony)
    {
        foreach (string typeName in ScreenTypeNames)
        {
            Type? screenType = AccessTools.TypeByName(typeName);
            MethodInfo? initMethod = screenType == null ? null : AccessTools.Method(screenType, "InitCharacterButtons");
            if (initMethod == null)
            {
                continue;
            }

            try
            {
                harmony.Patch(
                    initMethod,
                    postfix: new HarmonyMethod(typeof(BetaCharacterSelectPatches), nameof(AfterCharacterButtonsInitialized)),
                    transpiler: new HarmonyMethod(typeof(BetaCharacterSelectPatches), nameof(FilterHiddenCharacters)));

                PatchIfFound(
                    harmony,
                    AccessTools.Method(screenType, "UpdateRandomCharacterVisibility"),
                    transpiler: nameof(FilterRandomEligibleCharacters),
                    postfix: nameof(RefreshCharacterFocusNavigation));

                PatchIfFound(
                    harmony,
                    AccessTools.Method(screenType, "InitRotatingCharacterButton"),
                    postfix: nameof(RefreshCharacterFocusNavigation));
            }
            catch (Exception ex)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"Failed to install Manosaba character select compatibility on {typeName}: {ex.Message}");
            }
        }

        Type? customRunScreenType = AccessTools.TypeByName(CustomRunScreenTypeName);
        MethodInfo? customRunInitMethod = customRunScreenType == null
            ? null
            : AccessTools.Method(customRunScreenType, "InitCharacterButtons");
        if (customRunInitMethod != null)
        {
            try
            {
                harmony.Patch(
                    customRunInitMethod,
                    transpiler: new HarmonyMethod(typeof(BetaCharacterSelectPatches), nameof(FilterHiddenCharacters)));
            }
            catch (Exception ex)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"Failed to install Manosaba hidden-character filtering on custom run screen: {ex.Message}");
            }
        }

        Type? ascensionHelperType = AccessTools.TypeByName(AscensionHelperTypeName);
        MethodInfo? rotatingCharacterMethod = ascensionHelperType == null
            ? null
            : AccessTools.Method(ascensionHelperType, "GetRotatingCharacterIdForStreak");
        if (rotatingCharacterMethod != null)
        {
            try
            {
                harmony.Patch(
                    rotatingCharacterMethod,
                    transpiler: new HarmonyMethod(typeof(BetaCharacterSelectPatches), nameof(FilterHiddenCharacters)));
            }
            catch (Exception ex)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"Failed to exclude hidden characters from Dread rotation: {ex.Message}");
            }
        }

        // The beta screen initializes its random button through this method. BaseLib's random
        // eligibility transpiler currently targets the vanilla button initialization path only.
        PatchIfFound(
            harmony,
            AccessTools.Method(typeof(NCharacterSelectButton), "InitAsRandom"),
            transpiler: nameof(FilterRandomEligibleCharacters));
    }

    private static void PatchIfFound(
        Harmony harmony,
        MethodBase? target,
        string? transpiler = null,
        string? postfix = null)
    {
        if (target == null || (transpiler == null && postfix == null))
        {
            return;
        }

        harmony.Patch(
            target,
            postfix: postfix == null ? null : new HarmonyMethod(typeof(BetaCharacterSelectPatches), postfix),
            transpiler: transpiler == null ? null : new HarmonyMethod(typeof(BetaCharacterSelectPatches), transpiler));
    }

    private static IEnumerable<CodeInstruction> FilterHiddenCharacters(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (!instruction.Calls(AllCharactersGetter))
            {
                yield return instruction;
                continue;
            }

            var replacement = new CodeInstruction(OpCodes.Call, VisibleCharactersMethod);
            replacement.labels.AddRange(instruction.labels);
            replacement.blocks.AddRange(instruction.blocks);
            yield return replacement;
        }
    }

    private static IEnumerable<CharacterModel> GetVisibleCharacters()
    {
        foreach (CharacterModel character in ModelDb.AllCharacters)
        {
            if (character is CustomCharacterModel { HideFromVanillaCharacterSelect: true })
            {
                continue;
            }

            yield return character;
        }
    }

    private static IEnumerable<CodeInstruction> FilterRandomEligibleCharacters(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (!instruction.Calls(AllCharactersGetter))
            {
                yield return instruction;
                continue;
            }

            var replacement = new CodeInstruction(OpCodes.Call, RandomEligibleCharactersMethod);
            replacement.labels.AddRange(instruction.labels);
            replacement.blocks.AddRange(instruction.blocks);
            yield return replacement;
        }
    }

    private static IEnumerable<CharacterModel> GetRandomEligibleCharacters()
    {
        foreach (CharacterModel character in ModelDb.AllCharacters)
        {
            if (character is CustomCharacterModel { AllowInVanillaRandomCharacterSelect: false })
            {
                continue;
            }

            yield return character;
        }
    }

    private static void AfterCharacterButtonsInitialized(object __instance)
    {
        FieldInfo? containerField = AccessTools.Field(__instance.GetType(), "_charButtonContainer");
        if (containerField?.GetValue(__instance) is not Control container)
        {
            return;
        }

        List<NCharacterSelectButton> buttons = GetVisibleCharacterButtons(container);

        if (buttons.Count > 5)
        {
            MakeScrollable(container, buttons);
        }

        // Reparenting changes the buttons' relative paths, so navigation must be rebuilt afterward.
        SetFocusNeighbors(buttons);
    }

    private static void RefreshCharacterFocusNavigation(object __instance)
    {
        FieldInfo? containerField = AccessTools.Field(__instance.GetType(), "_charButtonContainer");
        if (containerField?.GetValue(__instance) is Control container)
        {
            List<NCharacterSelectButton> buttons = GetVisibleCharacterButtons(container);
            SetFocusNeighbors(buttons);
            UpdateScrollableContentSize(container, buttons);
        }
    }

    private static List<NCharacterSelectButton> GetVisibleCharacterButtons(Control container) => container.GetChildren()
            .OfType<NCharacterSelectButton>()
            .Where(button => button.Visible)
            .ToList();

    private static void SetFocusNeighbors(IReadOnlyList<NCharacterSelectButton> buttons)
    {
        for (int index = 0; index < buttons.Count; index++)
        {
            NCharacterSelectButton button = buttons[index];
            button.FocusNeighborTop = button.GetPath();
            button.FocusNeighborBottom = button.GetPath();
            button.FocusNeighborLeft = buttons[(index - 1 + buttons.Count) % buttons.Count].GetPath();
            button.FocusNeighborRight = buttons[(index + 1) % buttons.Count].GetPath();
        }
    }

    private static void MakeScrollable(Control buttonContainer, IReadOnlyList<NCharacterSelectButton> buttons)
    {
        if (buttonContainer.GetParent() is not Control parent || buttonContainer.GetParent() is NHorizontalScrollContainer)
        {
            return;
        }

        foreach (NCharacterSelectButton button in buttonContainer.GetChildren().OfType<NCharacterSelectButton>())
        {
            button.MouseFilter = Control.MouseFilterEnum.Pass;
        }

        // Read the viewport size before RemoveChild: once detached, buttonContainer is outside the tree and
        // GetViewportRect() logs '!is_inside_tree()' and returns an empty rect (forcing the 1800px fallback).
        float viewportWidth = parent.IsInsideTree() ? parent.GetViewportRect().Size.X : 0f;

        int index = buttonContainer.GetIndex();
        parent.RemoveChild(buttonContainer);

        NHorizontalScrollContainer scroll = NHorizontalScrollContainer.Create(
            "CharacterButtonScrollContainer",
            buttonContainer,
            control =>
            {
                float maxScrollWidth = viewportWidth > 0f ? MathF.Min(1800f, viewportWidth - 80f) : 1800f;
                float scrollWidth = MathF.Min(116f * buttons.Count, MathF.Max(1f, maxScrollWidth));

                control.AnchorLeft = 0.5f;
                control.AnchorTop = 0.5f;
                control.AnchorRight = 0.5f;
                control.AnchorBottom = 0.5f;
                control.OffsetLeft = -scrollWidth * 0.5f;
                control.OffsetTop = -177f;
                control.OffsetRight = scrollWidth * 0.5f;
                control.OffsetBottom = -10f;
                control.GrowHorizontal = Control.GrowDirection.Both;
                control.GrowVertical = Control.GrowDirection.Both;
                control.ClipContents = true;
            });

        parent.AddChild(scroll);
        parent.MoveChild(scroll, index);
        scroll.AddChild(buttonContainer);

        buttonContainer.AnchorLeft = 0f;
        buttonContainer.AnchorTop = 0f;
        buttonContainer.AnchorRight = 0f;
        buttonContainer.AnchorBottom = 0f;
        buttonContainer.Position = Vector2.Zero;
        buttonContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        UpdateScrollableContentSize(buttonContainer, buttons);

        scroll.InitFocusScrolling();
        scroll.CallDeferred(Node.MethodName.SetProcessInput, false);
    }

    private static void UpdateScrollableContentSize(Control buttonContainer, IReadOnlyList<NCharacterSelectButton> buttons)
    {
        if (buttonContainer.GetParent() is not NHorizontalScrollContainer || buttons.Count == 0)
        {
            return;
        }

        float contentWidth = buttons.Sum(button => MathF.Max(button.CustomMinimumSize.X, button.Size.X));
        contentWidth += buttonContainer.GetThemeConstant("separation", "HBoxContainer") * Math.Max(0, buttons.Count - 1);
        float contentHeight = MathF.Max(buttonContainer.CustomMinimumSize.Y, buttonContainer.Size.Y);
        contentHeight = MathF.Max(contentHeight, buttons.Max(button => MathF.Max(button.CustomMinimumSize.Y, button.Size.Y)));
        buttonContainer.CustomMinimumSize = new Vector2(contentWidth, contentHeight);
        buttonContainer.Size = new Vector2(contentWidth, contentHeight);
    }
}
