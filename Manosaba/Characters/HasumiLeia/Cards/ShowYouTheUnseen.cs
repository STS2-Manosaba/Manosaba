using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common.Cards;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class ShowYouTheUnseen : PathCustomCardModel
{
    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<ExtremeDash>()];

    public ShowYouTheUnseen()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState)
            return;

        int timesPlayed = Math.Max(1, DynamicVars.Cards.IntValue);
        int energySpent = ResolveEnergyXValue();
        int copies = timesPlayed * energySpent;
        if (copies <= 0)
            return;

        List<CardPileAddResult> results = [];

        for (int i = 0; i < copies; i++)
        {
            CardModel dash = combatState.CreateCard<ExtremeDash>(Owner);
            if (IsUpgraded)
                CardCmd.Upgrade(dash);

            results.Add(await CardPileCmd.AddGeneratedCardToCombat(
                dash,
                PileType.Draw,
                Owner,
                CardPilePosition.Random));
        }

        CardCmd.PreviewCardPileAdd(results);
        DynamicVars.Cards.BaseValue = timesPlayed + 1;
    }
}
