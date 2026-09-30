using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Extensions;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace manosaba.Characters.SakurabaEma.Cards;

public abstract class TestimonyCardBase : PathCustomCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Testimony, CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new PowerVar<EvidencePower>(2m),
        ..TestimonyVars,
    ];

    protected virtual IEnumerable<DynamicVar> TestimonyVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<EvidencePower>(),
        HoverTipFactory.FromKeyword(ManosabaKeywords.Testimony),
        ..TestimonyExtraHoverTips,
    ];

    protected virtual IEnumerable<IHoverTip> TestimonyExtraHoverTips => [];

    protected TestimonyCardBase() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await CommonActions.Apply<EvidencePower>(choiceContext, Owner.Creature, this, DynamicVars["EvidencePower"].BaseValue);
        await OnAfterTestimony(choiceContext);
    }

    protected virtual Task OnAfterTestimony(PlayerChoiceContext choiceContext)
    {
        _ = choiceContext;
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}

public abstract class CharacterTestimonyCardBase : TestimonyCardBase
{
    protected abstract string CharacterId { get; }

    protected override IEnumerable<IHoverTip> TestimonyExtraHoverTips => [HoverTipFactory.FromCard<Statement>()];

    protected override async Task OnAfterTestimony(PlayerChoiceContext choiceContext)
    {
        _ = choiceContext;
        if (CombatState == null)
        {
            return;
        }

        Player? targetPlayer = CombatState.Players
            .Where(player => player.Creature.IsAlive)
            .Where(IsTargetCharacter)
            .OrderBy(player => player.NetId)
            .FirstOrDefault();

        if (targetPlayer == null)
        {
            return;
        }

        CardModel statement = CombatState.CreateCard<Statement>(targetPlayer);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(statement, PileType.Hand, targetPlayer);
        CardCmd.PreviewCardPileAdd(result);
    }

    private bool IsTargetCharacter(Player player) =>
        player.Character.Id.Entry.EndsWith(CharacterId, StringComparison.OrdinalIgnoreCase);
}
