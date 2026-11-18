using BaseLib.Utils;
using Manosaba.Characters.KurobeNanoka.Helpers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.KurobeNanoka.Relics;

[Pool(typeof(KurobeNanokaRelicPool))]
public sealed class HandGun : PathCustomRelicModel
{
    private const string DamageThresholdVar = "DamageThreshold";
    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(30m, ValueProp.Move),
        new DynamicVar(DamageThresholdVar, 10m),
    ];

    public override Task BeforeCombatStart()
    {
        _triggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _triggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _ = props;
        _ = cardSource;

        if (_triggeredThisCombat || target != Owner.Creature || dealer == null || !dealer.IsAlive)
        {
            return;
        }

        if (result.UnblockedDamage <= DynamicVars[DamageThresholdVar].BaseValue)
        {
            return;
        }

        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null || !combatState.GetOpponentsOf(Owner.Creature).Contains(dealer))
        {
            return;
        }

        _triggeredThisCombat = true;
        Flash();
        NanokaHelper.PlayGunFireSfx();
        await CreatureCmd.Damage(choiceContext, dealer, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, null, null);
    }
}
