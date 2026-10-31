using BaseLib.Utils;
using manosaba.Characters.KurobeNanoka;
using Manosaba.Characters.Common.Monsters;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.KurobeNanoka.Cards;

[Pool(typeof(KurobeNanokaCardPool))]
public sealed class MyTurnToProtect : PathCustomCardModel
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];

    public MyTurnToProtect() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        Creature? jailer = Owner.PlayerCombatState?.GetPet<Jailer>()
            ?? Owner.Creature.CombatState?.Allies.FirstOrDefault(c => c.Monster is Jailer && c.PetOwner == Owner);
        if (jailer is { IsAlive: true } && jailer.CurrentHp < jailer.MaxHp * 0.2m)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            return;
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
