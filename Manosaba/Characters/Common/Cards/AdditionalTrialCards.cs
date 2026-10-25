using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Combat;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(CommonCardPool))]
public sealed class Counterargument : EmaTrialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new TrialDamageVar(8m), new DynamicVar("BonusDamage", 5m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
        [HoverTipFactory.FromCard<Statement>(), HoverTipFactory.FromCard<PresentEvidence>(), WitchEncyclopediaState.HoverTip];
    public Counterargument() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target is not { } target || CombatState is not { } combat) return;
        await WitchEncyclopediaState.Present(context, Owner, SelectionScreenPrompt);
        if (!target.IsAlive) return;
        bool ready = ProgressReady(this);
        decimal damage = DynamicVars.Damage.BaseValue + (ready ? DynamicVars["BonusDamage"].BaseValue : 0m);
        await CreatureCmd.Damage(context, target, damage, TrialMoveValueProp, Owner.Creature, this);
        if (ready)
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(combat.CreateCard<Statement>(Owner), PileType.Hand, Owner));
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["BonusDamage"].UpgradeValueBy(2m);
    }
}

[Pool(typeof(CommonCardPool))]
public sealed class WitchLanguageMastery : PathCustomCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WitchLanguageMasteryPower>()];
    public WitchLanguageMastery() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
        => await CommonActions.Apply<WitchLanguageMasteryPower>(context, Owner.Creature, this, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
