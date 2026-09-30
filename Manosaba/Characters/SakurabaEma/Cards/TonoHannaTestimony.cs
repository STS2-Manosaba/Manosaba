using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using TonoHannaCharacter = manosaba.Characters.TonoHanna.TonoHanna;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class TonoHannaTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => TonoHannaCharacter.CharacterId;
}
