using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.TonoHanna.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class FingerSupportLine : PathCustomCardModel
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner is { PlayerCombatState: { } combatState } owner
        && owner.Gold >= 5 * combatState.Energy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(5),
        new BlockVar(16m, ValueProp.Move),
    ];

    public FingerSupportLine() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0)
            return;

        await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue * x, Owner, GoldLossType.Spent);

        for (int i = 0; i < x; i++)
        {
            int maxBlock = (int)DynamicVars.Block.BaseValue;
            int block = Owner.RunState.Rng.CombatTargets.NextInt(maxBlock + 1);
            if (block > 0)
                await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
        }

        await CommonActions.Apply<FingerSupportLinePower>(choiceContext, Owner.Creature, this, x);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(8m);
    }
}
