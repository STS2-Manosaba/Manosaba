using Godot;
using Manosaba.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Manosaba.Characters.Common.Commands;

public static class RefuteCutInCmd
{
    public const string ScenePath = "res://Manosaba/scenes/vfx/refute_cutin/refute_cutin.tscn";

    /// <summary>Play and await a reusable cut-in. The default preserves the original test card.</summary>
    public static async Task Play(RefuteCutInVariant variant = RefuteCutInVariant.Ema3,
        string? voicePath = null, bool playAudio = true)
    {
        if (!Enum.IsDefined(variant))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown refute cut-in variant.");
        NCombatRoom? room = NCombatRoom.Instance;
        if (!TestMode.IsOff || room == null || !room.IsInsideTree())
            return;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            GD.PushWarning("[Manosaba] Refute cut-in scene is missing. Re-export Manosaba.pck.");
            return;
        }

        var effect = scene.Instantiate<CanvasLayer>();
        effect.Set("audio_volume", playAudio ? ManosabaConfig.GetManosabaSfxVolume() : 0f);
        effect.Set("voice_path", voicePath ?? string.Empty);
        effect.Set("character", (int)variant / 3);
        effect.Set("expression", (int)variant % 3);
        // Owned by the room: leaving combat also removes the overlay and its sound players.
        room.AddChild(effect);
        ulong started = Time.GetTicksMsec();
        try
        {
            while (GodotObject.IsInstanceValid(effect) && effect.IsInsideTree()
                && GodotObject.IsInstanceValid(room) && room.IsInsideTree()
                && Time.GetTicksMsec() - started < 6000)
                await Cmd.Wait(0.05f);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(effect) && !effect.IsQueuedForDeletion())
                effect.QueueFree();
        }
    }
}
