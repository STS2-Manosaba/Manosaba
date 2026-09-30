using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using ShitoAlisaCharacter = manosaba.Characters.ShitoAlisa.ShitoAlisa;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class ShitoAlisaTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => ShitoAlisaCharacter.CharacterId;
}
