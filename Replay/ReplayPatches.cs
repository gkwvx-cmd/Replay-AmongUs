using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using GkwvxYtMods.Replay;

namespace GkwvxYtMods.Mod
{
    [HarmonyPatch]
    public static class ReplayPatches
    {
        private static GravadorReplaySessao? _gravador;
        private static ManualLogSource? _log;

        public static void Inicializar(ManualLogSource log)
        {
            _log = log;
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
        [HarmonyPostfix]
        public static void AoIniciarPartida(ShipStatus __instance)
        {
            try
            {
                string nomeMapa = __instance != null ? __instance.name : "The Skeld";
                _gravador = new GravadorReplaySessao(mapa: nomeMapa, intervaloMinimoMs: 100);
                _gravador.Iniciar();
                _log?.LogInfo($"[gkwvxYT Replay] Gravação iniciada no mapa: {nomeMapa}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[gkwvxYT Replay] Erro ao iniciar gravação: {ex}");
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void AoAtualizarHud()
        {
            if (_gravador == null || !_gravador.Gravando)
                return;

            try
            {
                var todosJogadores = PlayerControl.AllPlayerControls;
                if (todosJogadores == null || todosJogadores.Count == 0)
                    return;

                var lista = new List<PosicaoJogador>(todosJogadores.Count);
                for (int i = 0; i < todosJogadores.Count; i++)
                {
                    var p = todosJogadores[i];
                    if (p == null || p.Data == null)
                        continue;

                    var pos = p.GetTruePosition();
                    lista.Add(new PosicaoJogador
                    {
                        JogadorId = p.PlayerId,
                        Nome = p.Data.PlayerName ?? $"Jogador_{p.PlayerId}",
                        Cor = p.Data.ColorId.ToString(),
                        X = pos.x,
                        Y = pos.y,
                        Vivo = !p.Data.IsDead
                    });
                }

                _gravador.CapturarTick(lista);
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[gkwvxYT Replay] Falha ao capturar tick: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        [HarmonyPostfix]
        public static void AoEliminarJogador(PlayerControl __instance, PlayerControl target)
        {
            if (_gravador == null || !_gravador.Gravando)
                return;

            try
            {
                string autor = __instance?.Data?.PlayerName ?? "Desconhecido";
                string alvo = target?.Data?.PlayerName ?? "Desconhecido";
                _gravador.RegistrarEvento("eliminacao", autor: autor, alvo: alvo);
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[gkwvxYT Replay] Falha ao registrar eliminação: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        [HarmonyPostfix]
        public static void AoIniciarReuniao()
        {
            if (_gravador == null || !_gravador.Gravando)
                return;

            _gravador.RegistrarEvento("reuniao_emergencia", detalhe: "Reunião iniciada");
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        [HarmonyPostfix]
        public static void AoEncerrarPartida()
        {
            SalvarSeEstiverGravando("fim_partida");
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
        [HarmonyPostfix]
        public static void AoSairDaPartida()
        {
            SalvarSeEstiverGravando("saida_partida");
        }

        private static void SalvarSeEstiverGravando(string motivo)
        {
            if (_gravador == null || !_gravador.Gravando)
                return;

            try
            {
                _gravador.RegistrarEvento(motivo);
                string pastaReplays = Path.Combine(Paths.BepInExRootPath, "GkwvxYtReplays");
                var filme = _gravador.FinalizarESalvar(pastaReplays);
                _log?.LogInfo($"[gkwvxYT Replay] Salvo ({filme.Frames.Count} frames) em: {pastaReplays}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[gkwvxYT Replay] Erro ao salvar replay: {ex}");
            }
            finally
            {
                _gravador = null;
            }
        }
    }
}
