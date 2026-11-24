using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Cards;

internal static class TestimonyCardHelper
{
    private static readonly Type[] Types =
    [
        typeof(SakurabaEmaTestimony),
        typeof(NikaidoHiroTestimony),
        typeof(NatsumeAnanTestimony),
        typeof(JogasakiNoahTestimony),
        typeof(HasumiLeiaTestimony),
        typeof(SaekiMiriaTestimony),
        typeof(HoshoMagoTestimony),
        typeof(KurobeNanokaTestimony),
        typeof(ShitoAlisaTestimony),
        typeof(TachibanaSherryTestimony),
        typeof(TonoHannaTestimony),
        typeof(SawatariCocoTestimony),
        typeof(HikamiMeruruTestimony),
        typeof(WardenTestimony),
        typeof(GuardTestimony),
        typeof(GrandWitchTestimony),
    ];

    internal static CardModel CreateRandom(ICombatState combat, Player owner)
    {
        Type selected = Types[owner.RunState.Rng.CombatCardSelection.NextInt(Types.Length)];
        return combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(selected)), owner);
    }
}
