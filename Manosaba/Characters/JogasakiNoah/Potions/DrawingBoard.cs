using BaseLib.Utils;
using Godot;
using manosaba.Characters.JogasakiNoah;
using Manosaba.Characters.JogasakiNoah.Powers;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Manosaba.Characters.JogasakiNoah.Potions
{
    [Pool(typeof(JogasakiNoahPotionPool))]
    public class DrawingBoard : PathCustomPotionModel
    {
        /// <summary>
        /// Base-game <c>[SavedProperty]</c> only works on cards/relics/enchantments/modifiers, so on a potion the stored
        /// monster was lost on save/load and reconnect. BaseLib's <see cref="SavedSpireField{TKey,TVal}"/> supports potions
        /// through its custom save path; the id is stored as its <see cref="ModelId.ToString"/> form.
        /// </summary>
        private static readonly SavedSpireField<DrawingBoard, string> StoredMonsterField = CreateStoredMonsterField();

        private static SavedSpireField<DrawingBoard, string> CreateStoredMonsterField()
        {
            var field = new SavedSpireField<DrawingBoard, string>(() => null, "storedMonsterId")
            {
                Serializer = (value, writer) => writer.WriteString(value ?? string.Empty),
                Deserializer = reader => reader.ReadString(),
            };
            field.CopyOnClone();
            return field;
        }

        private ModelId StoredMonsterId
        {
            get
            {
                string? raw = StoredMonsterField.Get(this);
                return string.IsNullOrEmpty(raw) ? ModelId.none : ModelId.Deserialize(raw);
            }
        }

        public bool IsNetworked => true;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override PotionRarity Rarity => PotionRarity.Token;
        public override TargetType TargetType => TargetType.Self;

        public override bool CanBeGeneratedInCombat => true;

        public void SetStoredMonster(ModelId id)
        {
            AssertMutable();
            StoredMonsterField.Set(this, id == ModelId.none ? null : id.ToString());
        }

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
        {
            ModelId storedMonsterId = StoredMonsterId;
            if (storedMonsterId == ModelId.none) return;
            if (Owner?.Creature?.CombatState is not { } combatState)
            {
                return;
            }

            MonsterModel? canonicalMonster = ModelDb.GetById<MonsterModel>(storedMonsterId);
            if (canonicalMonster == null)
            {
                return;
            }

            MonsterModel monster = canonicalMonster.ToMutable();
            Creature pet = combatState.CreateCreature(monster, CombatSide.Player, null);
            await PlayerCmd.AddPet(pet, Owner);
            PetEnemyAiPower.AssignAiSlot(pet);
            NCreature? node = NCombatRoom.Instance?.GetCreatureNode(pet);
            if (node != null)
            {
                Vector2 s = node.Visuals.GetCurrentBody().Scale;
                node.Visuals.GetCurrentBody().Scale = new Vector2(-Mathf.Abs(s.X), s.Y);
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
    }
}
