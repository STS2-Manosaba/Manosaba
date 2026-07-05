using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public sealed class InfiniteMirror : PathCustomCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override bool IsPlayable => base.IsPlayable && SawatariCocoHelper.IsInLiveStreamMode(Owner.Creature);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<LiveStreamModePower>()];

    public InfiniteMirror() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (Owner.Creature is not { } creature)
        {
            return;
        }

        decimal currentAmount = creature.GetPowerAmount<LiveStreamModePower>();
        if (currentAmount <= 0m)
        {
            return;
        }

        await CommonActions.Apply<LiveStreamModePower>(choiceContext, creature, this, currentAmount);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
