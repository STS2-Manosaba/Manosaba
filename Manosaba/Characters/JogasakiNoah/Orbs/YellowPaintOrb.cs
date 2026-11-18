using BaseLib.Utils;
using Godot;
using Manosaba.Characters.JogasakiNoah;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Manosaba.Characters.JogasakiNoa.Orbs;

[Pool(typeof(JogasakiNoahOrbPool))]
public sealed class YellowPaintOrb : ManosabaOrbModel
{
    public override Color DarkenedColor => new("8F7635");

    public override decimal PassiveVal => ModifyPaintOrbValue(1m);

    public override decimal EvokeVal => ModifyPaintOrbValue(1m);

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target != null)
        {
            throw new InvalidOperationException("Yellow paint orb cannot target creatures.");
        }

        ActivatePassive();
        PlayPassiveSfx();
        await PlayerCmd.GainEnergy(PassiveVal, Owner);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        List<Creature> targets = [Owner.Creature];
        Creature? teammate = SelectRandomOtherAlivePlayerTeammate();
        if (teammate?.Player != null)
        {
            targets.Add(teammate);
        }

        foreach (Creature target in targets)
        {
            if (target.Player != null)
            {
                await PlayerCmd.GainEnergy(EvokeVal, target.Player);
            }
        }

        return targets;
    }
}
