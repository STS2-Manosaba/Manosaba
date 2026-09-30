using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace Manosaba.Characters.Common.Encounters;

public sealed class BasementGuardianEventEncounter : EncounterModel, ICustomModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Monsters.BasementGuardian>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<Monsters.BasementGuardian>().ToMutable(), null),
        ];
    }
}
