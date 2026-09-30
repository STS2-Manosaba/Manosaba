using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using NikaidoHiroCharacter = manosaba.Characters.NikaidoHiro.NikaidoHiro;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class NikaidoHiroTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => NikaidoHiroCharacter.CharacterId;
}
