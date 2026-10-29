using Godot;
using Manosaba.Audio;
using Manosaba.Characters.Common.Powers;
using Manosaba.Config;
using manosaba.Characters.SakurabaEma.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace manosaba.Characters.SakurabaEma.Combat;

public static class MagicToKillWitchPresentation
{
    public const string ScenePath = "res://Manosaba/scenes/vfx/magic_to_kill_witch/magic_to_kill_witch.tscn";
    public const string BgmEvent = "event:/Manosaba/audio/bgm/EmA (-狂-).mp3";
    private static readonly HashSet<ulong> PlayedThisRun = [];

    public static void ResetForNewRun() => PlayedThisRun.Clear();

    public static bool IsEligible(CardModel card) => card is MagicToKillTheWitch
        && card.Owner?.Character is SakurabaEma
        && card.Owner.Creature.GetPowerAmount<MajokaPower>() >= card.DynamicVars["MajokaThreshold"].IntValue;

    public static bool ClaimPlayback(ulong playerId, ManosabaFxPlayMode mode)
    {
        if (mode == ManosabaFxPlayMode.Never)
            return false;
        bool first = PlayedThisRun.Add(playerId);
        return mode != ManosabaFxPlayMode.OncePerRun || first;
    }

    public static async Task Play(CardModel card)
    {
        if (!TestMode.IsOff || !IsEligible(card)
            || NCombatRoom.Instance is not { } room || !room.IsInsideTree())
            return;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            GD.PushWarning("[Manosaba] Magic to Kill the Witch scene is missing. Re-export Manosaba.pck.");
            return;
        }
        if (!ClaimPlayback(card.Owner.NetId, ManosabaConfig.MagicToKillWitchEffectFrequency))
            return;

        var effect = scene.Instantiate<CanvasLayer>();
        // Both start in the same action before any awaited delay. The router owns
        // music until combat ends; the room owns and cleans up the visual overlay.
        GodotSfxRouter.TryPlay(BgmEvent);
        room.AddChild(effect);
        ulong started = Time.GetTicksMsec();
        try
        {
            while (GodotObject.IsInstanceValid(effect) && effect.IsInsideTree()
                && GodotObject.IsInstanceValid(room) && room.IsInsideTree()
                && Time.GetTicksMsec() - started < 14000)
                await Cmd.Wait(0.05f);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(effect) && !effect.IsQueuedForDeletion())
                effect.QueueFree();
        }
    }
}
