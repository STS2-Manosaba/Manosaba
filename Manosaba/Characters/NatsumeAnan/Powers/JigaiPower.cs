using manosaba.Characters.NatsumeAnan.Cards;
using Manosaba.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.Entities.Creatures;
namespace manosaba.Characters.NatsumeAnan.Powers;

public sealed class JigaiPower : ManosabaTemporaryStrengthPower
{
    public override bool AllowNegative => false;
    public override AbstractModel OriginModel => ModelDb.Card<Jigaishiro>();
    public override LocString Title => new LocString("powers", "MANOSABA-JIGAI_POWER.title");
    public override LocString Description => new LocString("powers", "MANOSABA-JIGAI_POWER.description");
    protected override string SmartDescriptionLocKey => "MANOSABA-JIGAI_POWER.smartDescription";
    protected override bool IsPositive => false;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        _ = combatState;

        if (side != Owner.Side || !Owner.IsAlive || Amount <= 0m)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner,
            Amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null, null);
    }
}
