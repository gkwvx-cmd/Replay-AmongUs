using System;
using System.Collections.Generic;
using System.IO;

namespace GkwvxYtMods.Replay
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string? caminhoArg = null;
            string? filtroJogador = null;
            bool apenasEventos = false;
            bool apenasEstatisticas = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--jogador" when i + 1 < args.Length:
                        filtroJogador = args[++i];
                        break;
                    case "--eventos":
                        apenasEventos = true;
                        break;
                    case "--resumo":
                        apenasEstatisticas = true;
                        break;
                    default:
                        if (!args[i].StartsWith("--"))
                            caminhoArg = args[i];
                        break;
                }
            }

            string caminho = !string.IsNullOrWhiteSpace(caminhoArg)
                ? caminhoArg
                : Path.Combine(AppContext.BaseDirectory, "exemplo_gkwvxyt.ndjson");

            if (!File.Exists(caminho))
            {
                var filmeDemo = CriarPartidaSintetica();
                NdjsonReplayStorage.Salvar(caminho, filmeDemo);
                Console.WriteLine($"Replay sintético criado em: {caminho}\n");
            }

            var carregado = NdjsonReplayStorage.Carregar(caminho);
            VisualizadorReplay.ExibirResumo(carregado, filtroJogador, apenasEventos, apenasEstatisticas);
        }

        public static FilmeReplay CriarPartidaSintetica()
        {
            return new FilmeReplay
            {
                Cabecalho = new CabecalhoReplay
                {
                    PartidaId = "gkwvxyt-demo-001",
                    Mapa = "The Skeld",
                    Origem = "gkwvxyt-br",
                    Tema = "gkwvxYT",
                    Versao = 3,
                    InicioUtc = new DateTime(2026, 11, 26, 22, 0, 0, DateTimeKind.Utc)
                },
                Frames = new List<FrameReplay>
                {
                    new()
                    {
                        TempoMs = 0,
                        Jogadores = new List<PosicaoJogador>
                        {
                            new() { JogadorId = 1, Nome = "Faxineiro", Cor = "laranja", X = -0.8f, Y = 1.2f, Vivo = true },
                            new() { JogadorId = 2, Nome = "TripulanteAzul", Cor = "azul", X = 0.5f, Y = 1.0f, Vivo = true }
                        },
                        Eventos = new List<EventoReplay>
                        {
                            new() { Nome = "inicio_partida", Detalhe = "Cafeteria" }
                        }
                    },
                    new()
                    {
                        TempoMs = 2500,
                        Jogadores = new List<PosicaoJogador>
                        {
                            new() { JogadorId = 1, Nome = "Faxineiro", Cor = "laranja", X = 4.2f, Y = -1.5f, Vivo = true },
                            new() { JogadorId = 2, Nome = "TripulanteAzul", Cor = "azul", X = 3.9f, Y = -1.7f, Vivo = true }
                        },
                        Eventos = new List<EventoReplay>
                        {
                            new() { Nome = "tarefa_concluida", Autor = "Faxineiro", Detalhe = "Fios na Elétrica" }
                        }
                    },
                    new()
                    {
                        TempoMs = 5000,
                        Jogadores = new List<PosicaoJogador>
                        {
                            new() { JogadorId = 1, Nome = "Faxineiro", Cor = "laranja", X = 4.5f, Y = -1.8f, Vivo = false },
                            new() { JogadorId = 2, Nome = "TripulanteAzul", Cor = "azul", X = 6.1f, Y = -0.4f, Vivo = true }
                        },
                        Eventos = new List<EventoReplay>
                        {
                            new() { Nome = "eliminacao", Autor = "Desconhecido", Alvo = "Faxineiro", Detalhe = "Elétrica" },
                            new() { Nome = "reuniao_emergencia", Autor = "TripulanteAzul", Detalhe = "Corpo reportado" }
                        }
                    }
                }
            };
        }
    }
}
