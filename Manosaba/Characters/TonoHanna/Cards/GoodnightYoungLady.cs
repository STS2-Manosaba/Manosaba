using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.TonoHanna.Powers;
using Manosaba.Config;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class GoodnightYoungLady : PathCustomCardModel
{
    private static bool _bgmPlayedThisRun;
    private const string BgmEventPath = "event:/Manosaba/audio/bgm/goodnight_young_lady.mp3";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    private sealed class GoodnightHealVar : DynamicVar
    {
        public GoodnightHealVar() : base("Heal", 2m)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            _ = previewMode;
            _ = target;
            _ = runGlobalHooks;
            decimal collectionStacks = card.Owner?.Creature?.GetPower<PuppetCollectionSummaryPower>()?.Amount ?? 0m;
            PreviewValue = BaseValue + collectionStacks;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoodnightHealVar()];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PuppetCollectionSummaryPower>()];

    public GoodnightYoungLady() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }

    public static void ResetBgmForNewRun() => _bgmPlayedThisRun = false;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        decimal collectionStacks = ownerCreature.GetPower<PuppetCollectionSummaryPower>()?.Amount ?? 0m;
        await CreatureCmd.Heal(ownerCreature, DynamicVars["Heal"].BaseValue + collectionStacks);

        ManosabaFxPlayMode bgmFrequency = ManosabaConfig.GoodnightYoungLadyBgmFrequency;
        if (bgmFrequency != ManosabaFxPlayMode.Never &&
            (bgmFrequency != ManosabaFxPlayMode.OncePerRun || !_bgmPlayedThisRun))
        {
            SfxCmd.Play(BgmEventPath, 0.8f);
            if (bgmFrequency == ManosabaFxPlayMode.OncePerRun)
            {
                _bgmPlayedThisRun = true;
            }
        }

        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
