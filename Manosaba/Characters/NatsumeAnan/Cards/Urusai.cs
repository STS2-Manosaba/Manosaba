using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.NatsumeAnan.Cards;

[Pool(typeof(NatsumeAnanCardPool))]
public sealed class Urusai : NatsumeKotodamaCardModel
{
    private int _resolvedKotodamaX;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
    ];

    public Urusai() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy, true)
    {
    }

    public int PrepareKotodamaXCostForPlay()
    {
        int kotodamaSpent = Math.Max(0, KotodamaEnergy.Get(Owner));
        _resolvedKotodamaX = ResolveKotodamaXValue(kotodamaSpent);
        return kotodamaSpent;
    }

    public void OverrideResolvedKotodamaXCostForPlay(int value)
    {
        _resolvedKotodamaX = Math.Max(0, value);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hits = _resolvedKotodamaX;
        if (hits <= 0 && cardPlay.IsAutoPlay)
        {
            // Auto-play paths (for example Instigate -> AutoPlayFromDrawPile) bypass SpendResources.
            // Match vanilla X-cost behavior: resolve X from current resource but do not spend it.
            hits = ResolveKotodamaXValue(KotodamaEnergy.Get(Owner));
            _resolvedKotodamaX = hits;
        }

        try
        {
            if (hits <= 0)
            {
                return;
            }

            MegaCrit.Sts2.Core.Combat.ICombatState? combatState = CombatState;
            if (combatState == null)
            {
                return;
            }

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(hits)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(combatState)
                .Execute(choiceContext);
        }
        finally
        {
            if (cardPlay.IsLastInSeries)
            {
                _resolvedKotodamaX = 0;
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
