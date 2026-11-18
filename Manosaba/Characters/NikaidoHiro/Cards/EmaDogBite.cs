using BaseLib.Utils;
using manosaba.Characters.NikaidoHiro;
using Manosaba.Characters.Common.Monsters;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.NikaidoHiro.Cards
{
    [Pool(typeof(NikaidoHiroCardPool))]
    public class EmaDogBite : PathCustomCardModel
    {
        private const int energyCost = 2;
        private const CardType type = CardType.Attack;
        private const CardRarity rarity = CardRarity.Uncommon;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;

        protected override bool IsPlayable => Owner?.PlayerCombatState?.GetPet<SakurabaEmaDog>() is { IsAlive: true };

        protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(2)];

        public EmaDogBite() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            Creature? target = cardPlay.Target;
            if (target == null)
            {
                return;
            }

            Creature? ema = Owner?.PlayerCombatState?.GetPet<SakurabaEmaDog>();
            if (ema == null || !ema.IsAlive)
            {
                return;
            }

            decimal damage = ema.CurrentHp;
            for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
            {
                await CreatureCmd.TriggerAnim(ema, "Attack", 0.15f);
                await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move, ema, this, null);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Repeat.UpgradeValueBy(1);
        }
    }
}
