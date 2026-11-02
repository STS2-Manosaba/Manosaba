using MegaCrit.Sts2.Core.Entities.Creatures;
using Manosaba.Characters.TonoHanna.Powers;

namespace Manosaba.Characters.TonoHanna.Helpers;

public static class HannaDiceResultHelper
{
    public static int ApplyNumericFaceBonus(Creature owner, int face) =>
        face + (owner.GetPower<HighSensePower>()?.NumericFaceBonus ?? 0);
}
