using System.Reflection;
using HarmonyLib;
using Manosaba.Characters.Common.Overrides;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Patches;

// Runtime-added Trial must have the same damage, Block, and display rules as native Trial cards.
internal static class AttachedTrialRules
{
    internal static bool IsAttachedTrial(CardModel? card) => card != null &&
        card.Keywords.Contains(ManosabaKeywords.Trial) && !card.CanonicalKeywords.Contains(ManosabaKeywords.Trial);
    internal static ValueProp Apply(CardModel? card, ValueProp props) =>
        IsAttachedTrial(card) && card!.Owner?.Creature?.HasPower<LawDevilPower>() != true
            ? props | ValueProp.Unpowered : props;
}

// Apply before BeforeAttack listeners, including Vigor, inspect the command.
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeAttack))]
public static class Patch_Hook_BeforeAttack_AttachedTrial
{
    [HarmonyPrefix]
    private static void Prefix(AttackCommand command)
    {
        if ((AttachedTrialRules.Apply(command.ModelSource as CardModel, command.DamageProps) & ValueProp.Unpowered) != 0)
            command.Unpowered();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
public static class Patch_Hook_ModifyDamage_AttachedTrial
{
    [HarmonyPrefix]
    private static void Prefix(CardModel? cardSource, ref ValueProp props) => props = AttachedTrialRules.Apply(cardSource, props);
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyBlock))]
public static class Patch_Hook_ModifyBlock_AttachedTrial
{
    [HarmonyPrefix]
    private static void Prefix(CardModel? cardSource, ref ValueProp props) => props = AttachedTrialRules.Apply(cardSource, props);
}

[HarmonyPatch]
public static class Patch_CardModel_Description_AttachedTrial
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CardModel)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => m.Name == "GetDescriptionForPile");
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (AttachedTrialRules.IsAttachedTrial(__instance))
            __result = "[gold]" + new LocString("card_keywords", "MANOSABA-TRIAL.title").GetFormattedText() + "[/gold]\n" + __result;
    }
}
