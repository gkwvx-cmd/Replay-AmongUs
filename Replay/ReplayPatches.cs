using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using GkwvxYtMods.Replay;

namespace GkwvxYtMods.Mod
{
    public static class ReplayPatches
    {
        private static GravadorReplaySessao? _gravador;
        private static ManualLogSource? _log;

        public static void Inicializar(ManualLogSource log)
        {
            _log = log;
        }

        public static void AplicarPatches(Harmony harmony)
        {
            AplicarPatchSeguro(harmony, "ShipStatus", "Start", nameof(AoIniciarPartida));
            AplicarPatchSeguro(harmony, "HudManager", "Update", nameof(AoAtualizarHud));
            AplicarPatchSeguro(harmony, "PlayerControl", "MurderPlayer", nameof(AoEliminarJogador));
            AplicarPatchSeguro(harmony, "MeetingHud", "Start", nameof(AoIniciarReuniao));
            AplicarPatchSeguro(harmony, "AmongUsClient", "OnGameEnd", nameof(AoEncerrarPartida));
            AplicarPatchSeguro(harmony, "AmongUsClient", "ExitGame", nameof(AoSairDaPartida));
        }

        private static void AplicarPatchSeguro(Harmony harmony, string nomeTipo, string nomeMetodo, string nomePostfix)
        {
            try
            {
                Type? tipoAlvo = AccessTools.TypeByName(nomeTipo);
                if (tipoAlvo == null)
                {
                    _log?.LogWarning($"[gkwvxYT Replay] Tipo '{nomeTipo}' não encontrado no contexto atual.");
                    return;
                }

                MethodInfo? metodoAlvo = AccessTools.Method(tipoAlvo, nomeMetodo);
                MethodInfo? postfix = AccessTools.Method(typeof(ReplayPatches), nomePostfix);

                if (metodoAlvo != null && postfix != null)
                {
                    harmony.Patch(metodoAlvo, postfix: new HarmonyMethod(postfix));
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[gkwvxYT Replay] Falha ao aplicar patch em {nomeTipo}.{nomeMetodo}: {ex.Message}");
            }
        }

        public static void AoIniciarPartida(object __instance)
        {
            try
            {
                string nomeMapa = LerPropriedadeOuCampo(__instance, "name") as string ?? "The Skeld";
                _gravador = new GravadorReplaySessao(mapa: nomeMapa, intervaloMinimoMs: 100);
                _gravador.Iniciar();
                _log?.LogInfo($"[gkwvxYT Replay] Gravação iniciada no mapa: {nomeMapa}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[gkwvxYT Replay] Erro ao iniciar gravação: {ex}");
            }
        }

        public static void AoAtualizarHud()
        {
            if (_gravador == null || !_gravador.Gravando)
                return;

            try
            {
                Type? tipoPlayerControl = AccessTools.TypeByName("PlayerControl");
                if (tipoPlayerControl == null)
                    return;

                object? allPlayers = AccessTools.Property(tipoPlayerControl, "AllPlayerControls")?.GetValue(null)
                                  ?? AccessTools.Field(tipoPlayerControl, "AllPlayerControls")?.GetValue(null);

                if (allPlayers is not IEnumerable enumerable)
                    return;

                var lista = new List<PosicaoJogador>();
                foreach (object? player in enumerable)
                {
                    if (player == null)
                        continue;

                    object? data = LerPropriedadeOuCampo(player, "Data");
                    if (data == null)
                        continue;

                    byte id = Convert.ToByte(LerPropriedadeOuCampo(player, "PlayerId") ?? 0);
                    string nome = LerPropriedadeOuCampo(data, "PlayerName") as string ?? $"Jogador_{id}";
                    string cor = (LerPropriedadeOuCampo(data, "ColorId") ?? LerPropriedadeOuCampo(data, "DefaultOutfit") ?? "padrao").ToString() ?? "padrao";
                    bool isDead = Convert.ToBoolean(LerPropriedadeOuCampo(data, "IsDead") ?? false);

                    float x = 0f;
                    float y = 0f;
                    object? pos = InvocarMetodoSemParametros(player, "GetTruePosition");
                    if (pos != null)
                    {
                        x = Convert.ToSingle(LerPropriedadeOuCampo(pos, "x") ?? 0f);
                        y = Convert.ToSingle(LerPropriedadeOuCampo(pos, "y") ?? 0f);
                    }

                    lista.Add(new PosicaoJogador
                    {
                        JogadorId = id,
                        Nome = nome,
                        Cor = cor,
                        X = x,
                        Y = y,
                        Vivo = !isDead
                    });
                }

                if (lista.Count > 0)
                    _gravador.CapturarTick(lista);
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[gkwvxYT Replay] Falha ao capturar tick: {ex.Message}");
            }
        }

        public static void AoEliminarJogador(object __instance, object? target)
        {
            if (_gravador == null || !_gravador.Gravando)
                return;

            try
            {
                object? autorData = LerPropriedadeOuCampo(__instance, "Data");
                object? alvoData = target != null ? LerPropriedadeOuCampo(target, "Data") : null;

                string autor = LerPropriedadeOuCampo(autorData, "PlayerName") as string ?? "Desconhecido";
                string alvo = LerPropriedadeOuCampo(alvoData, "PlayerName") as string ?? "Desconhecido";
                _gravador.RegistrarEvento("eliminacao", autor: autor, alvo: alvo);
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[gkwvxYT Replay] Falha ao registrar eliminação: {ex.Message}");
            }
        }

        public static void AoIniciarReuniao()
        {
            if (_gravador == null || !_gravador.Gravando)
                return;

            _gravador.RegistrarEvento("reuniao_emergencia", detalhe: "Reunião iniciada");
        }

        public static void AoEncerrarPartida()
        {
            SalvarSeEstiverGravando("fim_partida");
        }

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

        private static object? LerPropriedadeOuCampo(object? obj, string nome)
        {
            if (obj == null)
                return null;

            Type t = obj.GetType();
            PropertyInfo? prop = AccessTools.Property(t, nome);
            if (prop != null)
                return prop.GetValue(obj);

            FieldInfo? field = AccessTools.Field(t, nome);
            if (field != null)
                return field.GetValue(obj);

            return null;
        }

        private static object? InvocarMetodoSemParametros(object? obj, string nomeMetodo)
        {
            if (obj == null)
                return null;

            MethodInfo? metodo = AccessTools.Method(obj.GetType(), nomeMetodo, Type.EmptyTypes);
            return metodo?.Invoke(obj, null);
        }
    }
}
