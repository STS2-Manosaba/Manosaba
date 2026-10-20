using Godot;
using Manosaba.Audio;
using Manosaba.Characters.Common.Commands;
using Manosaba.Config;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace manosaba.Characters.SakurabaEma.Combat;

/// <summary>Explicitly invoked by the source card, never by the shared evidence mechanic.</summary>
public static class EmaTrialPresentation
{
    public sealed record Cue(string CardName, string VoiceSource, RefuteCutInVariant? CutIn = null,
        int RequiredProgress = 10, string? Bgm = null)
    {
        public string VoicePath => $"res://Manosaba/audio/SFX/ema/{CardName}.ogg";
    }

    private static readonly HashSet<(ulong PlayerId, string Card)> PlayedThisRun = [];

    public static void ResetForNewRun() => PlayedThisRun.Clear();

    public static Cue? GetCue(CardModel card) => card switch
    {
        PresentEvidence => new("present_evidence", "CommonTrial_Ema05119.ogg", RefuteCutInVariant.Ema2),
        Rebuttal => new("rebuttal", "CommonTrial_Ema05117.ogg", RefuteCutInVariant.Ema2),
        Agreement => new("agreement", "CommonTrial_Ema05121.ogg", RefuteCutInVariant.Ema2),
        Sophistry => new("sophistry", "CommonTrial_Ema06197.ogg", RefuteCutInVariant.Ema1),
        Question => new("question", "CommonTrial_Ema05123.ogg"),
        ExhaustiveMethod => new("exhaustive_method", "CommonTrial_Ema06196.ogg", RefuteCutInVariant.Ema2),
        Statement => new("statement", "CommonTrial_Ema05124.ogg"),
        AccUhEartsIndictment => new("acc_uh_earts_indictment", "CommonTrial_Ema06192.ogg", RefuteCutInVariant.Ema3,
            Bgm: "event:/Manosaba/audio/bgm/AccUhEarts (-告-).mp3"),
        AccUhEartsRevelation => new("acc_uh_earts_revelation", "CommonTrial_Ema06195.ogg", RefuteCutInVariant.Ema3,
            Bgm: "event:/Manosaba/audio/bgm/AccUhEarts (-発-).mp3"),
        DArime => new("d_arime", "CommonTrial_Ema06197.ogg", RefuteCutInVariant.Ema3, 20,
            "event:/Manosaba/audio/bgm/DArime.mp3"),
        _ => null
    };

    public static bool IsEligible(CardModel card, Cue cue) =>
        card.Owner?.Character is SakurabaEma
        && (cue.CutIn == null || (EmaTrialCard.Progress(card) >= cue.RequiredProgress
            && !card.Owner.Creature.HasPower<SpecialMagicPower>()));

    // One shared setting, with one allowance per card/player, not one allowance for all ten cards.
    public static bool ClaimAudio(ulong playerId, string cardName, ManosabaFxPlayMode mode)
    {
        if (mode == ManosabaFxPlayMode.Never)
            return false;
        bool first = PlayedThisRun.Add((playerId, cardName));
        return mode != ManosabaFxPlayMode.OncePerRun || first;
    }

    public static async Task Play(CardModel card)
    {
        Cue? cue = GetCue(card);
        if (!TestMode.IsOff || cue == null || !IsEligible(card, cue)
            || NCombatRoom.Instance is not { } room || !room.IsInsideTree())
            return;

        bool audio = ClaimAudio(card.Owner.NetId, cue.CardName, ManosabaConfig.EmaWitchTrialAudioFrequency);
        if (audio && cue.Bgm != null)
            GodotSfxRouter.TryPlay(cue.Bgm);

        if (cue.CutIn is { } variant)
        {
            await RefuteCutInCmd.Play(variant, cue.VoicePath, audio);
        }
        else if (audio)
        {
            var voice = new AudioStreamPlayer
            {
                Stream = ResourceLoader.Load<AudioStream>(cue.VoicePath),
                Bus = "SFX",
                VolumeLinear = ManosabaConfig.GetManosabaSfxVolume()
            };
            room.AddChild(voice);
            voice.Finished += () => voice.QueueFree();
            voice.Play();
        }
    }
}
