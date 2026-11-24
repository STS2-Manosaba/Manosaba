using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Cards;
using manosaba.Characters.SakurabaEma.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Powers;

public class EndlessEvidencePower : PathCustomPowerModel
{
    protected virtual bool GeneratesUpgradedEvidence => false;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [WitchEncyclopediaState.HoverTip];
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        for (int i = 0; i < Amount; i++) await WitchEncyclopediaState.GenerateRandomEvidence(player, GeneratesUpgradedEvidence);
    }
}

public sealed class UpgradedEndlessEvidencePower : EndlessEvidencePower
{
    protected override bool GeneratesUpgradedEvidence => true;
    public override string CustomPackedIconPath => ModelDb.Power<EndlessEvidencePower>().CustomPackedIconPath;
    public override string CustomBigIconPath => CustomPackedIconPath;
    public override string CustomBigBetaIconPath => CustomPackedIconPath;
}

public sealed class AfterDinnerStatementPower : PathCustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Statement>()];
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || Owner.CombatState is not { } combat) return;
        int count = Amount;
        await PowerCmd.Remove(this);
        for (int i = 0; i < count; i++)
            CardCmd.PreviewCardPileAdd(await EmaCardGeneration.Add(combat.CreateCard<Statement>(player), PileType.Hand, player));
    }
}
