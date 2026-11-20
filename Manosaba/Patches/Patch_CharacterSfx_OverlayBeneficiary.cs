using HarmonyLib;
using Manosaba.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Patches;

file static class SmithRestSiteOptionOwner
{
    internal static ulong? NetId(SmithRestSiteOption option) =>
        Traverse.Create(option).Property<Player>("Owner").Value?.NetId;
}

[HarmonyPatch(typeof(PotionCmd), nameof(PotionCmd.TryToProcure), typeof(PotionModel), typeof(Player), typeof(int))]
public static class Patch_PotionCmd_TryToProcure_OverlayBeneficiary
{
    [HarmonyPrefix]
    private static void Prefix(Player player) =>
        CharacterSfxOverlayBeneficiary.Push(player.NetId);

    [HarmonyFinalizer]
    private static void Finalizer() =>
        CharacterSfxOverlayBeneficiary.Pop();
}

[HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain), typeof(RelicModel), typeof(Player), typeof(int))]
public static class Patch_RelicCmd_Obtain_OverlayBeneficiary
{
    [HarmonyPrefix]
    private static void Prefix(Player player) =>
        CharacterSfxOverlayBeneficiary.Push(player.NetId);

    [HarmonyFinalizer]
    private static void Finalizer() =>
        CharacterSfxOverlayBeneficiary.Pop();
}

[HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.DoLocalPostSelectVfx))]
public static class Patch_SmithRestSiteOption_DoLocalPostSelectVfx_OverlayBeneficiary
{
    [HarmonyPrefix]
    private static void Prefix(SmithRestSiteOption __instance) =>
        CharacterSfxOverlayBeneficiary.EnqueuePendingSmithCardSound(SmithRestSiteOptionOwner.NetId(__instance));
}

[HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.DoRemotePostSelectVfx))]
public static class Patch_SmithRestSiteOption_DoRemotePostSelectVfx_OverlayBeneficiary
{
    [HarmonyPrefix]
    private static void Prefix(SmithRestSiteOption __instance) =>
        CharacterSfxOverlayBeneficiary.EnqueuePendingSmithCardSound(SmithRestSiteOptionOwner.NetId(__instance));
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.LoseBlock))]
public static class Patch_CreatureCmd_LoseBlock_OverlayBeneficiary
{
    [HarmonyPrefix]
    private static void Prefix(Creature target) =>
        CharacterSfxOverlayBeneficiary.Push(target.Player?.NetId);

    [HarmonyFinalizer]
    private static void Finalizer() =>
        CharacterSfxOverlayBeneficiary.Pop();
}
