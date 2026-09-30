using BaseLib.Utils;
using Manosaba.Characters.Common;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.NikaidoHiro.Cards;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NikaidoHiroCharacter = manosaba.Characters.NikaidoHiro.NikaidoHiro;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class Flop : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpLoss", 3m),
        new EnergyVar(2),
        new PowerVar<SusPower>(1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        EnergyHoverTip,
        HoverTipFactory.FromPower<SusPower>(),
        HoverTipFactory.FromCard<EmaNikaidoHiro>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    public Flop() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        Creature ownerCreature = Owner.Creature;
        decimal newHp = Math.Max(1m, ownerCreature.CurrentHp - DynamicVars["HpLoss"].BaseValue);
        if (ownerCreature.CurrentHp != newHp)
        {
            await CreatureCmd.SetCurrentHp(ownerCreature, newHp);
        }

        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CommonActions.Apply<SusPower>(choiceContext, ownerCreature, this, DynamicVars["SusPower"].BaseValue);

        Player? nikaidoHiro = SelectNikaidoHiroPlayer();
        if (nikaidoHiro == null || CombatState == null)
        {
            return;
        }

        CardModel ema = CombatState.CreateCard<EmaNikaidoHiro>(nikaidoHiro);
        CardCmd.ApplyKeyword(ema, CardKeyword.Exhaust);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(ema, PileType.Hand, nikaidoHiro);
        CardCmd.PreviewCardPileAdd(result);
    }

    private Player? SelectNikaidoHiroPlayer()
    {
        if (CombatState == null)
        {
            return null;
        }

        List<Player> candidates = CombatState.GetTeammatesOf(Owner.Creature)
            .Where(creature => creature.IsAlive && creature.Player != null)
            .Select(creature => creature.Player!)
            .Where(IsNikaidoHiro)
            .OrderBy(player => player.NetId)
            .ToList();

        return candidates.Count == 0 ? null : candidates[0];
    }

    private static bool IsNikaidoHiro(Player player) =>
        player.Character.Id.Entry.EndsWith(NikaidoHiroCharacter.CharacterId, StringComparison.OrdinalIgnoreCase);

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
