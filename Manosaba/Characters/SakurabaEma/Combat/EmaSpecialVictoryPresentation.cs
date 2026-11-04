using System.Runtime.CompilerServices;
using Godot;
using Manosaba.Audio;
using Manosaba.Config;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace manosaba.Characters.SakurabaEma.Combat;

internal static class EmaSpecialVictoryPresentation
{
    private const string SceneDirectory = "res://Manosaba/scenes/vfx/ema_special_victory/";
    private const string KillEmaBgm = "event:/Manosaba/audio/bgm/EmA\u00a0(-危-).mp3";
    private const string DieBgm = "event:/Manosaba/audio/bgm/bloom\u00a0(-Piano\u00a0version-).mp3";
    private const float BgmVolume = 0.8f;
    private static readonly HashSet<string> MusicPlayedThisRun = [];
    private static readonly ConditionalWeakTable<ICombatState, HashSet<string>> MusicPlayedInCombat = new();

    internal static void ResetForNewRun() => MusicPlayedThisRun.Clear();

    internal static Task PlayKillEma() => Play("kill_ema.tscn");

    internal static Task PlayDie() => Play("die.tscn", duckBgm: true);

    internal static void TryPlayKillEmaMusic(ICombatState combat) =>
        TryPlayMusic(combat, KillEmaBgm, ManosabaConfig.KillEmaVictoryBgmFrequency);

    internal static void TryPlayDieMusic(ICombatState combat) =>
        TryPlayMusic(combat, DieBgm, ManosabaConfig.DieVictoryBgmFrequency);

    private static void TryPlayMusic(ICombatState combat, string bgm, ManosabaFxPlayMode mode)
    {
        if (!TestMode.IsOff || NCombatRoom.Instance is not { } room || !room.IsInsideTree())
            return;

        HashSet<string> played = MusicPlayedInCombat.GetOrCreateValue(combat);
        if (!played.Contains(bgm) && ClaimMusic(bgm, mode))
        {
            played.Add(bgm);
            SfxCmd.Play(bgm, BgmVolume);
        }
    }

    private static bool ClaimMusic(string bgm, ManosabaFxPlayMode mode)
    {
        if (mode == ManosabaFxPlayMode.Never)
            return false;

        bool firstPlay = MusicPlayedThisRun.Add(bgm);
        return mode == ManosabaFxPlayMode.EveryTime || firstPlay;
    }

    private static async Task Play(string sceneFile, bool duckBgm = false)
    {
        if (!TestMode.IsOff || NCombatRoom.Instance is not { } room || !room.IsInsideTree())
            return;

        string scenePath = SceneDirectory + sceneFile;
        if (ResourceLoader.Load<PackedScene>(scenePath) is not { } scene)
        {
            GD.PushWarning("[Manosaba] Missing Ema victory scene: " + scenePath);
            return;
        }

        SceneTree tree = room.GetTree();
        var effect = scene.Instantiate<CanvasLayer>();
        effect.Set("audio_volume", ManosabaConfig.GetManosabaSfxVolume());

        if (duckBgm)
            GodotSfxRouter.SetCustomBgmVoiceDucking(true);
        try
        {
            room.AddChild(effect);
            ulong started = Time.GetTicksMsec();
            // Real-time waits keep the voice and fades synchronized in every combat speed mode.
            while (GodotObject.IsInstanceValid(effect) && effect.IsInsideTree()
                && !CombatManager.Instance.IsOverOrEnding && Time.GetTicksMsec() - started < 30000)
            {
                SceneTreeTimer tick = tree.CreateTimer(0.05, ignoreTimeScale: true);
                await tree.ToSignal(tick, SceneTreeTimer.SignalName.Timeout);
            }
        }
        finally
        {
            if (duckBgm)
                GodotSfxRouter.SetCustomBgmVoiceDucking(false);
            if (GodotObject.IsInstanceValid(effect) && !effect.IsQueuedForDeletion())
                effect.QueueFree();
        }
    }
}
