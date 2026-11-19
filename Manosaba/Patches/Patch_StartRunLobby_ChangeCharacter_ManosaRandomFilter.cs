using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace Manosaba.Patches;

/// <summary>
/// 單機／多人出發時，<see cref="StartRunLobby.BeginRunLocally"/> 會從 <see cref="ModelDb.AllCharacters"/>
/// 抽籤並直接呼叫私用方法 <c>ChangeCharacter</c>，<b>不會</b>經過 <c>SetLocalCharacter</c>。
/// </summary>
/// <remarks>
/// 僅在 <c>isRandomCharacterResolution</c> 為真時處理（出發時種子隨機結算）。須對每位 <c>playerId</c> 套用替換；
/// <see cref="StartRunLobby.SetLocalCharacter"/> 呼叫的 <c>ChangeCharacter</c> 帶 false，改由另一個 Prefix 處理。
/// </remarks>
[HarmonyPatch]
public static class Patch_StartRunLobby_ChangeCharacter_ManosaRandomFilter
{
    public static bool Prepare() => TargetMethod() != null;

    private static MethodBase? TargetMethod() =>
        AccessTools.DeclaredMethod(
            typeof(StartRunLobby),
            "ChangeCharacter",
            new[] { typeof(ulong), typeof(CharacterModel), typeof(bool) });

    [HarmonyPrefix]
    private static void Prefix(
        StartRunLobby __instance,
        ulong playerId,
        ref CharacterModel character)
    {
        if (!StartRunLobbyRandomResolutionScope.IsActive)
        {
            return;
        }

        StartRunLobbyPlayer target = __instance.Players.FirstOrDefault(p =>
            ManosabaRandomCharacterPool.ReadLobbyPlayerId(p) == playerId);
        if (target.id != playerId)
        {
            return;
        }

        ManosabaRandomCharacterPool.TryReplaceVanillaPickWhenRandomSlot(
            ref character,
            ManosabaRandomCharacterPool.ReadLobbyPlayerCharacter(target));
    }
}
