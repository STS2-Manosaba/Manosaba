using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class AccomplicePower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Unpowered), new DamageVar(3m, ValueProp.Unpowered)];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;

        Creature? accomplice = Applier;
        if (accomplice == null || accomplice.IsDead)
            return;

        if (cardPlay.Card.Type == CardType.Attack)
        {
            await CreatureCmd.GainBlock(Owner, DynamicVars.Block, cardPlay);
            await DamageRandomEnemy(choiceContext, accomplice);
        }
        else if (cardPlay.Card.Type == CardType.Skill)
        {
            await DamageRandomEnemy(choiceContext, Owner);
            await CreatureCmd.GainBlock(accomplice, DynamicVars.Block, cardPlay);
        }
    }

    private async Task DamageRandomEnemy(PlayerChoiceContext choiceContext, Creature source)
    {
        Creature? enemy = Owner.CombatState?.HittableEnemies
            .OrderBy(_ => source.Player?.RunState.Rng.CombatTargets.NextInt(1000000) ?? 0)
            .FirstOrDefault();
        if (enemy == null)
            return;

        await CreatureCmd.Damage(choiceContext, enemy, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, source, null);
    }
}
