using BaseLib.Utils;
using manosaba.Characters.HasumiLeia;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Extensions;

namespace Manosaba.Characters.HasumiLeia.Cards;

[Pool(typeof(HasumiLeiaCardPool))]
public sealed class StardustMirage : PathCustomCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const int CardsToReveal = 5;
    private const int HitCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.SwordTechnique];

    public StardustMirage()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> drawPile = PileType.Draw.GetPile(Owner).Cards.ToList();
        List<CardModel> revealed = drawPile.Take(CardsToReveal).ToList();

        foreach (CardModel card in revealed)
        {
            CardCmd.Preview([card], 1.2f, CardPreviewStyle.HorizontalLayout);
            await Cmd.CustomScaledWait(1.2f, 1.2f);
        }

        if (cardPlay.Target is { } target)
        {
            foreach (CardModel card in revealed)
            {
                if (!card.CanonicalKeywords.Contains(ManosabaKeywords.SwordTechnique))
                {
                    continue;
                }

                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .WithHitCount(HitCount)
                    .FromCard(this)
                    .Targeting(target)
                    .Execute(choiceContext);
            }
        }

        drawPile.StableShuffle(Owner.RunState.Rng.Shuffle);
        foreach (CardModel card in drawPile)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom, this, skipVisuals: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
