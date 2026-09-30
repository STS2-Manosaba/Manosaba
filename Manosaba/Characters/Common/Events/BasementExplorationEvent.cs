using System.Linq;
using BaseLib.Abstracts;
using Manosaba.Characters.Common.Encounters;
using Manosaba.Characters.Common.Relics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Manosaba.Characters.Common.Events;

public sealed class BasementExplorationEvent : CustomEventModel
{
    private const int EscapeHpLoss = 5;

    // Default layout + shouldResumeAfterCombat: true is required so we can return to this event after combat
    // (Combat layout forbids resume; see vanilla EventModel.EnterCombatWithoutExitingEvent).
    // Ritual Sword is granted via combat ExtraRewards like TheLanternKey's SpecialCardReward.
    public override EventLayoutType LayoutType => EventLayoutType.Default;
    public override bool IsShared => true;
    public override EncounterModel CanonicalEncounter => ModelDb.Encounter<BasementGuardianEventEncounter>();

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex < 1
            && runState.Players.All(player => player.Creature.CurrentHp > 5);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, EscapeRoute, $"{Id.Entry}.pages.INITIAL.options.ESCAPE"),
            new EventOption(this, FightRoute, $"{Id.Entry}.pages.INITIAL.options.FIGHT"),
        ];
    }

    private async Task EscapeRoute()
    {
        if (Owner?.Creature != null)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                EscapeHpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);

            List<CardModel> selected = (await CardSelectCmd.FromDeckForRemoval(
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
            await CardPileCmd.RemoveFromDeck(selected);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ESCAPE.description"));
    }

    private Task FightRoute()
    {
        // Shared event owners are local to each client. Build the same reward list
        // for every player so reward indices and choice counters stay synchronized.
        List<Reward> extraRewards = [];
        foreach (var player in Owner!.RunState.Players.OrderBy(player => player.NetId))
        {
            RelicModel ritualSword = ModelDb.Relic<RitualSword>().ToMutable();
            ritualSword.Owner = player;
            extraRewards.Add(new RelicReward(ritualSword, player));
        }
        EnterCombatWithoutExitingEvent<BasementGuardianEventEncounter>(extraRewards, shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    public override async Task Resume(AbstractRoom room)
    {
        if (Owner == null)
        {
            SetEventFinished(L10NLookup($"{Id.Entry}.pages.FAIL.description"));
            return;
        }

        if (room is not CombatRoom { Encounter: BasementGuardianEventEncounter })
        {
            SetEventFinished(L10NLookup($"{Id.Entry}.pages.FAIL.description"));
            return;
        }

        // Ritual Sword is obtained from the combat reward screen (ExtraRewards), not here.
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.VICTORY.description"));
    }
}
