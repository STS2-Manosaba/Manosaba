using System.Collections.Generic;
using BaseLib.Utils;
using Manosaba.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Manosaba.Characters.Common.Relics;

/// <summary>
/// Event relic: counts enemy <see cref="Creature"/> deaths (see vanilla <c>GremlinHorn</c> <c>AfterDeath</c>), then replaces with <see cref="RitualSwordBloodied"/>.
/// </summary>
[Pool(typeof(EventRelicPool))]
public sealed class RitualSword : PathCustomRelicModel, Manosaba.Compatibility.IManosabaAfterDeathHandler
{
    private const string EnemiesKey = "Enemies";

    private int _enemiesDefeated;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => EnemiesDefeated;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(EnemiesKey, 12m)];

    [SavedProperty]
    public int EnemiesDefeated
    {
        get => _enemiesDefeated;
        set
        {
            AssertMutable();
            _enemiesDefeated = value;
            InvokeDisplayAmountChanged();
        }
    }

    public async Task HandleAfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
    {
        _ = choiceContext;
        _ = deathAnimLength;
        if (wasRemovalPrevented || Owner?.Creature == null)
        {
            return;
        }

        if (target.Side == Owner.Creature.Side)
        {
            return;
        }

        if (Owner.Creature.CombatState == null || target.CombatState != Owner.Creature.CombatState)
        {
            return;
        }

        EnemiesDefeated++;
        Flash();
        if ((decimal)EnemiesDefeated >= DynamicVars[EnemiesKey].BaseValue)
        {
            await RelicCmd.Replace(this, ModelDb.Relic<RitualSwordBloodied>().ToMutable());
        }
    }
}
