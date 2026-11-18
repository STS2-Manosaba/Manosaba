using BaseLib.Utils;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

using Manosaba.Characters.Common.Powers;
using Manosaba.Utils;

namespace manosaba.Characters.SawatariCoco.Cards;

public static class FanServiceSkillTokens
{
    public static List<CardModel> CreateOptions(ICombatState combatState, Player player) =>
    [
        combatState.CreateCard<FanServiceAutographToken>(player),
        combatState.CreateCard<FanServiceRoastTimeToken>(player),
    ];

    public static void SyncFanCountVars(CardModel card, Player player)
    {
        int fanCount = SawatariCocoHelper.GetTotalFanCount(player);
        if (card is FanServiceAutographToken)
        {
            card.DynamicVars.Block.BaseValue = fanCount;
        }
        else if (card is FanServiceRoastTimeToken)
        {
            card.DynamicVars.Damage.BaseValue = fanCount;
        }
    }
}

public abstract class FanServiceEventTokenBase : PathCustomCardModel
{
    protected FanServiceEventTokenBase(CardType type, TargetType targetType)
        : base(67, type, CardRarity.Token, targetType, true)
    {
    }

    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
}

[Pool(typeof(TokenCardPool))]
public sealed class FanServiceAutographToken : FanServiceEventTokenBase
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(0, ValueProp.Unpowered)];

    public FanServiceAutographToken() : base(CardType.Skill, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (Owner.Creature is not { } creature)
        {
            return;
        }

        decimal block = SawatariCocoHelper.GetTotalFanCount(Owner);
        if (block > 0m)
        {
            await CreatureCmd.GainBlock(creature, block, ValueProp.Unpowered, null);
        }
    }
}

[Pool(typeof(TokenCardPool))]
public sealed class FanServiceRoastTimeToken : FanServiceEventTokenBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(0, ValueProp.Unpowered)];

    public FanServiceRoastTimeToken() : base(CardType.Attack, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        if (CombatState is not { } combatState || Owner.Creature is not { } dealer)
        {
            return;
        }

        decimal damage = DynamicVars.Damage.BaseValue;
        if (damage <= 0m)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, combatState.HittableEnemies, damage, ValueProp.Unpowered, dealer, this, null);
    }
}
