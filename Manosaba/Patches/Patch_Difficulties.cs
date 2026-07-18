using BaseLib.Utils;
using HarmonyLib;
using Manosaba.Multiplayer;
using Manosaba.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using System.Runtime.CompilerServices;

namespace Manosaba.Patches;

/// <summary>
/// 大廳難度「敵人 HP 倍率」在戰鬥中的套用與修補。
/// <para>
/// 流程概念：玩家在連線大廳選的敵方 HP% 存在 <see cref="ManosabaLobbyDifficultyState"/>；進戰鬥後 vanilla 會依
/// 遭遇、多人與Act先做一次 <see cref="Creature.ScaleHpForMultiplayer"/>，本檔再在多個進入點把「最終敵方 HP」乘上大廳倍率，
/// 並處理只改 max、只改 current、復活補血等邊界，避免 current HP 卡在舊設計值、或無限血首領（如 Doormaker）被錯誤放大。
/// </para>
/// <para>
/// 使用 <see cref="ConditionalWeakTable{TKey,TValue}"/> 避免同一隻怪在 <see cref="Creature.AfterAddedToRoom"/> 被重複縮放；
/// 新開一局時於 <see cref="RunState.CreateForNewRun"/> 清快取並凍結本局大廳快照。
/// </para>
/// </summary>
public static class Patch_Difficulties
{
    private const decimal MinCreatureHp = 1m;
    private static readonly decimal MaxCreatureHp = int.MaxValue;

    private static ConditionalWeakTable<Creature, object> _hpAppliedAfterAdded = new();
    private static readonly object _hpAppliedMarker = new();

    /// <summary>大廳敵方 HP 倍率已啟用，且該單位為仍在戰鬥中的敵人。</summary>
    /// <remarks>供「只修正 current HP 與設計 max 不一致」類 patch 使用：須在戰鬥中且倍率≠1。</remarks>
    private static bool IsLobbyEnemyHpScalingCreature(Creature creature)
    {
        return creature.IsEnemy
            && ManosabaCombatCompat.HasCombatState(creature)
            && ManosabaLobbyDifficultyState.GetEnemyHpMultiplierForGameplay() != 1m;
    }

    /// <summary>活著的敵人且大廳倍率≠1 時，於進房後等路徑套用 HP 縮放。</summary>
    private static bool ShouldScaleEnemyHp(Creature creature)
    {
        return creature.IsEnemy
            && !creature.IsDead
            && ManosabaLobbyDifficultyState.GetEnemyHpMultiplierForGameplay() != 1m;
    }

    /// <summary>
    /// 與 <see cref="ShouldScaleEnemyHp"/> 相同條件（活敵），並含復活類流程（例如實驗體多階段）：
    /// 在 <c>CurrentHp == 0</c> 時仍會呼叫 <see cref="CreatureCmd.SetMaxHp"/> 的情況。
    /// </summary>
    private static bool ShouldScaleEnemySetMaxHp(Creature creature, decimal amount)
    {
        if (ManosabaLobbyDifficultyState.GetEnemyHpMultiplierForGameplay() == 1m)
        {
            return false;
        }

        if (!creature.IsEnemy || !ManosabaCombatCompat.HasCombatState(creature))
        {
            return false;
        }

        if (ShouldScaleEnemyHp(creature))
        {
            return true;
        }

        return creature.IsDead && amount > 0m;
    }

    /// <summary>
    /// 復活流程中，<see cref="CreatureCmd.SetMaxHp"/> 之後的 <see cref="CreatureCmd.Heal"/> 可能仍帶未乘大廳倍率的數值；
    /// 僅在目標仍視為死亡（0 HP）時縮放補量，一般戰鬥中的治療不受影響。
    /// </summary>
    private static bool ShouldScaleEnemyHealForRevive(Creature creature, decimal amount)
    {
        if (ManosabaLobbyDifficultyState.GetEnemyHpMultiplierForGameplay() == 1m)
        {
            return false;
        }

        if (!creature.IsEnemy || !ManosabaCombatCompat.HasCombatState(creature))
        {
            return false;
        }

        // 特例：幻象（IllusionPower）的復活補血量是由「已縮放的 MaxHp」推導而來（ReviveMove healing MaxHp - CurrentHp），
        // 本身已含大廳倍率；若再乘一次會重複縮放（治療跳字灌水；倍率<1 時甚至補不滿）。此類怪跳過，其餘復活怪（實驗體、
        // MockRevive 等傳入未乘倍率補量者）維持原本縮放。
        if (creature.HasPower<IllusionPower>())
        {
            return false;
        }

        return creature.IsDead && amount > 0m;
    }

