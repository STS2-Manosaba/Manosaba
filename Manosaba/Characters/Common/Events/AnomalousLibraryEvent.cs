using BaseLib.Abstracts;
using Manosaba.Characters.Common.Encounters;
using Manosaba.Characters.Common.Relics;
using Manosaba.Patches;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Manosaba.Characters.Common.Events;

public sealed class AnomalousLibraryEvent : CustomEventModel
{
    public override bool IsShared => true;
    public override EventLayoutType LayoutType => EventLayoutType.Default;
    public override string CustomInitialPortraitPath => "res://images/events/manosaba-anomalous_library_event.png";
    public override EncounterModel CanonicalEncounter => ModelDb.Encounter<AnomalousLibraryEncounter>();

    public override bool IsAllowed(IRunState runState) => runState.CurrentActIndex == 1
        && runState.Players.Any(p => ManosabaRandomCharacterPool.IsManosabaPlayableCharacter(p.Character));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, Fight, $"{Id.Entry}.pages.INITIAL.options.FIGHT"),
        new(this, Leave, $"{Id.Entry}.pages.INITIAL.options.LEAVE"),
    ];

    private Task Fight()
    {
        EnterCombatWithoutExitingEvent<AnomalousLibraryEncounter>(Array.Empty<Reward>(), shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }

    public override Task Resume(AbstractRoom room)
    {
        if (room is not CombatRoom { Encounter: AnomalousLibraryEncounter } || Owner == null || Owner.Creature.IsDead)
        {
            SetEventFinished(PageDescription("FAIL"));
            return Task.CompletedTask;
        }
        SetEventState(PageDescription("VICTORY"),
        [
            new EventOption(this, TakeMagnifyingGlass, $"{Id.Entry}.pages.VICTORY.options.GLASS",
                HoverTipFactory.FromRelic<LibraryMagnifyingGlass>()),
            new EventOption(this, TakeHannaDoll, $"{Id.Entry}.pages.VICTORY.options.DOLL",
                HoverTipFactory.FromRelic<LibraryHannaDoll>()),
        ]);
        return Task.CompletedTask;
    }

    private Task TakeMagnifyingGlass() => TakeReward<LibraryMagnifyingGlass>();
    private Task TakeHannaDoll() => TakeReward<LibraryHannaDoll>();

    private async Task TakeReward<T>() where T : RelicModel
    {
        // EventSynchronizer executes the voted option once per player's event
        // instance on every peer. Each instance must award only its own Owner.
        await RelicCmd.Obtain(ModelDb.Relic<T>().ToMutable(), Owner!);
        SetEventFinished(PageDescription("REWARD"));
    }
}
