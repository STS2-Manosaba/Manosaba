using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma.Powers;
using manosaba.Characters.SakurabaEma.Visuals;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class WitchIslandTreasureMap : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BadEndSkillPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BadEndSkillPower>()];

    public WitchIslandTreasureMap() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable && Owner.Character.Id.Entry.EndsWith(SakurabaEma.CharacterId, StringComparison.OrdinalIgnoreCase);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        BadEndSkillPower? existing = Owner.Creature.GetPower<BadEndSkillPower>();
        if (existing != null)
        {
            existing.OnDuplicateCardPlayed();
        }
        else
        {
            await PowerCmd.Apply<BadEndSkillPower>(choiceContext, Owner.Creature, DynamicVars["BadEndSkillPower"].BaseValue, Owner.Creature, this);
        }

        BadEndSkillButtonUi.EnsureShown(Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
