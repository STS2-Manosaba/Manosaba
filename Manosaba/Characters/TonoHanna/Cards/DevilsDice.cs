using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.TonoHanna.Helpers;
using Manosaba.Characters.TonoHanna.Vfx;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class DevilsDice : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsUpgraded
        ? [
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
            HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromCard<AngelsDice>(),
        ]
        : [
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
            HoverTipFactory.FromPower<StrengthPower>(),
        ];

    public DevilsDice() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { } target)
        {
            return;
        }

        int face = Owner!.RunState.Rng.CombatTargets.NextInt(6) + 1;
        await HannaDiceRollVfx.PlayDevilsThrowAtCombatCenterAsync(face);
        int modifiedFaceResult = HannaDiceResultHelper.ApplyNumericFaceBonus(Owner!.Creature!, face);
        await CommonActions.Apply<TemporaryStrengthDownPower>(choiceContext, target, this, modifiedFaceResult);

        if (IsUpgraded && CombatState != null && Owner != null)
        {
            CardModel angelsDice = CombatState.CreateCard<AngelsDice>(Owner);
            CardCmd.ApplyKeyword(angelsDice, CardKeyword.Ethereal);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(angelsDice, PileType.Hand, Owner);
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}
