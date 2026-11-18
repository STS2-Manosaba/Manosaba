using System.Linq;
using BaseLib.Utils;
using manosaba.Characters.ShitoAlisa;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Utils;

namespace Manosaba.Characters.ShitoAlisa.Cards;

/// <summary>X-cost: deal damage X times to all enemies, then apply X [gold]Burn[/gold] to each.</summary>
[Pool(typeof(ShitoAlisaCardPool))]
public sealed class FireTornado : ShitoAlisaCardModel
{
    protected override bool HasEnergyCostX => true;

    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        WithCombust(0, new DamageVar(3m, ValueProp.Move));

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BurnPower>()];

    public FireTornado()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (CombatState is not { } combatState)
            return;

        int x = ResolveEnergyXValue();
        if (x <= 0)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(x)
            .FromCard(this, cardPlay)
            .TargetingAllOpponentsCompat(combatState)
            .Execute(choiceContext);

        decimal burnPerEnemy = x;
        foreach (Creature enemy in combatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive && e.IsHittable && e.CanReceivePowers).ToList())
            await CommonActions.Apply<BurnPower>(choiceContext, enemy, this, burnPerEnemy);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
