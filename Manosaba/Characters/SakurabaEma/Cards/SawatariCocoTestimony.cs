using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using SawatariCocoCharacter = manosaba.Characters.SawatariCoco.SawatariCoco;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class SawatariCocoTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => SawatariCocoCharacter.CharacterId;
}
