using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class EveryoneShouldJustDie : PathCustomCardModel
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new BlockVar(5m, ValueProp.Move),
        new PowerVar<EnemyMajokaPower>(10m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<EnemyMajokaPower>(),
    ];

    public EveryoneShouldJustDie() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (CombatState == null || Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await CreatureCmd.GainBlock(ownerCreature, DynamicVars.Block, cardPlay);

        foreach (Creature enemy in CombatState.GetOpponentsOf(ownerCreature).Where(enemy => enemy.IsHittable))
        {
            await CommonActions.Apply<EnemyMajokaPower>(choiceContext, enemy, this, DynamicVars["EnemyMajokaPower"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["EnemyMajokaPower"].UpgradeValueBy(5m);
    }
}
