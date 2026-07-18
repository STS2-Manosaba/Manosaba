using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public sealed class TreeHollow : PathCustomCardModel
{
    public override bool GainsBlock => true;

    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, ManosabaKeywords.Unique];

    protected override bool IsPlayable => base.IsPlayable && Owner.Creature.GetPowerAmount<MajokaPower>() < 100m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(25, ValueProp.Move),
        new PowerVar<MajokaPower>(100m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];

    public TreeHollow() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        decimal majokaToApply = DynamicVars["MajokaPower"].BaseValue - Owner.Creature.GetPowerAmount<MajokaPower>();
        if (majokaToApply > 0m)
        {
            await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, this, majokaToApply);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
