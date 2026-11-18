using Manosaba.Extensions;
using manosaba.Extensions;
using System.Linq;
using Manosaba.Characters.Common.Overrides;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.HasumiLeia.Powers;

public sealed class TreeBranchSecondSwordPower : PathCustomPowerModel
{
    private const decimal ExtraHitScale = 0.3m;
    private const string SharedIconFile = "second_sword_power.png";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override bool ShouldPlayVfx => false;
    public override string CustomPackedIconPath => SharedIconFile.PowerImagePath();
    public override string CustomBigIconPath => SharedIconFile.PowerImagePath();
    public override string CustomBigBetaIconPath => SharedIconFile.PowerImagePath();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;

        if (Owner.GetPower<SecondSwordPower>() is not null)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Owner.CombatState == null)
        {
            return;
        }

        if (command.Attacker != Owner || command.ModelSource is not CardModel card || card.Owner != Owner.Player)
        {
            return;
        }

        if (card.Type != CardType.Attack)
        {
            return;
        }

        if (!card.CanonicalKeywords.Contains(ManosabaKeywords.SwordTechnique))
        {
            return;
        }

        if (card.CanonicalKeywords.Contains(ManosabaKeywords.TwoSwords))
        {
            return;
        }

        foreach (DamageResult result in command.Results.SelectMany(hit => hit))
        {
            if (result.Receiver is not Creature receiver || receiver.IsDead)
            {
                continue;
            }

            if (result.TotalDamage <= 0)
            {
                continue;
            }

            decimal extraDamage = decimal.Ceiling(result.TotalDamage * ExtraHitScale);
            if (extraDamage <= 0m)
            {
                continue;
            }

            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                receiver,
                extraDamage,
                ValueProp.Unpowered,
                Owner,
                card, null);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
    {
        _ = choiceContext;

        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
