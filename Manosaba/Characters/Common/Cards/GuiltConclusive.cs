using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(CommonCardPool))]
public sealed class GuiltConclusive : EmaTrialCard
{
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
        [HoverTipFactory.FromCard<SuspicionConversion>(), HoverTipFactory.FromCard<GuiltConversion>()];
    public GuiltConclusive() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combat) return;
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext,
            [combat.CreateCard<SuspicionConversion>(Owner), combat.CreateCard<GuiltConversion>(Owner)], Owner);
        if (selected != null) await CardCmd.AutoPlay(choiceContext, selected, Owner.Creature);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public abstract class GuiltConversionCard : PathCustomCardModel
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override int MaxUpgradeLevel => 0;
    public override string PortraitPath => ModelDb.Card<Rebuttal>().PortraitPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SusPower>(), HoverTipFactory.FromPower<GuiltPower>()];
    protected GuiltConversionCard() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, false) { }
    protected override void OnUpgrade() { }
    protected async Task Convert<TFrom, TTo>(PlayerChoiceContext context)
        where TFrom : PowerModel where TTo : PowerModel
    {
        int amount = Owner.Creature.GetPowerAmount<TFrom>();
        if (amount <= 0) return;
        await PowerCmd.Remove(Owner.Creature.GetPower<TFrom>()!);
        await CommonActions.Apply<TTo>(context, Owner.Creature, this, amount);
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class SuspicionConversion : GuiltConversionCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Convert<GuiltPower, SusPower>(choiceContext);
}

[Pool(typeof(TokenCardPool))]
public sealed class GuiltConversion : GuiltConversionCard
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Convert<SusPower, GuiltPower>(choiceContext);
}
