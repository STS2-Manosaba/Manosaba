using BaseLib.Utils;
using Manosaba.Extensions;
using manosaba.Characters.SakurabaEma;
using manosaba.Characters.SakurabaEma.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HikamiMeruruCharacter = manosaba.Characters.HikamiMeruru.HikamiMeruru;
using TachibanaSherryCharacter = manosaba.Characters.TachibanaSherry.TachibanaSherry;
using TonoHannaCharacter = manosaba.Characters.TonoHanna.TonoHanna;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(SakurabaEmaCardPool))]
public sealed class BestFriendsGroup : PathCustomCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BestFriendsGroupPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BestFriendsGroupPower>()];

    public BestFriendsGroup() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CommonActions.Apply<BestFriendsGroupPower>(choiceContext, Owner.Creature, this, DynamicVars["BestFriendsGroupPower"].BaseValue);

        if (CombatState == null)
            return;

        foreach (Player player in EmaCardTargeting.FindTeammatesByCharacterIds(
                     CombatState,
                     Owner.Creature,
                     TachibanaSherryCharacter.CharacterId,
                     TonoHannaCharacter.CharacterId,
                     HikamiMeruruCharacter.CharacterId))
        {
            await CommonActions.Apply<BestFriendsGroupPower>(choiceContext, player.Creature, this, DynamicVars["BestFriendsGroupPower"].BaseValue);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
