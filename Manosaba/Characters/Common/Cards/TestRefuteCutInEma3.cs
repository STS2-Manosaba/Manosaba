using BaseLib.Utils;
using Manosaba.Characters.Common.Commands;

namespace Manosaba.Characters.Common.Cards;

[Pool(typeof(MegaCrit.Sts2.Core.Models.CardPools.TokenCardPool))]
public sealed class TestRefuteCutInEma3 : RefuteCutInTestCard
{
    protected override RefuteCutInVariant Variant => RefuteCutInVariant.Ema3;
}
