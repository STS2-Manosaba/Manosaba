using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using HikamiMeruruCharacter = manosaba.Characters.HikamiMeruru.HikamiMeruru;

namespace manosaba.Characters.SakurabaEma.Cards;

[Pool(typeof(TokenCardPool))]
public sealed class HikamiMeruruTestimony : CharacterTestimonyCardBase
{
    protected override string CharacterId => HikamiMeruruCharacter.CharacterId;
}
