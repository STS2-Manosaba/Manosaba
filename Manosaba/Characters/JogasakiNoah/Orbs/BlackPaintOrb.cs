using BaseLib.Utils;
using Godot;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.JogasakiNoah;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Manosaba.Characters.JogasakiNoa.Orbs;

[Pool(typeof(JogasakiNoahOrbPool))]
public sealed class BlackPaintOrb : ManosabaOrbModel
{
    public override Color DarkenedColor => new("2C2C2C");

    public override decimal PassiveVal => ModifyPaintOrbValue(3m);

    public override decimal EvokeVal => ModifyPaintOrbValue(5m);

    public override Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        return Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        ActivatePassive();
        PlayPassiveSfx();
        await CommonActions.Apply<MajokaPower>(choiceContext, Owner.Creature, null, PassiveVal);
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
            await CommonActions.Apply<MajokaPower>(playerChoiceContext, target, null, EvokeVal);
        }
        return targets;
    }
}
