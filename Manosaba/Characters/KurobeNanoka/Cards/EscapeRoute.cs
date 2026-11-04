using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class EscapeRoute : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(11m, ValueProp.Move),
    ];

    public EscapeRoute() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Resolve the amount through the normal block pipeline first. This
        // applies Dexterity and respects effects that prevent block gain,
        // matching primitive cards such as Dodge and Roll.
        decimal blockAmount = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (blockAmount <= 0m)
        {
            return;
        }

        // Escape Route grants the resolved amount next turn, not this turn.
        await CreatureCmd.LoseBlock(Owner.Creature, blockAmount);
        await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, Owner.Creature, blockAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
