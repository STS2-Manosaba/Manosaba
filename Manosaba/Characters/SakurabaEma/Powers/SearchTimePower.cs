using manosaba.Characters.SakurabaEma.Cards;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public sealed class SearchTimePower : PathCustomPowerModel
{
    private bool _completed;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override string CustomPackedIconPath => ModelDb.Power<GuiltPower>().CustomPackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<GuiltPower>().CustomBigIconPath;
    public override string CustomBigBetaIconPath => CustomBigIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("RequiredCards", 8m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<RawTellOwk>()];

    internal void StartSearch(int requiredCards)
    {
        AssertMutable();
        _completed = false;
        DynamicVars["RequiredCards"].BaseValue = requiredCards;
        SetAmount(0);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_completed || cardPlay.Card.Owner?.Creature != Owner || !Owner.IsAlive || Owner.Player is not { } player)
            return;

        SetAmount(Amount + 1);
        if (Amount < DynamicVars["RequiredCards"].IntValue)
            return;

        // Finish before generating the reward so repeated/reentrant card callbacks cannot award it twice.
        _completed = true;
        Flash();
        await PowerCmd.Remove(this);
        await GiveRawTellOwk(player);
    }

    internal static async Task GiveRawTellOwk(Player player)
    {
        if (player.Creature.CombatState is not { } combat)
            return;

        CardModel card = combat.CreateCard<RawTellOwk>(player);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        CardCmd.PreviewCardPileAdd(result);
    }
}
