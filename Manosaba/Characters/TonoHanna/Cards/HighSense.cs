using BaseLib.Utils;
using manosaba.Characters.TonoHanna;
using Manosaba.Characters.TonoHanna.Powers;
using Manosaba.Characters.TonoHanna.Visuals;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Manosaba.Characters.TonoHanna.Cards;

[Pool(typeof(TonoHannaCardPool))]
public sealed class HighSense : PathCustomCardModel
{
    protected override bool IsPlayable =>
        base.IsPlayable && Owner.Character is global::manosaba.Characters.TonoHanna.TonoHanna;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HighSensePower>()];

    public HighSense() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        if (Owner?.Creature is not { } ownerCreature)
        {
            return;
        }

        HighSensePower? existingPower = ownerCreature.GetPower<HighSensePower>();
        if (existingPower != null)
        {
            existingPower.OnDuplicateCardPlayed();
        }
        else
        {
            await PowerCmd.Apply<HighSensePower>(choiceContext, ownerCreature, 1m, ownerCreature, this);
        }

        HighSenseSkillButtonUi.EnsureShown(Owner);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
