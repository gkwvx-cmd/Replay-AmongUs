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
            var harmony = new Harmony("br.gkwvxyt.amongus.replay");
            ReplayPatches.Inicializar(Log);
            ReplayPatches.AplicarPatches(harmony);
        }
    }
}
