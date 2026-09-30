using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace manosaba.Characters.SakurabaEma.Cards;

internal static class TestimonyCardHelper
{
    internal static List<CardModel> CreateAll(ICombatState combatState, Player owner) =>
    [
        combatState.CreateCard<SakurabaEmaTestimony>(owner),
        combatState.CreateCard<NikaidoHiroTestimony>(owner),
        combatState.CreateCard<NatsumeAnanTestimony>(owner),
        combatState.CreateCard<JogasakiNoahTestimony>(owner),
        combatState.CreateCard<HasumiLeiaTestimony>(owner),
        combatState.CreateCard<SaekiMiriaTestimony>(owner),
        combatState.CreateCard<HoshoMagoTestimony>(owner),
        combatState.CreateCard<KurobeNanokaTestimony>(owner),
        combatState.CreateCard<ShitoAlisaTestimony>(owner),
        combatState.CreateCard<TachibanaSherryTestimony>(owner),
        combatState.CreateCard<TonoHannaTestimony>(owner),
        combatState.CreateCard<SawatariCocoTestimony>(owner),
        combatState.CreateCard<HikamiMeruruTestimony>(owner),
        combatState.CreateCard<WardenTestimony>(owner),
        combatState.CreateCard<GuardTestimony>(owner),
        combatState.CreateCard<GrandWitchTestimony>(owner),
    ];
}
