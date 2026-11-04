using System;
using Godot;
using Manosaba.Config;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;

namespace Manosaba.Audio;

public static class GodotSfxRouter
{
    private static readonly string[] AudioExtensions = [".ogg", ".wav", ".mp3"];
    private const string BgmEventPrefix = "event:/Manosaba/audio/bgm/";
    private static readonly HashSet<string> LoopingBgmEvents =
    [
        "event:/Manosaba/audio/bgm/azathoth.ogg",
    ];
    private const string CharacterSelectEventPrefix = "event:/Manosaba/audio/characters/";
    /// <summary>Added on top of the game's requested linear volume (converted to dB). +6 dB ≈ ~2× perceived loudness.</summary>
    private const float CharacterSelectExtraDb = 12f;
    private static AudioStreamPlayer? _customBgmPlayer;
    private static string? _currentBgmEvent;
    private static float _currentBgmBaseVolume = 1f;
    private static float _customBgmVoiceGain = 1f;
    private static bool _vanillaBgmDucked;
    private static AudioStreamPlayer? _characterSelectPlayer;

    public static bool TryPlay(string? eventPath, float volume = 1f)
    {
        if (string.IsNullOrWhiteSpace(eventPath))
        {
            return false;
        }

        if (!TryResolveAudioPath(eventPath, out string? audioPath))
        {
            return false;
        }

        AudioStream? stream = ResourceLoader.Load<AudioStream>(audioPath);
        if (stream == null)
        {
            GD.PushWarning("[Manosaba] Missing Godot audio file for event: " + eventPath + " -> " + audioPath);
            return true;
        }

        NGame? game = NGame.Instance;
        if (game == null)
        {
            return true;
        }

        if (eventPath.StartsWith(BgmEventPrefix))
        {
            PlayCustomBgm(game, eventPath, stream, volume);
            return true;
        }

        if (eventPath.StartsWith(CharacterSelectEventPrefix))
        {
            StopCharacterSelectSfx();
        }

        var player = new AudioStreamPlayer
        {
            Name = "ManosabaSfxPlayer",
            Stream = stream,
            Bus = "SFX"
        };
        ApplyManosabaOneShotVolume(player, eventPath, volume);

        game.AddChild(player);
        if (eventPath.StartsWith(CharacterSelectEventPrefix))
        {
            _characterSelectPlayer = player;
            player.Finished += () =>
            {
                if (_characterSelectPlayer == player)
                {
                    _characterSelectPlayer = null;
                }

                if (GodotObject.IsInstanceValid(player))
                {
                    player.QueueFree();
                }
            };
        }
        else
        {
            player.Finished += () => player.QueueFree();
        }

        player.Play();
        return true;
    }

    /// <summary>Stops the current custom character-select clip so rapid switching does not stack voices.</summary>
    private static void StopCharacterSelectSfx()
    {
        if (_characterSelectPlayer == null || !GodotObject.IsInstanceValid(_characterSelectPlayer))
        {
            _characterSelectPlayer = null;
            return;
        }

        AudioStreamPlayer p = _characterSelectPlayer;
        _characterSelectPlayer = null;
        p.Stop();
        p.QueueFree();
    }

    private static void ApplyManosabaOneShotVolume(AudioStreamPlayer player, string eventPath, float volume)
    {
        float linear = Mathf.Max(volume, 0.0001f);
        if (eventPath.StartsWith(CharacterSelectEventPrefix))
        {
            player.VolumeDb = 20f * MathF.Log10(linear) + CharacterSelectExtraDb;
            return;
        }

        player.VolumeLinear = volume;
    }

    private static bool TryResolveAudioPath(string? eventPath, out string? audioPath)
    {
        const string eventPrefix = "event:/Manosaba";
        if (string.IsNullOrWhiteSpace(eventPath))
        {
            audioPath = null;
            return false;
        }

        if (!eventPath.StartsWith(eventPrefix))
        {
            audioPath = null;
            return false;
        }

        string baseResPath = "res://" + eventPath["event:/".Length..];

        if (ResourceLoader.Exists(baseResPath))
        {
            audioPath = baseResPath;
            return true;
        }

        foreach (string ext in AudioExtensions)
        {
            string candidate = baseResPath + ext;
            if (ResourceLoader.Exists(candidate))
            {
                audioPath = candidate;
                return true;
            }
        }

        // Keep warning readable; loader will fail and report this candidate.
        audioPath = baseResPath + ".ogg";
        return true;
    }

