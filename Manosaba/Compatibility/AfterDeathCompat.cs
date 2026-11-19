using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;

namespace Manosaba.Compatibility;

/// <summary>Stable callback contract for game versions that changed AbstractModel.AfterDeath.</summary>
public interface IManosabaAfterDeathHandler
{
    Task HandleAfterDeath(PlayerChoiceContext context, Creature creature, bool wasRemovalPrevented, float deathAnimLength);
}

[HarmonyPatch]
internal static class Patch_AbstractModel_AfterDeathCompat
{
    private static MethodBase? TargetMethod() => AccessTools.DeclaredMethod(typeof(AbstractModel), "AfterDeath");

    private static bool Prefix(AbstractModel __instance, object[] __args, ref Task __result)
    {
        if (__instance is not IManosabaAfterDeathHandler handler)
        {
            return true;
        }

        PlayerChoiceContext? context = __args.OfType<PlayerChoiceContext>().FirstOrDefault();
        Creature? creature = __args.OfType<Creature>().FirstOrDefault();
        bool? wasRemovalPrevented = __args.OfType<bool>().Cast<bool?>().FirstOrDefault();
        float? deathAnimLength = __args.OfType<float>().Cast<float?>().FirstOrDefault();
        if (context == null || creature == null || !wasRemovalPrevented.HasValue || !deathAnimLength.HasValue)
        {
            throw new InvalidOperationException("Could not map the current AbstractModel.AfterDeath signature.");
        }

        __result = handler.HandleAfterDeath(context, creature, wasRemovalPrevented.Value, deathAnimLength.Value);
        return false;
    }
}
