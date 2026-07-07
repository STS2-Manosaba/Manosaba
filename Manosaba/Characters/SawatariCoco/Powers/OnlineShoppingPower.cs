using BaseLib.Utils;
using manosaba.Characters.SawatariCoco.Helper;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Manosaba.Characters.SawatariCoco.Powers;

/// <summary>網購時間：每回合開始時依層數選擇商品牌加入抽牌堆。</summary>
public sealed class OnlineShoppingPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player != Owner.Player || Owner.CombatState is not { } combatState || Amount <= 0m)
        {
            return;
        }

        for (int i = 0; i < (int)Amount; i++)
        {
            List<CardModel> options = SawatariCocoHelper.ShoppingCardTypes
                .Select(type =>
                {
                    CardModel? canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
                    return canonical != null ? combatState.CreateCard(canonical, player) : null;
                })
                .OfType<CardModel>()
                .ToList();

            if (options.Count == 0)
            {
                return;
            }

            CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, player, canSkip: true);
            if (selected == null)
            {
                return;
            }

            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Draw, player, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}
