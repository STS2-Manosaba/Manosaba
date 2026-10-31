using BaseLib.Utils;
using System;
using Manosaba.Characters.Common.Monsters;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.KurobeNanoka.Relics;

[Pool(typeof(KurobeNanokaRelicPool))]
public sealed class Ribbon : LevelingPathCustomRelicModel
{
    private const decimal GuardStartingHp = 10m;
    private const decimal GuardMaxHp = 30m;
    private const decimal GuardSafeTurnMinHealAmount = 5m;
    private const decimal GuardSafeTurnMaxHealAmount = 10m;
    private const decimal SummonMajokaGain = 30m;
    private const decimal InterceptMajokaGain = 10m;

    private bool _hasSummonedThisCombat;
    private bool _redirectedDamageToGuard;
    private bool _isTrackingEnemyTurn;
    private bool _guardTookDamageThisEnemyTurn;
    private Creature? _trackedGuardAtEnemyTurnStart;
    private decimal _trackedGuardHpAtEnemyTurnStart;

    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override int MaxRelicLevel => 5;

    public int CombatStartBulletBonus => RelicLevel >= 4 ? 2 : RelicLevel >= 2 ? 1 : 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new SummonVar(GuardStartingHp),
        new PowerVar<MajokaPower>(SummonMajokaGain),
        new DynamicVar("InterceptMajoka", InterceptMajokaGain),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MajokaPower>()];

    public override Task AfterObtained()
    {
        ApplyRelicLevelEffects();
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        _hasSummonedThisCombat = false;
        _redirectedDamageToGuard = false;
        _isTrackingEnemyTurn = false;
        _guardTookDamageThisEnemyTurn = false;
        _trackedGuardAtEnemyTurnStart = null;
        _trackedGuardHpAtEnemyTurnStart = 0m;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        _ = combatState;

        if (side == CombatSide.Enemy)
        {
            _isTrackingEnemyTurn = true;
            _guardTookDamageThisEnemyTurn = false;
            _trackedGuardAtEnemyTurnStart = GetGuard();
            _trackedGuardHpAtEnemyTurnStart = _trackedGuardAtEnemyTurnStart?.CurrentHp ?? 0m;
            Console.WriteLine(
                $"[Ribbon] Enemy turn start. owner={DescribeOwner(Owner)} hasSummoned={_hasSummonedThisCombat} " +
                $"guard={DescribeGuard(_trackedGuardAtEnemyTurnStart)} trackedHp={_trackedGuardHpAtEnemyTurnStart}");
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;

        if (player != Owner)
        {
            return;
        }

        Console.WriteLine(
            $"[Ribbon] Player turn start. owner={DescribeOwner(Owner)} trackingEnemyTurn={_isTrackingEnemyTurn} " +
            $"guardTookDamage={_guardTookDamageThisEnemyTurn} currentGuard={DescribeGuard(GetGuard())} " +
            $"trackedGuard={DescribeGuard(_trackedGuardAtEnemyTurnStart)} trackedHp={_trackedGuardHpAtEnemyTurnStart}");

        if (!_isTrackingEnemyTurn)
        {
            return;
        }

        if (!_guardTookDamageThisEnemyTurn
            && _trackedGuardAtEnemyTurnStart != null
            && _trackedGuardHpAtEnemyTurnStart > 0m
            && (!_trackedGuardAtEnemyTurnStart.IsAlive
                || _trackedGuardAtEnemyTurnStart.CurrentHp < _trackedGuardHpAtEnemyTurnStart))
        {
            _guardTookDamageThisEnemyTurn = true;
            Console.WriteLine(
                $"[Ribbon] Marking guard damaged from turn-start snapshot. owner={DescribeOwner(Owner)} " +
                $"trackedGuard={DescribeGuard(_trackedGuardAtEnemyTurnStart)} trackedHp={_trackedGuardHpAtEnemyTurnStart}");
        }

        _isTrackingEnemyTurn = false;
        _trackedGuardAtEnemyTurnStart = null;
        _trackedGuardHpAtEnemyTurnStart = 0m;

        if (_hasSummonedThisCombat && !_guardTookDamageThisEnemyTurn)
        {
            Console.WriteLine($"[Ribbon] Safe-turn heal triggered. owner={DescribeOwner(Owner)} heal={GetGuardSafeTurnHealAmount()}");
            Flash();
            await JailerCmd.Heal(
                choiceContext,
                Owner,
                GetGuardSafeTurnHealAmount(),
                DynamicVars.Summon.BaseValue,
                allowOverhealToIncreaseMaxHp: RelicLevel >= MaxRelicLevel);
            return;
        }

        Console.WriteLine(
            $"[Ribbon] Safe-turn heal skipped. owner={DescribeOwner(Owner)} hasSummoned={_hasSummonedThisCombat} " +
            $"guardTookDamage={_guardTookDamageThisEnemyTurn}");
    }

    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
    {
        Console.WriteLine(
            $"[Ribbon] ModifyUnblockedDamageTarget. owner={DescribeOwner(Owner)} target={DescribeGuard(target)} " +
            $"amount={amount} redirectedFlag={_redirectedDamageToGuard} hasSummoned={_hasSummonedThisCombat}");

        if (target != Owner.Creature || amount <= 0m)
        {
            return target;
        }

        Creature? guard = Owner.PlayerCombatState?.GetPet<Jailer>();
        if (guard is { IsAlive: true })
        {
            _redirectedDamageToGuard = true;
            Console.WriteLine($"[Ribbon] Redirecting damage to existing guard. owner={DescribeOwner(Owner)} guard={DescribeGuard(guard)}");
            return guard;
        }

        if (_hasSummonedThisCombat)
        {
            return target;
        }

        guard = JailerCmd.Summon(Owner, DynamicVars.Summon.BaseValue);
        if (guard == null)
        {
            return target;
        }

        _hasSummonedThisCombat = true;
        Console.WriteLine($"[Ribbon] Guard summoned. owner={DescribeOwner(Owner)} guard={DescribeGuard(guard)}");
        Flash();

        _ = PowerCmd.Apply<MajokaPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MajokaPower"].BaseValue,
            Owner.Creature,
            null);

        if (guard.IsAlive)
        {
            _redirectedDamageToGuard = true;
            Console.WriteLine($"[Ribbon] Redirecting damage to newly summoned guard. owner={DescribeOwner(Owner)} guard={DescribeGuard(guard)}");
            return guard;
        }

        return target;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_isTrackingEnemyTurn && target == Owner.Creature && (result.UnblockedDamage > 0m || !target.IsAlive))
        {
            _guardTookDamageThisEnemyTurn = true;
            Console.WriteLine(
                $"[Ribbon] Owner damage recorded as unsafe turn. owner={DescribeOwner(Owner)} " +
                $"target={DescribeGuard(target)} blocked={result.BlockedDamage} unblocked={result.UnblockedDamage} alive={target.IsAlive}");
        }

        if (_isTrackingEnemyTurn && IsOwnersGuard(target) && (result.UnblockedDamage > 0m || !target.IsAlive))
        {
            _guardTookDamageThisEnemyTurn = true;
            Console.WriteLine(
                $"[Ribbon] Guard damage recorded. owner={DescribeOwner(Owner)} target={DescribeGuard(target)} " +
                $"blocked={result.BlockedDamage} unblocked={result.UnblockedDamage} alive={target.IsAlive}");
        }

        if (!_redirectedDamageToGuard)
        {
            Console.WriteLine(
                $"[Ribbon] AfterDamageReceived without redirect flag. owner={DescribeOwner(Owner)} target={DescribeGuard(target)} " +
                $"blocked={result.BlockedDamage} unblocked={result.UnblockedDamage} alive={target.IsAlive}");
            return;
        }

        if (_isTrackingEnemyTurn && result.UnblockedDamage > 0m)
        {
            _guardTookDamageThisEnemyTurn = true;
        }

        if (result.BlockedDamage <= 0m && result.UnblockedDamage <= 0m)
        {
            _redirectedDamageToGuard = false;
            Console.WriteLine(
                $"[Ribbon] Redirected hit dealt no damage. owner={DescribeOwner(Owner)} target={DescribeGuard(target)}");
            return;
        }

        _redirectedDamageToGuard = false;
        Console.WriteLine(
            $"[Ribbon] Redirected hit granted majoka. owner={DescribeOwner(Owner)} target={DescribeGuard(target)} " +
            $"blocked={result.BlockedDamage} unblocked={result.UnblockedDamage}");
        Flash();
        await GainDefenseMajoka();
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = cardSource;

        if (power is not CoveredPower || power.Owner != Owner.Creature || amount <= 0m)
        {
            return;
        }

        if (applier == null || applier == Owner.Creature)
        {
            return;
        }

        decimal currentMajoka = Owner.Creature.GetPowerAmount<MajokaPower>();
        if (currentMajoka >= 100m)
        {
            return;
        }

        Flash();
        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, null, 100m - currentMajoka);
    }

    protected override void OnRelicLevelChanged(int oldLevel, int newLevel)
    {
        ApplyRelicLevelEffects();
    }

    private void ApplyRelicLevelEffects()
    {
        decimal hpPerLevel = (GuardMaxHp - GuardStartingHp) / (MaxRelicLevel - 1);
        DynamicVars.Summon.BaseValue = GuardStartingHp + (RelicLevel - 1) * hpPerLevel;
    }

    private async Task<Creature?> SummonGuardAsync()
    {
        Creature? guard = JailerCmd.Summon(Owner, DynamicVars.Summon.BaseValue);
        if (guard == null)
        {
            return null;
        }

        _hasSummonedThisCombat = true;
        Console.WriteLine($"[Ribbon] Guard summoned. owner={DescribeOwner(Owner)} guard={DescribeGuard(guard)}");
        Flash();

        _ = PowerCmd.Apply<MajokaPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["MajokaPower"].BaseValue,
            Owner.Creature,
            null);

        return guard;
    }

    private Task<MajokaPower?> GainDefenseMajoka()
    {
        return CommonActions.Apply<MajokaPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, null, DynamicVars["InterceptMajoka"].BaseValue);
    }

    private Creature? GetGuard()
    {
        return Owner.PlayerCombatState?.GetPet<Jailer>()
               ?? Owner.Creature.CombatState?.Allies.FirstOrDefault(IsOwnersGuard);
    }

    private bool IsOwnersGuard(Creature creature)
    {
        return creature.Monster is Jailer && creature.PetOwner == Owner;
    }

    private static string DescribeGuard(Creature? creature)
    {
        if (creature == null)
        {
            return "null";
        }

        return $"id={creature.Name} alive={creature.IsAlive} hp={creature.CurrentHp}/{creature.MaxHp} side={creature.Side}";
    }

    private static string DescribeOwner(Player? player)
    {
        if (player == null)
        {
            return "null";
        }

        return $"netId={player.NetId} name={player.Creature.Name}";
    }

    private decimal GetGuardSafeTurnHealAmount()
    {
        decimal relicLevelRatio = MaxRelicLevel <= 1 ? 1m : (RelicLevel - 1m) / (MaxRelicLevel - 1m);
        relicLevelRatio = Math.Clamp(relicLevelRatio, 0m, 1m);
        return GuardSafeTurnMinHealAmount + (GuardSafeTurnMaxHealAmount - GuardSafeTurnMinHealAmount) * relicLevelRatio;
    }
}
