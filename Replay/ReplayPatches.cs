using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using GkwvxYtMods.Replay;

namespace SeuModBepInEx
{
    [HarmonyPatch]
    public static class ReplayPatches
    {
        private static GravadorReplaySessao? _gravador;

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
        [HarmonyPostfix]
        public static void AoIniciarPartida(ShipStatus __instance)
        {
            _gravador = new GravadorReplaySessao(mapa: __instance.name);
            _gravador.Iniciar();
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void AoAtualizarFrame()
        {
            if (_gravador == null || !_gravador.Gravando || PlayerControl.AllPlayerControls == null)
                return;

            var lista = new List<PosicaoJogador>();
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null) continue;
                var pos = p.GetTruePosition();
                lista.Add(new PosicaoJogador
                {
                    JogadorId = p.PlayerId,
                    Nome = p.Data.PlayerName,
                    Cor = p.Data.ColorName,
                    X = pos.x,
                    Y = pos.y,
                    Vivo = !p.Data.IsDead
                });
            }

            _gravador.CapturarTick(lista);
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        [HarmonyPostfix]
        public static void AoEliminarJogador(PlayerControl __instance, PlayerControl target)
        {
            if (_gravador == null || !_gravador.Gravando) return;
            string autor = __instance?.Data?.PlayerName ?? "Desconhecido";
            string alvo = target?.Data?.PlayerName ?? "Desconhecido";
            _gravador.RegistrarEvento("eliminacao", autor: autor, alvo: alvo);
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        [HarmonyPostfix]
        public static void AoEncerrarPartida()
        {
            if (_gravador == null || !_gravador.Gravando) return;
            string pasta = Path.Combine(Paths.PluginPath, "GkwvxYtReplays");
            _gravador.FinalizarESalvar(pasta);
            _gravador = null;
        }
    }
}
