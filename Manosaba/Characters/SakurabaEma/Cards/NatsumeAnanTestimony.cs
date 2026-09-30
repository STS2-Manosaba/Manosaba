using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using NatsumeAnanCharacter = manosaba.Characters.NatsumeAnan.NatsumeAnan;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class NatsumeAnanTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => NatsumeAnanCharacter.CharacterId;
}
