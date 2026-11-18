using BaseLib.Utils;
using Godot;
using Manosaba.Characters.JogasakiNoah;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.JogasakiNoa.Orbs;

[Pool(typeof(JogasakiNoahOrbPool))]
public sealed class BluePaintOrb : ManosabaOrbModel
{
    public override Color DarkenedColor => new("395A96");

    public override decimal PassiveVal => ModifyPaintOrbValue(3m);

    public override decimal EvokeVal => ModifyPaintOrbValue(5m);

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target != null)
        {
            throw new InvalidOperationException("Blue paint orb cannot target creatures.");
        }

        ActivatePassive();
        PlayPassiveSfx();
        await CreatureCmd.GainBlock(Owner.Creature, PassiveVal, ValueProp.Unpowered, null);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        List<Creature> targets = [Owner.Creature];
        Creature? teammate = SelectRandomOtherAlivePlayerTeammate();
        if (teammate != null)
        {
            targets.Add(teammate);
        }

        foreach (Creature target in targets)
        {
            await CreatureCmd.GainBlock(target, EvokeVal, ValueProp.Unpowered, null);
        }

        return targets;
    }
}
