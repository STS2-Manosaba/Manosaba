using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using SakurabaEmaCharacter = manosaba.Characters.SakurabaEma.SakurabaEma;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class SakurabaEmaTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => SakurabaEmaCharacter.CharacterId;
}
