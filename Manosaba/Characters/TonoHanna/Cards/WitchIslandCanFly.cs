using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class WitchIslandCanFly : PathCustomCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new PowerVar<FloatingPower>(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(ManosabaKeywords.Floating)];

    public WitchIslandCanFly() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CreatureCmd.GainBlock(ownerCreature, DynamicVars.Block, cardPlay);
        await CommonActions.Apply<FloatingPower>(choiceContext, ownerCreature, this, DynamicVars["FloatingPower"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
