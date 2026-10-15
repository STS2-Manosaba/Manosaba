using BaseLib.Abstracts;
using Manosaba.Characters.Common.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Manosaba.Characters.Common.Encounters;

public sealed class AnomalousLibraryEncounter : EncounterModel, ICustomModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<LibraryWarden>(), ModelDb.Monster<LibraryGuard>()];
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ModelDb.Monster<LibraryWarden>().ToMutable(), null), (ModelDb.Monster<LibraryGuard>().ToMutable(), null)];
}
