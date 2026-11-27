using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace GkwvxYtMods.Mod
{
    [BepInPlugin("br.gkwvxyt.amongus.replay", "gkwvxYT Replay", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ReplayPatches.Inicializar(Log);
            Harmony.CreateAndPatchAll(typeof(ReplayPatches));
        }
    }
}
