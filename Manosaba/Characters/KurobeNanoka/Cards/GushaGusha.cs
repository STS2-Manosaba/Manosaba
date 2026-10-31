using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using MegaCrit.Sts2.Core.Models.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class GushaGusha1 : PathCustomCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<GushaGusha2>(false)];

    public GushaGusha1() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<StrengthPower>(context, Owner.Creature, this, 1m);
        await CommonActions.Apply<DexterityPower>(context, Owner.Creature, this, 1m);
        await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<GushaGusha2>(Owner), PileType.Discard, Owner);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

[Pool(typeof(TokenCardPool))]
public sealed class GushaGusha2 : PathCustomCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<GushaGusha3>(false)];

    public GushaGusha2() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self, false) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<StrengthPower>(context, Owner.Creature, this, 2m);
        await CommonActions.Apply<DexterityPower>(context, Owner.Creature, this, 2m);
        await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<GushaGusha3>(Owner), PileType.Discard, Owner);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class GushaGusha3 : PathCustomCardModel
{
    public GushaGusha3() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self, false) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<StrengthPower>(context, Owner.Creature, this, 3m);
        await CommonActions.Apply<DexterityPower>(context, Owner.Creature, this, 3m);
    }
}
