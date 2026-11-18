using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using Manosaba;
using Manosaba.Config;
using Manosaba.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace Manosaba.Patches;

/// <summary>
/// 「隨機」抽籤時篩選 Mod 自機。以執行時型別所屬 <see cref="Assembly"/> 是否為本 Mod DLL 判定，
/// 比依賴命名空間字串更可靠（避免 ModelDb / 產生型別與預期 namespace 不一致時池為空）。
/// </summary>
internal static class ManosabaRandomCharacterPool
{
    private static readonly Lazy<FieldInfo?> LobbyPlayerCharacterField = new(
        () => typeof(StartRunLobbyPlayer).GetField(
            "character",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

    private static readonly Lazy<FieldInfo?> LobbyPlayerIdField = new(
        () => typeof(StartRunLobbyPlayer).GetField(
            "id",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

    private static readonly Lazy<CharacterModel[]> SortedManosabaPool = new(
        () => BuildManosabaOnlyPool()
            .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
            .ToArray());

    private static readonly Lazy<Dictionary<ModelId, int>> AllCharacterIndexById = new(
        () => ModelDb.AllCharacters
            .Select((character, index) => new { character.Id, index })
            .ToDictionary(x => x.Id, x => x.index));

    private static Assembly ModAssembly => typeof(Entry).Assembly;

    /// <summary>參考組件中的 <see cref="LobbyPlayer"/> 未必公開 <c>character</c> 欄位，改以反射讀取。</summary>
    internal static CharacterModel? ReadLobbyPlayerCharacter(StartRunLobbyPlayer? player)
    {
        if (player is null)
        {
            return null;
        }

        return LobbyPlayerCharacterField.Value?.GetValue(player.Value) as CharacterModel;
    }

    internal static ulong? ReadLobbyPlayerId(StartRunLobbyPlayer? player)
    {
        if (player is null)
        {
            return null;
        }

        object? v = LobbyPlayerIdField.Value?.GetValue(player.Value);
        return v is ulong u ? u : null;
    }

    internal static bool IsManosabaPlayableCharacter(CharacterModel c) =>
        c is not RandomCharacter && ReferenceEquals(c.GetType().Assembly, ModAssembly);

    private static bool IsEligibleForManosabaRandom(CharacterModel c) =>
        IsManosabaPlayableCharacter(c)
        && c is not CustomCharacterModel { AllowInVanillaRandomCharacterSelect: false };

    internal static CharacterModel[] BuildManosabaOnlyPool()
    {
        return ModelDb.AllCharacters
            .Where(IsEligibleForManosabaRandom)
            .ToArray();
    }

    /// <summary>
    /// 「隨機」占位結算時修正原版抽籤結果：ManosabaOnly 模式改抽 Mod 自機。
    /// </summary>
    /// <remarks>
    /// 單人與多人皆用同一套映射：以「原版／UI 傳入的角色」在 <see cref="ModelDb.AllCharacters"/> 列舉中的索引，
    /// 對排序後的 Manosaba 池取模（與種子隨機在多人間一致，單機也不再額外使用 <c>Rng.Chaotic</c>）。
    /// 多人時由 HOST 在選角畫面同步 <see cref="RandomCharacterPoolMode"/>。
    /// </remarks>
    internal static void TryReplaceVanillaPickWhenRandomSlot(ref CharacterModel incoming, CharacterModel? slotCurrent)
    {
        if (slotCurrent is not RandomCharacter)
        {
            return;
        }

        if (incoming is RandomCharacter)
        {
            return;
        }

        if (ManosabaLobbyDifficultyState.GetRandomCharacterPoolModeForGameplay() != RandomCharacterPoolMode.ManosabaCharactersOnly)
        {
            return;
        }

        if (IsEligibleForManosabaRandom(incoming))
        {
            return;
        }

        TryRemapRandomPick(
            ref incoming,
            SortedManosabaPool.Value,
            "[Manosaba] RandomCharacterPool=ManosabaOnly but pool empty; vanilla random pick kept.");
    }

    private static void TryRemapRandomPick(ref CharacterModel incoming, CharacterModel[] pool, string emptyPoolMessage)
    {
        if (pool.Length == 0)
        {
            Log.Warn(emptyPoolMessage);
            return;
        }

        incoming = MapVanillaRandomPickToManosabaPool(incoming, pool);
    }

    /// <summary>
    /// 以與 <c>BeginRunLocally</c> 相同的 <see cref="ModelDb.AllCharacters"/> 列舉順序，將原版抽中的角色換成 Mod 池內對應項。
    /// </summary>
    private static CharacterModel MapVanillaRandomPickToManosabaPool(CharacterModel vanillaPick, CharacterModel[] poolSorted)
    {
        int idx = -1;
        if (AllCharacterIndexById.Value.TryGetValue(vanillaPick.Id, out int foundIndex))
        {
            idx = foundIndex;
        }

        if (idx < 0)
        {
            int h = StringComparer.Ordinal.GetHashCode(vanillaPick.Id.Entry ?? string.Empty);
            idx = (h & int.MaxValue) % poolSorted.Length;
        }

        return poolSorted[idx % poolSorted.Length];
    }
}
