using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Config;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class SteelBallRun : PathCustomCardModel
{
    private static bool _bgmPlayedThisRun;
    private const string BgmEventPath = "event:/Manosaba/audio/bgm/steel_ball_run.mp3";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MajokaPower>(100m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MajokaPower>(),
        HoverTipFactory.FromKeyword(ManosabaKeywords.Unique),
    ];

    public SteelBallRun() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    public static void ResetBgmForNewRun() => _bgmPlayedThisRun = false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        decimal majokaToApply = DynamicVars["MajokaPower"].BaseValue - Owner.Creature.GetPowerAmount<MajokaPower>();
        if (majokaToApply > 0m)
        {
            await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, majokaToApply);
        }

        ManosabaFxPlayMode bgmFrequency = ManosabaConfig.SteelBallRunBgmFrequency;
        if (bgmFrequency == ManosabaFxPlayMode.Never ||
            (bgmFrequency == ManosabaFxPlayMode.OncePerRun && _bgmPlayedThisRun))
        {
            return;
        }

        SfxCmd.Play(BgmEventPath, 0.8f);
        if (bgmFrequency == ManosabaFxPlayMode.OncePerRun)
        {
            _bgmPlayedThisRun = true;
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
