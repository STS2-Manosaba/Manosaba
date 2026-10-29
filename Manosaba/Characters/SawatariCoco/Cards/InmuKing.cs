using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Config;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public sealed class InmuKing : PathCustomCardModel
{
    private static bool _bgmPlayedThisRun;
    private const string BgmEventPath = "event:/Manosaba/audio/bgm/inmu_king.mp3";

    private const int energyCost = 3;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<HidingPower>(1m),
        new HealVar(14m),
        new PowerVar<LiveStreamModePower>(5m),
        new PowerVar<MajokaPower>(14m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HidingPower>(),
        HoverTipFactory.FromPower<LiveStreamModePower>(),
        HoverTipFactory.FromPower<MajokaPower>(),
    ];

    public InmuKing() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public static void ResetBgmForNewRun()
    {
        _bgmPlayedThisRun = false;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        ManosabaFxPlayMode playMode = ManosabaConfig.InmuKingEffectFrequency;
        if (playMode != ManosabaFxPlayMode.Never &&
            (playMode != ManosabaFxPlayMode.OncePerRun || !_bgmPlayedThisRun))
        {
            SfxCmd.Play(BgmEventPath, 0.8f);
            if (playMode == ManosabaFxPlayMode.OncePerRun)
            {
                _bgmPlayedThisRun = true;
            }
        }

        await CommonActions.Apply<HidingPower>(choiceContext, Owner.Creature, this, DynamicVars["HidingPower"].BaseValue);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        await CommonActions.Apply<LiveStreamModePower>(choiceContext, Owner.Creature, this, DynamicVars["LiveStreamModePower"].BaseValue);
        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, DynamicVars["MajokaPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
