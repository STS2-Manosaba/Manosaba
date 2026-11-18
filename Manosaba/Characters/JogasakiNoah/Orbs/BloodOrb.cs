using BaseLib.Utils;
using Godot;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.JogasakiNoah;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Manosaba.Characters.JogasakiNoa.Orbs;

[Pool(typeof(JogasakiNoahOrbPool))]
public sealed class BloodOrb : ManosabaOrbModel
{
    private decimal _layers = 3m;

    public override Color DarkenedColor => new("8A1F2A");

    public decimal Layers => _layers;

    public override decimal PassiveVal => ModifyBloodOrbValue(_layers);

    public override decimal EvokeVal => ModifyBloodOrbValue(3m);

    public void AddLayers(decimal amount)
    {
        _layers = Math.Max(1m, _layers + amount);
        UpdateVisuals();
    }

    public override Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        return Passive(choiceContext, null);
    }

    public override Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target != null)
        {
            throw new InvalidOperationException("Blood orbs cannot target creatures.");
        }

        ActivatePassive();
        PlayPassiveSfx();
        AddLayers(1m);
        return Task.CompletedTask;
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        ActivateEvoke([Owner.Creature]);
        PlayEvokeSfx();
        await CreatureCmd.Heal(Owner.Creature, EvokeVal);
        return [Owner.Creature];
    }

    private void UpdateVisuals()
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.OrbManager?.UpdateVisuals(OrbEvokeType.None);
    }

    private decimal ModifyBloodOrbValue(decimal baseValue)
    {
        decimal orbModifiedValue = ModifyOrbValue(baseValue);
        decimal majokaAmount = Owner?.Creature?.GetPowerAmount<MajokaPower>() ?? 0m;
        decimal amplified = orbModifiedValue * MajokaPower.GetMajokaDamageMultiplier(majokaAmount);
        return Math.Max(1m, Math.Floor(amplified));
    }
}
