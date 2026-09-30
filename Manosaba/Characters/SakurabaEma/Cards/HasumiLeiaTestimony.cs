using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using HasumiLeiaCharacter = manosaba.Characters.HasumiLeia.HasumiLeia;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class HasumiLeiaTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => HasumiLeiaCharacter.CharacterId;
}
