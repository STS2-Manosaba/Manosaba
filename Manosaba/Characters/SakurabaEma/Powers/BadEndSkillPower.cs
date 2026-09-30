using Manosaba.Characters.SawatariCoco.Powers;
using Manosaba.Characters.TachibanaSherry.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class BadEndSkillPower : PathCustomPowerModel
{
    public const int DefaultCooldownTurnsAfterUse = 3;
    public const int MinimumCooldownTurnsAfterUse = 1;

    private const string CooldownVarName = "Cooldown";
    private const string CooldownTurnsAfterUseVarName = "CooldownTurnsAfterUse";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CooldownVarName, 0m),
        new DynamicVar(CooldownTurnsAfterUseVarName, DefaultCooldownTurnsAfterUse),
    ];

    public int CooldownRemaining => DynamicVars[CooldownVarName].IntValue;
    public int CooldownTurnsAfterUse => DynamicVars[CooldownTurnsAfterUseVarName].IntValue;
    public bool IsReady => CooldownRemaining <= 0;

    public void MarkUsed()
    {
        DynamicVars[CooldownVarName].BaseValue = CooldownTurnsAfterUse;
    }

    public void OnDuplicateCardPlayed()
    {
        DynamicVar cooldownTurnsAfterUse = DynamicVars[CooldownTurnsAfterUseVarName];
        if (cooldownTurnsAfterUse.IntValue <= MinimumCooldownTurnsAfterUse)
            return;

        cooldownTurnsAfterUse.BaseValue--;
        if (CooldownRemaining > 0)
            DynamicVars[CooldownVarName].BaseValue--;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        if (Owner.Player?.Character.Id.Entry.EndsWith(SakurabaEma.CharacterId, StringComparison.OrdinalIgnoreCase) != true)
        {
            await PowerCmd.Remove(this);
            return;
        }

        await RemoveIncompatible<CouldItBeThatSkillPower>();
        await RemoveIncompatible<FanServiceSkillPower>();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;

        if (player == Owner.Player && CooldownRemaining > 0)
            DynamicVars[CooldownVarName].BaseValue--;

        return Task.CompletedTask;
    }

    private async Task RemoveIncompatible<TPower>()
        where TPower : PowerModel
    {
        TPower? power = Owner.GetPower<TPower>();
        if (power != null)
            await PowerCmd.Remove(power);
    }
}
