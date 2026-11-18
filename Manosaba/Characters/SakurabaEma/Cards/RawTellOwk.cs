using BaseLib.Utils;
using Manosaba.Extensions;
using Manosaba.Combat;
using manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class RawTellOwk : PathCustomCardModel
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<InterrogationStartPower>(), HoverTipFactory.FromPower<InterrogationProgressPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Progress", 10m)];

    public RawTellOwk() : base(0, CardType.Power, CardRarity.Token, TargetType.Self, false)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        var field = Owner.Creature.CombatState
            ?? throw new InvalidOperationException("Interrogation requires an active combat.");
        if (FieldPowerState.Has<InterrogationStartPower>(field))
            FieldPowerState.Apply<InterrogationProgressPower>(field, DynamicVars["Progress"].IntValue);
        else
            await InterrogationStartPower.ApplyToField(choiceContext, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
