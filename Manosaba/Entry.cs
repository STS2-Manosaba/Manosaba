using System;
using System.Linq;
using System.Reflection;
using BaseLib.Config;
using Godot.Bridge;
using HarmonyLib;
using manosaba.Characters.HikamiMeruru;
using manosaba.Characters.HikamiMeruru.Helpers;
using manosaba.Characters.HikamiMeruru.Relics;
using manosaba.Characters.HoshoMago;
using manosaba.Characters.HoshoMago.Helpers;
using manosaba.Characters.JogasakiNoah;
using manosaba.Characters.JogasakiNoah.Helpers;
using manosaba.Characters.JogasakiNoah.Relics;
using manosaba.Characters.KurobeNanoka;
using manosaba.Characters.KurobeNanoka.Helpers;
using manosaba.Characters.KurobeNanoka.Relics;
using manosaba.Characters.NatsumeAnan;
using manosaba.Characters.NatsumeAnan.Helpers;
using manosaba.Characters.NatsumeAnan.Relics;
using manosaba.Characters.NikaidoHiro;
using manosaba.Characters.NikaidoHiro.Helpers;
using manosaba.Characters.NikaidoHiro.Relics;
using manosaba.Characters.SaekiMiria;
using manosaba.Characters.SaekiMiria.Helpers;
using manosaba.Characters.SaekiMiria.Relics;
using manosaba.Characters.SakurabaEma.Relics;
using manosaba.Characters.HasumiLeia;
using manosaba.Characters.HasumiLeia.Helpers;
using manosaba.Characters.ShitoAlisa;
using manosaba.Characters.ShitoAlisa.Helpers;
using manosaba.Characters.TachibanaSherry;
using manosaba.Characters.TachibanaSherry.Relics;
using manosaba.Characters.TonoHanna;
using manosaba.Characters.TonoHanna.Helpers;
using manosaba.Characters.TonoHanna.Relics;
using manosaba.Characters.SawatariCoco;
using manosaba.Characters.SawatariCoco.Helpers;
using manosaba.Characters.SawatariCoco.Relics;
using Manosaba.Audio;
using Manosaba.Characters.TachibanaSherry.Helpers;
using Manosaba.Characters.JogasakiNoah.Potions;
using Manosaba.Config;
using Manosaba.Input;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Manosaba;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer("Init")]
public class Entry
{
    public static string ModId = "Manosaba";
    // 初始化函数
    public static void Init()
    {
        ModConfigRegistry.Register(ModId, new ManosabaConfig());

        var harmony = new Harmony(ModId);
        foreach (Type type in typeof(Entry).Assembly.GetTypes())
        {
            if (!type.GetCustomAttributes<HarmonyPatch>().Any())
            {
                continue;
            }

            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                Log.Error($"Harmony failed patching {type.FullName}: {ex.Message}");
            }
        }

        // Must run even when PatchAll fails — Godot scenes reference scripts from this assembly.
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);

        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(DrawingBoard));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(PhotoOfTheGreatWitch));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(PenOfHiro));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(SprayCanOfNoah));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(CabinetKey));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MagnifyingGlass));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(FeatherFan));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(Ribbon));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(LegIrons));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(LegIronsSakurabaEma));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(Clipboard));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(LiveStreamingEquipment));
        PerfectGuardInputTracker.EnsureInstalled();
        CharacterSfxRegistry.Register(TachibanaSherry.CharacterId, SherrySfx.Instance);
        CharacterSfxRegistry.Register(SaekiMiria.CharacterId, SaekiMiriaSfx.Instance);
        CharacterSfxRegistry.Register(HasumiLeia.CharacterId, HasumiLeiaSfx.Instance);
        CharacterSfxRegistry.Register(ShitoAlisa.CharacterId, ShitoAlisaSfx.Instance);
        CharacterSfxRegistry.Register(NatsumeAnan.CharacterId, NatsumeAnanSfx.Instance);
        CharacterSfxRegistry.Register(NikaidoHiro.CharacterId, NikaidoHiroSfx.Instance);
        CharacterSfxRegistry.Register(KurobeNanoka.CharacterId, KurobeNanokaSfx.Instance);
        CharacterSfxRegistry.Register(JogasakiNoah.CharacterId, JogasakiNoahSfx.Instance);
        CharacterSfxRegistry.Register(HoshoMago.CharacterId, HoshoMagoSfx.Instance);
        CharacterSfxRegistry.Register(HikamiMeruru.CharacterId, HikamiMeruruSfx.Instance);
        CharacterSfxRegistry.Register(TonoHanna.CharacterId, TonoHannaSfx.Instance);
        CharacterSfxRegistry.Register(SawatariCoco.CharacterId, SawatariCocoSfx.Instance);
        Log.Debug("Mod initialized!");
    }
}
