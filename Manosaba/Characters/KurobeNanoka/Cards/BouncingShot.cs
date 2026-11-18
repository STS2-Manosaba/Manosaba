using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using manosaba.Extensions;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.KurobeNanoka.Helpers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class BouncingShot : GunBase
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const decimal BounceMultiplier = 0.8m;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.GunShot];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar("BulletCost", 1m),
    ];

    public BouncingShot() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!TrySpendBulletsOnPlay())
        {
            return;
        }

        Creature? initialTarget = cardPlay.Target;
        if (initialTarget == null)
        {
            return;
        }

        if (CombatState == null)
        {
            return;
        }

        List<Creature> orderedTargets = CombatState.HittableEnemies.ToList();
        if (orderedTargets.Count == 0)
        {
            return;
        }

        int startIndex = orderedTargets.IndexOf(initialTarget);
        if (startIndex >= 0)
        {
            orderedTargets = orderedTargets
                .Skip(startIndex)
                .Concat(orderedTargets.Take(startIndex))
                .ToList();
        }
        else
        {
            orderedTargets.Insert(0, initialTarget);
        }

        decimal damage = DynamicVars.Damage.BaseValue;
        foreach (Creature target in orderedTargets)
        {
            if (!target.IsHittable)
            {
                continue;
            }

            await ExecuteGunAttack(choiceContext, target, damage, cardPlay);
            damage *= BounceMultiplier;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
