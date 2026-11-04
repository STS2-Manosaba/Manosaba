using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Combat;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(CommonCardPool))]
public sealed class Counterargument : EmaTrialCard
{
    protected override bool HasProgressEffect => true;

    private sealed class ArgumentDamageVar() : DamageVar(11m, ValueProp.Move | ValueProp.Unpowered)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            decimal raw = BaseValue + ArgumentBonus(card);
            Props = card.Owner?.Creature.HasPower<LawDevilPower>() == true ? ValueProp.Move : ValueProp.Move | ValueProp.Unpowered;
            PreviewValue = runGlobalHooks && card.Owner != null && (card.CombatState ?? card.Owner.Creature.CombatState) is { } combat
                ? Hook.ModifyDamage(card.Owner.RunState, combat, target, card.Owner.Creature, raw, Props, card,
                    ModifyDamageHookType.All, previewMode, out _) : raw;
        }
    }

    private static decimal ArgumentBonus(CardModel card) =>
        ProgressReady(card) && card.Owner is { } owner
            ? owner.Creature.GetPowerAmount<ArgumentPower>() * card.DynamicVars["BonusDamage"].BaseValue : 0m;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new ArgumentDamageVar(), new DynamicVar("BonusDamage", 2m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
        [HoverTipFactory.FromCard<Statement>(), HoverTipFactory.FromPower<ArgumentPower>(), HoverTipFactory.FromCard<PresentEvidence>(), WitchEncyclopediaState.HoverTip];
    public Counterargument() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target is not { } target || CombatState is not { } combat) return;
        await WitchEncyclopediaState.Present(context, Owner, SelectionScreenPrompt);
        await AttackTrialTarget(context, target, DynamicVars.Damage.BaseValue + ArgumentBonus(this));
        CardCmd.PreviewCardPileAdd(await EmaCardGeneration.Add(combat.CreateCard<Statement>(Owner), PileType.Hand, Owner));
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
        DynamicVars["BonusDamage"].UpgradeValueBy(1m);
    }
}

[Pool(typeof(CommonCardPool))]
public sealed class WitchLanguageMastery : PathCustomCardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WitchLanguageMasteryPower>(1)];
    public WitchLanguageMastery() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
        => await CommonActions.Apply<WitchLanguageMasteryPower>(context, Owner.Creature, this, 1m);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
