using Godot;
using Manosaba.Audio;
using Manosaba.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace manosaba.Characters.SakurabaEma.Combat;

public static class InterrogationStartPresentation
{
    public const string ScenePath = "res://Manosaba/scenes/vfx/interrogation_start/interrogation_start.tscn";
    public const string BgmEvent = "event:/Manosaba/audio/bgm/rAwTell\u00a0Owk....mp3";
    private static bool _musicPlayedThisRun;

    public static void ResetForNewRun() => _musicPlayedThisRun = false;

    public static bool ClaimMusic(ManosabaFxPlayMode mode)
    {
        if (mode == ManosabaFxPlayMode.Never || (mode == ManosabaFxPlayMode.OncePerRun && _musicPlayedThisRun))
            return false;
        _musicPlayedThisRun = true;
        return true;
    }

    public static async Task Play()
    {
        if (!TestMode.IsOff || NCombatRoom.Instance is not { } room || !room.IsInsideTree())
            return;

        if (ClaimMusic(ManosabaConfig.InterrogationStartMusicFrequency))
            GodotSfxRouter.TryPlay(BgmEvent);

        PackedScene? scene = ResourceLoader.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            GD.PushWarning("[Manosaba] Interrogation start scene is missing. Re-export Manosaba.pck.");
            return;
        }

        var effect = scene.Instantiate<CanvasLayer>();
        room.AddChild(effect);
        ulong started = Time.GetTicksMsec();
        try
        {
            while (GodotObject.IsInstanceValid(effect) && effect.IsInsideTree()
                && GodotObject.IsInstanceValid(room) && room.IsInsideTree()
                && Time.GetTicksMsec() - started < 6500)
                await Cmd.Wait(0.05f);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(effect) && !effect.IsQueuedForDeletion())
                effect.QueueFree();
        }
    }
}