    private static void PlayCustomBgm(NGame game, string eventPath, AudioStream stream, float volume)
    {
        PauseVanillaBgm();
        ConfigureLoopingBgm(eventPath, stream);
        _currentBgmBaseVolume = volume;
        float effectiveVolume = volume * GetConfiguredManosabaSfxVolume() * _customBgmVoiceGain;

        if (_customBgmPlayer != null && GodotObject.IsInstanceValid(_customBgmPlayer))
        {
            if (_currentBgmEvent == eventPath && _customBgmPlayer.IsPlaying())
            {
                _customBgmPlayer.VolumeLinear = effectiveVolume;
                return;
            }

            _customBgmPlayer.Stop();
            _customBgmPlayer.QueueFree();
            _customBgmPlayer = null;
        }


        _customBgmPlayer = new AudioStreamPlayer
        {
            Name = "ManosabaCustomBgmPlayer",
            Stream = stream,
            VolumeLinear = effectiveVolume,
            Bus = "Master"
        };
        _customBgmPlayer.Finished += () =>
        {
            _customBgmPlayer?.QueueFree();
            _customBgmPlayer = null;
            _currentBgmEvent = null;
            _currentBgmBaseVolume = 1f;
            ResumeVanillaBgm();
        };

        game.AddChild(_customBgmPlayer);
        _customBgmPlayer.Play();
        _currentBgmEvent = eventPath;
    }

    private static void ConfigureLoopingBgm(string eventPath, AudioStream stream)
    {
        if (!LoopingBgmEvents.Contains(eventPath))
        {
            return;
        }

        try
        {
            stream.Set("loop", true);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[Manosaba] Failed to enable loop for custom BGM {eventPath}: {ex.Message}");
        }
    }

    private static float GetConfiguredManosabaSfxVolume()
    {
        try
        {
            return ManosabaConfig.GetManosabaSfxVolume();
        }
        catch
        {
            return 1f;
        }
    }

    public static void StopCustomBgmAndResumeVanilla()
    {
        if (_customBgmPlayer != null && GodotObject.IsInstanceValid(_customBgmPlayer))
        {
            _customBgmPlayer.Stop();
            _customBgmPlayer.QueueFree();
            _customBgmPlayer = null;
        }
        _currentBgmEvent = null;
        _currentBgmBaseVolume = 1f;
        ResumeVanillaBgm();
    }

    public static void RefreshCustomBgmVolumeFromSettings()
    {
        if (_customBgmPlayer != null && GodotObject.IsInstanceValid(_customBgmPlayer))
        {
            _customBgmPlayer.VolumeLinear = _currentBgmBaseVolume * GetConfiguredManosabaSfxVolume() * _customBgmVoiceGain;
        }
    }

    internal static void SetCustomBgmVoiceDucking(bool enabled)
    {
        _customBgmVoiceGain = enabled ? 0.2f : 1f;
        RefreshCustomBgmVolumeFromSettings();
    }

    private static void PauseVanillaBgm()
    {
        if (_vanillaBgmDucked)
        {
            return;
        }

        NAudioManager.Instance?.SetBgmVol(0f);
        _vanillaBgmDucked = true;
    }

    private static void ResumeVanillaBgm()
    {
        if (!_vanillaBgmDucked)
        {
            return;
        }

        NAudioManager.Instance?.SetBgmVol(GetConfiguredBgmVolumeRaw());
        _vanillaBgmDucked = false;
    }

    private static float GetConfiguredBgmVolumeRaw()
    {
        try
        {
            return Mathf.Clamp(SaveManager.Instance.SettingsSave.VolumeBgm, 0f, 1f);
        }
        catch
        {
            return 1f;
        }
    }
}
