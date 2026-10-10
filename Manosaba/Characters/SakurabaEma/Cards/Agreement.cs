using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Agreement : EmaTrialCard
{
    private sealed class TargetArgumentBlockVar : BlockVar
    {
        public TargetArgumentBlockVar() : base(6m, ValueProp.Move)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            _ = previewMode;

            decimal argument = ProgressReady(card) ? target?.GetPowerAmount<ArgumentPower>() ?? 0m : 0m;
            decimal blockPerArgument = card.DynamicVars["BlockPerArgument"].BaseValue;
            decimal raw = Math.Max(BaseValue + argument * blockPerArgument, 0m);

            if (runGlobalHooks)
            {
                ICombatState? combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
                Creature? blockOwner = target ?? card.Owner?.Creature;
                if (combatState == null || blockOwner == null)
                {
                    PreviewValue = raw;
                }
                else
                {
                    PreviewValue = Hook.ModifyBlock(
                        combatState,
                        blockOwner,
                        raw,
                        GetTrialPreviewProps(card),
                        card,
                        null,
                        out IEnumerable<AbstractModel> _);
                }
            }
            else if (!card.IsEnchantmentPreview)
            {
                PreviewValue = raw;
            }

            PreviewValue = Math.Max(PreviewValue, 0m);
        }
    }

    public override bool GainsBlock => true;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new TargetArgumentBlockVar(),
        new DynamicVar("BlockPerArgument", 3m),
    ];

    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ArgumentPower>(),
        HoverTipFactory.FromCard<Statement>(),
    ];

    public Agreement() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyAlly, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;
        if (cardPlay.Target?.Player is not { } targetPlayer || CombatState == null)
        {
            return;
        }

        CardModel statement = CombatState.CreateCard<Statement>(Owner);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(statement, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(result);

        decimal argument = ProgressReady(this) ? cardPlay.Target.GetPowerAmount<ArgumentPower>() : 0m;
        decimal block = DynamicVars.Block.BaseValue + argument * DynamicVars["BlockPerArgument"].BaseValue;
        await CreatureCmd.GainBlock(cardPlay.Target, new BlockVar(block, TrialMoveValueProp), cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);

    private static ValueProp GetTrialPreviewProps(CardModel card) =>
        card.Owner?.Creature?.GetPowerAmount<LawDevilPower>() > 0m
            ? ValueProp.Move
            : ValueProp.Move | ValueProp.Unpowered;
}
