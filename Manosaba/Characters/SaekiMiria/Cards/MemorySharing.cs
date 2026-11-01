using BaseLib.Utils;
using manosaba.Characters.SaekiMiria;
using Manosaba.Characters.Common.Overrides;
using Manosaba.Characters.Common.Powers;
using Manosaba.Characters.NikaidoHiro.Powers;
using Manosaba.Characters.SaekiMiria.Powers;
using Manosaba.Characters.TachibanaSherry.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Manosaba.Characters.SaekiMiria.Cards
{
    [Pool(typeof(SaekiMiriaCardPool))]
    public class MemorySharing : PathCustomCardModel
    {
        private const int energyCost = 1;
        private const CardType type = CardType.Power;
        private const CardRarity rarity = CardRarity.Ancient;
        private const TargetType targetType = TargetType.AllAllies;
        private const bool shouldShowInCardLibrary = true;
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MemorySharingPower>(), HoverTipFactory.FromPower<MajokaPower>()];
        public override IEnumerable<CardKeyword> CanonicalKeywords => [ManosabaKeywords.Mahou, CardKeyword.Eternal, CardKeyword.Innate];

        
        public MemorySharing() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (base.CombatState is not { } combatState || base.Owner.Creature is not { } ownerCreature)
            {
                return;
            }

            IEnumerable<Player> enumerable = from c in combatState.GetTeammatesOf(ownerCreature)
                                             where c != null && c.IsAlive && c.IsPlayer && c.Player != null
                                             select c.Player;

            foreach (Player item in enumerable)
            {
                if (item.Creature == null)
                {
                    continue;
                }

                Console.WriteLine($"Applying MemorySharingPower to {item.Creature.Name}");
                MemorySharingPower? selfPower = await CommonActions.Apply<MemorySharingPower>(choiceContext, item.Creature, this, 1m);
                selfPower?.SetApplier(ownerCreature);
                selfPower?.SetUpgraded(base.IsUpgraded);
            }
            
        }
    }
}
