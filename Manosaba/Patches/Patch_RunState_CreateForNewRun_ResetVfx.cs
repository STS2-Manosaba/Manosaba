using HarmonyLib;
using Manosaba.Characters.HikamiMeruru.Cards;
using Manosaba.Characters.TachibanaSherry.Cards;
using Manosaba.Characters.NikaidoHiro.Cards;
using Manosaba.Characters.HasumiLeia.Cards;
using manosaba.Characters.SawatariCoco.Cards;
using manosaba.Characters.NatsumeAnan.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Patches;

[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class Patch_RunState_CreateForNewRun_ResetVfx
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        LaboursOfHiro.ResetVfxForNewRun();
        HikamiMeruruExaid.ResetSfxForNewRun();
        SearchAnalyzeSolve.ResetSfxForNewRun();
        NovelSettingNoahFriendsSfx.ResetSfxForNewRun();
        CeremonialSword.ResetSfxForNewRun();
        IAmReborn.ResetSfxForNewRun();
        Pride.ResetSfxForNewRun();
        InmuKing.ResetBgmForNewRun();
        manosaba.Characters.SakurabaEma.Combat.EmaTrialPresentation.ResetForNewRun();
    }
}
