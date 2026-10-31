using BaseLib.Utils;
using manosaba.Characters.SaekiMiria;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Manosaba.Characters.SaekiMiria.Cards;

[Pool(typeof(SaekiMiriaCardPool))]
public sealed class Fairness : PathCustomCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyAlly;
    private const bool shouldShowInCardLibrary = true;

    public Fairness()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;

        if (Owner.Creature is not { } ownerCreature || cardPlay.Target is not { } target)
        {
            return;
        }

        decimal totalCurrentHp = ownerCreature.CurrentHp + target.CurrentHp;
        decimal ownerNewHp = totalCurrentHp / 2m;
        decimal targetNewHp = totalCurrentHp - ownerNewHp;

        if (ownerNewHp > ownerCreature.MaxHp)
        {
            decimal overflow = ownerNewHp - ownerCreature.MaxHp;
            ownerNewHp = ownerCreature.MaxHp;
            targetNewHp += overflow;
        }

        if (targetNewHp > target.MaxHp)
        {
            decimal overflow = targetNewHp - target.MaxHp;
            targetNewHp = target.MaxHp;
            ownerNewHp += overflow;
        }

        if (ownerCreature.CurrentHp != ownerNewHp)
        {
            await CreatureCmd.SetCurrentHp(ownerCreature, ownerNewHp);
        }

        if (target.CurrentHp != targetNewHp)
        {
            await CreatureCmd.SetCurrentHp(target, targetNewHp);
        }

        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
