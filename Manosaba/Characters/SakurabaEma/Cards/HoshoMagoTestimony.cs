using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using HoshoMagoCharacter = manosaba.Characters.HoshoMago.HoshoMago;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class HoshoMagoTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => HoshoMagoCharacter.CharacterId;
}
