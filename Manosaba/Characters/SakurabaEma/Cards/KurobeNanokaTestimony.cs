using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using KurobeNanokaCharacter = manosaba.Characters.KurobeNanoka.KurobeNanoka;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class KurobeNanokaTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => KurobeNanokaCharacter.CharacterId;
}
