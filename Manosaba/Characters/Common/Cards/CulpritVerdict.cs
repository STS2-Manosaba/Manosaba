using BaseLib.Utils;
using manosaba.Characters.Common;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Powers;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(CommonCardPool))]
public sealed class CulpritVerdict : EmaTrialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("RequiredRound", 8m)];
    protected override IEnumerable<IHoverTip> TrialExtraHoverTips =>
        [HoverTipFactory.FromPower<GuiltPower>(), HoverTipFactory.FromPower<TrueCulpritPower>()];
    protected override bool IsPlayable => base.IsPlayable && CombatState is { } combat
        && combat.RoundNumber >= DynamicVars["RequiredRound"].IntValue;
    public override string PortraitPath => ModelDb.Card<RawTellOwk>().PortraitPath;

    public CulpritVerdict() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.None, true) { }

    internal static IReadOnlyList<Creature> FindCulprits(ICombatState combat)
    {
        var creatures = combat.Creatures.Where(creature => creature.IsAlive).ToList();
        if (creatures.Count == 0 || creatures.Any(creature => creature.GetPowerAmount<TrueCulpritPower>() > 0))
            return [];

        int highestGuilt = creatures.Max(creature => creature.GetPowerAmount<GuiltPower>());
        // Zero is also a tied maximum; take a snapshot so application cannot change the winners.
        return creatures.Where(creature => creature.GetPowerAmount<GuiltPower>() == highestGuilt).ToList();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combat || combat.RoundNumber < DynamicVars["RequiredRound"].IntValue)
            return;

        foreach (Creature creature in FindCulprits(combat))
            await CommonActions.Apply<TrueCulpritPower>(choiceContext, creature, this, 1m);
    }

    protected override void OnUpgrade() => DynamicVars["RequiredRound"].UpgradeValueBy(-2m);
}
