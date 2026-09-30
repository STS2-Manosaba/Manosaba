using BaseLib.Utils;
using Manosaba.Characters.Common.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class BestFriendsGroupPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<PicnicTime>()];

    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player.Creature != Owner)
            return;

        for (int stack = 0; stack < (int)Amount; stack++)
        {
            if (Owner.CombatState != null && Owner.Player != null)
            {
                CardModel picnic = Owner.CombatState.CreateCard<PicnicTime>(Owner.Player);
                CardCmd.ApplyKeyword(picnic, CardKeyword.Ethereal);
                CardCmd.ApplyKeyword(picnic, CardKeyword.Exhaust);
                await CardPileCmd.AddGeneratedCardToCombat(picnic, PileType.Hand, Owner.Player);
            }
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Amount <= 0m || cardPlay.Card.Type != CardType.Skill || cardPlay.Card.Owner != Owner.Player)
            return;

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
            return;

        foreach (Creature creature in combatState.Players
                     .Select(player => player.Creature)
                     .Where(creature => creature.IsAlive && creature.GetPowerAmount<BestFriendsGroupPower>() > 0m))
        {
            await CreatureCmd.GainBlock(creature, new BlockVar(Amount, ValueProp.Unpowered), cardPlay);
        }
    }
}
