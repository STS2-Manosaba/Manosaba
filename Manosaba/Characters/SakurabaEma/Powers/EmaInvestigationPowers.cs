using BaseLib.Utils;
using Manosaba.Characters.Common.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class InformationNetworkPower : PathCustomPowerModel
{
    public const decimal DamagePerStack = 6m;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => ModelDb.Power<GuiltPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("TriggerDamage", DamagePerStack)];
    internal async Task AfterPresentEvidence(PlayerChoiceContext context)
    {
        if (Owner.CombatState is not { } combat) return;
        var enemies = combat.GetOpponentsOf(Owner).Where(c => c.IsHittable).ToList();
        if (enemies.Count == 0) return;
        Creature? target = combat.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target == null) return;
        Flash();
        await CreatureCmd.Damage(context, target, DynamicVars["TriggerDamage"].BaseValue * Amount, ValueProp.Unpowered, Owner, null);
    }
}

public sealed class SakurabaEmeowPower : PathCustomPowerModel
{
    private bool _applyingBleed;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => ModelDb.Power<BleedPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BleedPower>()];
    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (_applyingBleed || dealer != Owner || result.TotalDamage <= 0 || !target.IsAlive) return;
        _applyingBleed = true;
        try
        {
            Flash();
            await CommonActions.Apply<BleedPower>(context, target, cardSource, result.TotalDamage);
        }
        finally { _applyingBleed = false; }
    }
}

public sealed class GluttonyPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => ModelDb.Power<MajokaPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(1m), new PowerVar<DexterityPower>(1m), new PowerVar<MajokaPower>(10m),
            new DynamicVar("Heal", 1m), new PowerVar<GuiltPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<DexterityPower>(),
            HoverTipFactory.FromPower<MajokaPower>(), HoverTipFactory.FromPower<GuiltPower>()];
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || PileType.Hand.GetPile(player).IsEmpty) return;
        var selected = (await CardSelectCmd.FromHand(context, player,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), null, this)).FirstOrDefault();
        if (selected == null) return;
        CardType type = selected.Type;
        await CardCmd.Exhaust(context, selected);
        Flash();
        switch (type)
        {
            case CardType.Attack:
                await CommonActions.Apply<StrengthPower>(context, Owner, null, DynamicVars["StrengthPower"].BaseValue); break;
            case CardType.Skill:
                await CommonActions.Apply<DexterityPower>(context, Owner, null, DynamicVars["DexterityPower"].BaseValue); break;
            case CardType.Power:
                await CommonActions.Apply<MajokaPower>(context, Owner, null, DynamicVars["MajokaPower"].BaseValue); break;
            case CardType.Status:
                await CreatureCmd.Heal(Owner, DynamicVars["Heal"].BaseValue); break;
            case CardType.Curse:
                await CommonActions.Apply<GuiltPower>(context, Owner, null, DynamicVars["GuiltPower"].BaseValue); break;
        }
    }
}

