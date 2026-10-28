using System;
using BaseLib.Config;
using Manosaba.Audio;

namespace Manosaba.Config;

public enum ManosabaFxPlayMode
{
    EveryTime,
    OncePerRun,
    Never
}

public sealed class ManosabaConfig : SimpleModConfig
{
    [ConfigHideInUI]
    [ConfigSlider(100, 400, 5, Format = "{0:0}%")]
    public static double LobbyEnemyHpMultiplierPercent { get; set; } = 100d;

    [ConfigHideInUI]
    [ConfigSlider(100, 400, 5, Format = "{0:0}%")]
    public static double LobbyEnemyAttackDamageMultiplierPercent { get; set; } = 100d;

    [ConfigHideInUI]
    [ConfigSlider(0, 100, 5, Format = "{0:0}%")]
    public static double LobbyMurderousImpulseAllyDamageMultiplierPercent { get; set; } = 10d;

    [ConfigHideInUI]
    public static RandomCharacterPoolMode LobbyRandomCharacterPool { get; set; } = RandomCharacterPoolMode.ManosabaCharactersOnly;

    /// <summary>JSON sidecar: maps vanilla run <c>start_time</c> to last saved lobby difficulty snapshot.</summary>
    [ConfigHideInUI]
    public static string PerSaveLobbyDifficultyJson { get; set; } = string.Empty;

    private static double _manosabaSfxVolumePercent = 100d;

    [ConfigSection("SFXSettings")]
    [ConfigSlider(0, 100, 1, Format = "{0:0}%")]
    [ConfigHoverTip]
    public static double ManosabaSfxVolumePercent
    {
        get => _manosabaSfxVolumePercent;
        set
        {
            _manosabaSfxVolumePercent = value;
            GodotSfxRouter.RefreshCustomBgmVolumeFromSettings();
        }
    }

    [ConfigHoverTip]
    public static ManosabaFxPlayMode LaboursOfHiroEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode HikamiMeruruExaidEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode SearchAnalyzeSolveEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode NoahFriendsEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode FireJudgementCourtEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode UnsheatheBiosEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode IAmRebornEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode PrideEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode InmuKingEffectFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    [ConfigHoverTip]
    public static ManosabaFxPlayMode EmaWitchTrialAudioFrequency { get; set; } = ManosabaFxPlayMode.EveryTime;

    public static float GetManosabaSfxVolume()
    {
        return (float)Math.Clamp(ManosabaSfxVolumePercent / 100d, 0d, 1d);
    }
}
