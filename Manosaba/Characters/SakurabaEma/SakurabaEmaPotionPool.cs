using BaseLib.Abstracts;
using Godot;
using manosaba.Extensions;

namespace manosaba.Characters.SakurabaEma;

public class SakurabaEmaPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => SakurabaEma.Color;

    public override string BigEnergyIconPath => (SakurabaEma.CharacterId + "_energy.png").CharacterImgPath(SakurabaEma.CharacterId);
    public override string TextEnergyIconPath => (SakurabaEma.CharacterId + "_energy_text.png").CharacterImgPath(SakurabaEma.CharacterId);
}
