using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using KurobeNanokaCharacter = manosaba.Characters.KurobeNanoka.KurobeNanoka;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class NanokaIsARobot : EmaTrialCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10m, ValueProp.Move),
        new CardsVar(2),
        new PowerVar<SusPower>(1m),
        new PowerVar<ArtifactPower>(1m),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SusPower>(),
        HoverTipFactory.FromPower<ArtifactPower>(),
    ];

    public NanokaIsARobot() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(DynamicVars.Block.BaseValue, TrialMoveValueProp), cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await CommonActions.Apply<SusPower>(choiceContext, Owner.Creature, this, DynamicVars["SusPower"].BaseValue);

        if (CombatState == null)
            return;

        Player? nanoka = EmaCardTargeting
            .FindTeammatesByCharacterIds(CombatState, Owner.Creature, KurobeNanokaCharacter.CharacterId)
            .FirstOrDefault();
        if (nanoka != null)
            await CommonActions.Apply<ArtifactPower>(choiceContext, nanoka.Creature, this, DynamicVars["ArtifactPower"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
