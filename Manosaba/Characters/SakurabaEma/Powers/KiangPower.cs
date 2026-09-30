using Manosaba.Extensions;
using Manosaba.Characters.Common.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class KiangPower : PathCustomPowerModel
{
    private const decimal DamageReduction = 1m;
    private const decimal EndTurnDamage = 5m;
    private const string KiangSfx = "event:/Manosaba/audio/SFX/sakuraba_ema_kiang.ogg";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamageReduction", DamageReduction),
        new DynamicVar("EndTurnDamage", EndTurnDamage),
    ];

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = props;
        _ = dealer;
        _ = cardSource;

        if (target != Owner || amount <= 0m)
            return 0m;

        return -DamageReduction * Amount;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = choiceContext;
        _ = props;
        _ = dealer;
        _ = cardSource;

        if (target == Owner && (result.BlockedDamage > 0m || result.UnblockedDamage > 0m))
        {
            SfxCmd.Play(KiangSfx);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = amount;
        _ = applier;
        _ = cardSource;

        if (power != this || Owner.Player?.Character.Id.Entry.EndsWith(SakurabaEma.CharacterId, StringComparison.OrdinalIgnoreCase) == true)
            return;

        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = creatures;

        if (side != Owner.Side || Owner.CombatState == null || Amount <= 0m || !HasHiroEmaDog())
            return;

        List<Creature> enemies = Owner.CombatState.GetOpponentsOf(Owner)
            .Where(enemy => enemy.IsAlive && enemy.IsHittable)
            .ToList();
        if (enemies.Count == 0)
            return;

        Creature? target = Owner.CombatState.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target == null)
            return;

        await CreatureCmd.Damage(choiceContext, target, EndTurnDamage * Amount, ValueProp.Unpowered, Owner, null);
    }

    private bool HasHiroEmaDog()
    {
        if (Owner.CombatState == null)
            return false;

        return Owner.CombatState.Allies.Any(creature =>
            creature.IsAlive &&
            creature.Monster is SakurabaEmaDog &&
            creature.PetOwner?.Character.Id.Entry.EndsWith(NikaidoHiro.NikaidoHiro.CharacterId, StringComparison.OrdinalIgnoreCase) == true);
    }
}