    /// <summary>將原始 HP 乘上大廳敵方倍率，四捨五入並限制在合法範圍內。</summary>
    private static decimal ScaleEnemyHp(decimal rawHp)
    {
        decimal multiplier = ManosabaLobbyDifficultyState.GetEnemyHpMultiplierForGameplay();
        decimal scaled;
        try
        {
            scaled = Math.Round(rawHp * multiplier, MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            return MaxCreatureHp;
        }

        if (scaled < MinCreatureHp)
        {
            return MinCreatureHp;
        }

        if (scaled > MaxCreatureHp)
        {
            return MaxCreatureHp;
        }

        return scaled;
    }

    /// <summary>
    /// 怪進房後第一次機會：把敵方 max 與 current HP 一併乘上大廳敵方倍率（與 vanilla 既有乘法疊加）。
    /// </summary>
    /// <remarks>跳過無限血顯示階段（main: ShowsInfiniteHp / beta: HpDisplay），避免被放大，造成連線 checksum 不一致。</remarks>
    [HarmonyPatch(typeof(Creature), nameof(Creature.AfterAddedToRoom))]
    private static class Patch_Creature_AfterAddedToRoom_HpMultiplier
    {
        private static void Postfix(Creature __instance)
        {
            if (!ShouldScaleEnemyHp(__instance))
            {
                return;
            }

            // Doormaker／瀑布式「無限血」階段：不在此重複乘大廳倍率；放大極大 max HP 無意義且易造成連線 checksum 不一致。
            // 有限生命階段改由本檔對 CreatureCmd 等路徑的 patch 處理。
            if (__instance.HpDisplay.IsInfinite())
            {
                return;
            }

            if (_hpAppliedAfterAdded.TryGetValue(__instance, out _))
            {
                return;
            }

            _hpAppliedAfterAdded.Add(__instance, _hpAppliedMarker);

            int newMaxHp = (int)ScaleEnemyHp(__instance.MaxHp);
            __instance.SetMaxHpInternal(newMaxHp);
            __instance.SetCurrentHpInternal(newMaxHp);
        }
    }

    /// <summary>
    /// 攔截 <see cref="CreatureCmd.SetMaxHp"/>：任何把敵方 max 設成「未乘大廳倍率」的數值時，先乘倍率再寫入。
    /// </summary>
    /// <remarks>含復活類流程（當下可能已死亡但 <c>amount &gt; 0</c>），條件與 <see cref="ShouldScaleEnemySetMaxHp"/> 一致。</remarks>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.SetMaxHp))]
    private static class Patch_CreatureCmd_SetMaxHp_HpMultiplier
    {
        private static void Prefix(Creature creature, ref decimal amount)
        {
            if (!ShouldScaleEnemySetMaxHp(creature, amount))
            {
                return;
            }

            amount = ScaleEnemyHp(amount);
        }
    }

    /// <summary>
    /// 與 <see cref="Patch_CreatureCmd_SetMaxHp_HpMultiplier"/> 對稱：<see cref="CreatureCmd.SetMaxAndCurrentHp"/> 會用同一個
    /// 「已乘多人、未乘大廳」的數值先設 max 再設 current；若只 patch SetMaxHp，current 會卡在 50/76 這類狀態。
    /// 典型案例：Ovicopter 的 ToughEgg 孵化（<see cref="MegaCrit.Sts2.Core.Models.Monsters.ToughEgg.Hatch"/>）。
    /// </summary>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.SetCurrentHp))]
    private static class Patch_CreatureCmd_SetCurrentHp_HpMultiplier
    {
        private static void Prefix(Creature creature, ref decimal amount)
        {
            if (!ShouldScaleEnemySetMaxHp(creature, amount))
            {
                return;
            }

            amount = ScaleEnemyHp(amount);
        }
    }

    /// <summary>
    /// 部分流程在 max 已乘大廳倍率後，仍直接呼叫 <see cref="Creature.SetCurrentHpInternal"/> 並帶入未乘倍率的數值。
    /// 若該數值乘大廳倍率後等於目前 max，則改寫為 max，避免 current 低於已縮放的 max（兜底，主要路徑見 SetCurrentHp patch）。
    /// </summary>
    [HarmonyPatch(typeof(Creature), nameof(Creature.SetCurrentHpInternal))]
    private static class Patch_Creature_SetCurrentHpInternal_LobbyDesignCapSync
    {
        private static void Prefix(Creature __instance, ref decimal amount)
        {
            if (!IsLobbyEnemyHpScalingCreature(__instance) || __instance.Monster == null)
            {
                return;
            }

            int maxHp = __instance.MaxHp;
            if (maxHp <= 0 || amount >= maxHp)
            {
                return;
            }

            if ((int)ScaleEnemyHp(amount) != maxHp)
            {
                return;
            }

            amount = maxHp;
        }
    }

    /// <summary>
    /// max 提高時，vanilla 僅相當於 <c>CurrentHp = Min(CurrentHp, MaxHp)</c>；若提高前已滿血，不會自動補到新 max。
    /// 此處在偵測到「舊 max 滿血、新 max 變大」時，將 current HP 補至新 max（涵蓋未再以正確數值呼叫 <see cref="Creature.SetCurrentHpInternal"/> 的召喚物等）。
    /// </summary>
    /// <remarks>與大廳敵方 HP 倍率一併拉高 max 時特別需要。</remarks>
    [HarmonyPatch(typeof(Creature), nameof(Creature.SetMaxHpInternal))]
    private static class Patch_Creature_SetMaxHpInternal_LobbyCurrentTopUp
    {
        /// <summary>記錄 <see cref="Creature.SetMaxHpInternal"/> 呼叫前後狀態，供後置 patch 決定是否補滿 current HP。</summary>
        private struct TopUpState
        {
            /// <summary>是否為應追蹤的敵人（大廳倍率路徑）。</summary>
            public bool Track;
            /// <summary>變更前的 max HP。</summary>
            public int OldMaxHp;
            /// <summary>變更前的 current HP。</summary>
            public int OldCurrentHp;
        }

        private static void Prefix(Creature __instance, ref decimal amount, ref TopUpState __state)
        {
            __state = default;
            if (!IsLobbyEnemyHpScalingCreature(__instance))
            {
                return;
            }

            __state.Track = true;
            __state.OldMaxHp = __instance.MaxHp;
            __state.OldCurrentHp = __instance.CurrentHp;
        }

        private static void Postfix(Creature __instance, ref decimal amount, ref TopUpState __state)
        {
            if (!__state.Track)
            {
                return;
            }

            if (__instance.MaxHp <= __state.OldMaxHp)
            {
                return;
            }

            if (__state.OldMaxHp <= 0 || __state.OldCurrentHp < __state.OldMaxHp)
            {
                return;
            }

            if (__instance.CurrentHp < __instance.MaxHp)
            {
                __instance.SetCurrentHpInternal(__instance.MaxHp);
            }
        }
    }

    /// <summary>
    /// 復活路線上 <see cref="CreatureCmd.Heal"/> 仍可能帶「未乘大廳倍率」的補量；在目標仍為死亡狀態（0 HP）時對補量乘倍率。
    /// </summary>
    /// <remarks>一般戰鬥中補血不走此分支，避免影響正常治療量。</remarks>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
    private static class Patch_CreatureCmd_Heal_HpMultiplierRevive
    {
        private static void Prefix(Creature creature, ref decimal amount)
        {
            if (!ShouldScaleEnemyHealForRevive(creature, amount))
            {
                return;
            }

            amount = ScaleEnemyHp(amount);
        }
    }

    /// <summary>
    /// 新開一局：清大廳難度快取、重置「進房已套用 HP 倍率」弱表，再凍結本局要用的大廳敵方 HP 倍率。
    /// </summary>
    [HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
    private static class Patch_RunState_CreateForNewRun_ResetEnemyHpMultiplierCache
    {
        private static void Prefix()
        {
            ManosabaLobbyDifficultyState.ClearRunSnapshot();
        }

        private static void Postfix()
        {
            _hpAppliedAfterAdded = new ConditionalWeakTable<Creature, object>();
            ManosabaLobbyDifficultyState.FreezeForRun();
        }
    }
}
