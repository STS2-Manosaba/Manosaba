using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using JogasakiNoahCharacter = manosaba.Characters.JogasakiNoah.JogasakiNoah;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class JogasakiNoahTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => JogasakiNoahCharacter.CharacterId;
}
