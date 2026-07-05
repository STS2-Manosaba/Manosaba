using BaseLib.Utils;
using manosaba.Characters.SawatariCoco;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(SawatariCocoCardPool))]
public sealed class IWantToPlayAGame : PathCustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    private const int playWhatGameTokens = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Unique];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<IWantToPlayAGamePower>(1m),
        new CardsVar(playWhatGameTokens),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IWantToPlayAGamePower>(),
        HoverTipFactory.FromCard<PlayWhatGame>(),
    ];

    public IWantToPlayAGame() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        await CommonActions.Apply<IWantToPlayAGamePower>(choiceContext, Owner.Creature, this, DynamicVars["IWantToPlayAGamePower"].BaseValue);

        if (CombatState is not { } combatState)
        {
            return;
        }

        for (int i = 0; i < playWhatGameTokens; i++)
        {
            CardModel token = combatState.CreateCard(ModelDb.Card<PlayWhatGame>(), Owner);
            await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
