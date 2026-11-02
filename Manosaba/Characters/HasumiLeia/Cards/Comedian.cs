using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.HasumiLeia.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class Comedian : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new DynamicVar("BonusBlock", 2m),
    ];

    public Comedian()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int qualifyingCards = PileType.Hand.GetPile(Owner).Cards.Count(IsComedianCard);
        DynamicVars.Block.BaseValue = 8m + qualifyingCards * DynamicVars["BonusBlock"].BaseValue;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["BonusBlock"].UpgradeValueBy(1m);

    private static bool IsComedianCard(CardModel card) =>
        card.Rarity != CardRarity.Basic
        && card.Rarity != CardRarity.Common
        && card.Rarity != CardRarity.Uncommon
        && card.Rarity != CardRarity.Rare
        && card.Rarity != CardRarity.Ancient;
}

