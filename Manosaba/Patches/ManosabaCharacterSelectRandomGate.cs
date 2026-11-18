using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace Manosaba.Patches;

/// <summary>
/// Vanilla treats every <see cref="ModelDb.AllCharacters"/> entry as required to unlock random select.
/// </summary>
internal static class ManosabaCharacterSelectRandomGate
{
    internal static bool AreAllPlayableCharactersUnlocked(UnlockState unlockState)
    {
        foreach (CharacterModel c in ModelDb.AllCharacters.Where(
                     static c => c is not CustomCharacterModel { AllowInVanillaRandomCharacterSelect: false }))
        {
            if (!unlockState.Characters.Contains(c))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ShouldShowRandomForLobbyPlayers(IEnumerable<StartRunLobbyPlayer> players)
    {
        foreach (StartRunLobbyPlayer player in players)
        {
            UnlockState unlockState = UnlockState.FromSerializable(player.unlockState);
            if (AreAllPlayableCharactersUnlocked(unlockState))
            {
                return true;
            }
        }

        return false;
    }
}
