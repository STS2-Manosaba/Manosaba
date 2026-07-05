using BaseLib.Utils;
using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace manosaba.Characters.SawatariCoco.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class PlayWhatGame : PathCustomCardModel
{
    private const int energyCost = 0;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IWantToPlayAGamePower>()];

    public PlayWhatGame() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        decimal iWantToPlayAGameAmount = Owner.Creature.GetPowerAmount<IWantToPlayAGamePower>();
        if (iWantToPlayAGameAmount > 0m)
        {
            await CommonActions.Apply<IWantToPlayAGamePower>(choiceContext, Owner.Creature, this, -iWantToPlayAGameAmount);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }
}
