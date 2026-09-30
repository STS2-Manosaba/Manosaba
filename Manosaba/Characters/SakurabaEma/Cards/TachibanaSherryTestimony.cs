using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using TachibanaSherryCharacter = manosaba.Characters.TachibanaSherry.TachibanaSherry;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class TachibanaSherryTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => TachibanaSherryCharacter.CharacterId;
}
