using BaseLib.Utils;
using Godot;
using manosaba.Characters.JogasakiNoah;
using Manosaba.Characters.JogasakiNoah.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;


namespace Manosaba.Characters.JogasakiNoah.Cards
{
    [Pool(typeof(JogasakiNoahCardPool))]
    public class Still : PathCustomCardModel
    {
        private const int energyCost = 3;
        private const CardType type = CardType.Skill;
        private const CardRarity rarity = CardRarity.Rare;
        private const TargetType targetType = TargetType.AnyEnemy;
        private const bool shouldShowInCardLibrary = true;

        protected override bool IsPlayable => Owner?.PlayerCombatState?.MaxEnergy >= 1;

        public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

        public Still() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target;
            if (target == null)
                return;

            if (Owner?.Creature?.CombatState is not { } combatState)
            {
                return;
            }

            if (combatState.Encounter?.RoomType == RoomType.Monster)
            {
                MonsterModel? canonicalMonster = ModelDb.GetById<MonsterModel>(target.ModelId);
                if (canonicalMonster == null)
                {
                    return;
                }

                MonsterModel monster = canonicalMonster.ToMutable();
                Creature pet = combatState.CreateCreature(monster, CombatSide.Player, null);
                await PlayerCmd.AddPet(pet, Owner);
                PetEnemyAiPower.AssignAiSlot(pet, target);
                NCreature? node = NCombatRoom.Instance?.GetCreatureNode(pet);
                if (node != null)
                {
                    Vector2 s = node.Visuals.GetCurrentBody().Scale;
                    node.Visuals.GetCurrentBody().Scale = new Vector2(-Mathf.Abs(s.X), s.Y);
                    Manosaba.Compatibility.GameApiCompat.SetCreatureInteractable(node, true);
                }

                Creature perspective = pet.PetOwner?.Creature ?? Owner.Creature;
                if (pet.CombatState == null)
                {
                    return;
                }

                List<Creature> petTargets = pet.CombatState.GetOpponentsOf(perspective)
                    .Where(c => c != null && c.IsAlive)
                    .ToList();
                if (PetEnemyAiPower.TryPrepareForNextTurnWithTargets(pet, petTargets, rollNewMove: true))
                {
                    PetEnemyAiPower.TryAdvanceToValidMove(pet, petTargets);
                }
                await CommonActions.Apply<PetEnemyAiPower>(choiceContext, pet, null, 1m);
            }
            else
            {
                await PlayerCmd.GainEnergy(energyCost, Owner);
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
