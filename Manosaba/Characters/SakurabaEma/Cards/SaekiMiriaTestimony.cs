using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using SaekiMiriaCharacter = manosaba.Characters.SaekiMiria.SaekiMiria;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class SaekiMiriaTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => SaekiMiriaCharacter.CharacterId;
}
