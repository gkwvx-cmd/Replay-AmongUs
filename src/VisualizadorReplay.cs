using System;
using System.Collections.Generic;
using System.Linq;

namespace GkwvxYtMods.Replay
{
    public sealed class EstatisticaJogador
    {
        public string Nome { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
        public float DistanciaPercorrida { get; set; }
        public int TarefasConcluidas { get; set; }
        public int EliminacoesFeitas { get; set; }
        public bool Sobreviveu { get; set; } = true;
    }

    public static class VisualizadorReplay
    {
        public static List<EstatisticaJogador> CalcularEstatisticas(FilmeReplay filme)
        {
            var mapaJogadores = new Dictionary<byte, EstatisticaJogador>();
            var ultimaPosicao = new Dictionary<byte, (float X, float Y)>();

            foreach (var frame in filme.Frames)
            {
                foreach (var j in frame.Jogadores)
                {
                    if (!mapaJogadores.TryGetValue(j.JogadorId, out var est))
                    {
                        est = new EstatisticaJogador
                        {
                            Nome = j.Nome,
                            Cor = j.Cor,
                            Sobreviveu = j.Vivo
                        };
                        mapaJogadores[j.JogadorId] = est;
                    }

                    est.Sobreviveu = j.Vivo;

                    if (ultimaPosicao.TryGetValue(j.JogadorId, out var ant))
                    {
                        float dx = j.X - ant.X;
                        float dy = j.Y - ant.Y;
                        est.DistanciaPercorrida += MathF.Sqrt(dx * dx + dy * dy);
                    }

                    ultimaPosicao[j.JogadorId] = (j.X, j.Y);
                }

                foreach (var ev in frame.Eventos)
                {
                    if (string.IsNullOrWhiteSpace(ev.Autor))
                        continue;

                    var autorEst = mapaJogadores.Values.FirstOrDefault(x =>
                        x.Nome.Equals(ev.Autor, StringComparison.OrdinalIgnoreCase));

                    if (autorEst == null)
                        continue;

                    if (ev.Nome.Equals("tarefa_concluida", StringComparison.OrdinalIgnoreCase))
                        autorEst.TarefasConcluidas++;
                    else if (ev.Nome.Equals("eliminacao", StringComparison.OrdinalIgnoreCase))
                        autorEst.EliminacoesFeitas++;
                }
            }

            return mapaJogadores.Values
                .OrderByDescending(e => e.DistanciaPercorrida)
                .ToList();
        }

        public static void ExibirResumo(
            FilmeReplay filme,
            string? filtroJogador = null,
            bool apenasEventos = false,
            bool apenasEstatisticas = false)
        {
            long duracaoMs = filme.Frames.Count > 0 ? filme.Frames[^1].TempoMs : 0;
            int totalEventos = filme.Frames.Sum(f => f.Eventos.Count);
            var estatisticas = CalcularEstatisticas(filme);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("====================================================");
            Console.WriteLine($"  [{filme.Cabecalho.Tema}] VISUALIZADOR OFFLINE DE REPLAY");
            Console.WriteLine("====================================================");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Partida : {filme.Cabecalho.PartidaId}");
            Console.WriteLine($"Origem  : {filme.Cabecalho.Origem} (v{filme.Cabecalho.Versao})");
            Console.WriteLine($"Mapa    : {filme.Cabecalho.Mapa}");
            Console.WriteLine($"Início  : {filme.Cabecalho.InicioUtc:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Frames  : {filme.Frames.Count} | Eventos: {totalEventos} | Duração: {duracaoMs / 1000.0:F1}s");
            Console.ResetColor();
            Console.WriteLine("----------------------------------------------------");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ESTATÍSTICAS POR JOGADOR:");
            Console.ResetColor();
            foreach (var est in estatisticas)
            {
                if (!string.IsNullOrWhiteSpace(filtroJogador) &&
                    !est.Nome.Equals(filtroJogador, StringComparison.OrdinalIgnoreCase))
                    continue;

                string status = est.Sobreviveu ? "VIVO" : "ELIMINADO";
                Console.WriteLine($"  * {est.Nome} ({est.Cor}) [{status}] -> Distância: {est.DistanciaPercorrida:F2}m | Tarefas: {est.TarefasConcluidas} | Eliminações: {est.EliminacoesFeitas}");
            }

            if (apenasEstatisticas)
                return;

            Console.WriteLine("----------------------------------------------------");
            foreach (var frame in filme.Frames)
            {
                var jogadoresFiltrados = string.IsNullOrWhiteSpace(filtroJogador)
                    ? frame.Jogadores
                    : frame.Jogadores.Where(j => j.Nome.Equals(filtroJogador, StringComparison.OrdinalIgnoreCase)).ToList();

                var eventosFiltrados = string.IsNullOrWhiteSpace(filtroJogador)
                    ? frame.Eventos
                    : frame.Eventos.Where(e =>
                        (e.Autor != null && e.Autor.Equals(filtroJogador, StringComparison.OrdinalIgnoreCase)) ||
                        (e.Alvo != null && e.Alvo.Equals(filtroJogador, StringComparison.OrdinalIgnoreCase))).ToList();

                if (apenasEventos && eventosFiltrados.Count == 0)
                    continue;

                if (!apenasEventos && jogadoresFiltrados.Count == 0 && eventosFiltrados.Count == 0)
                    continue;

                Console.WriteLine($"[t = {frame.TempoMs,6} ms]");

                if (!apenasEventos)
                {
                    foreach (var j in jogadoresFiltrados)
                    {
                        string estado = j.Vivo ? "VIVO" : "ELIMINADO";
                        Console.WriteLine($"  • {j.Nome} ({j.Cor}) -> X:{j.X:F2}, Y:{j.Y:F2} [{estado}]");
                    }
                }

                foreach (var ev in eventosFiltrados)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ! EVENTO [{ev.Nome}]: autor={ev.Autor ?? "-"}, alvo={ev.Alvo ?? "-"} ({ev.Detalhe ?? ""})");
                    Console.ResetColor();
                }
            }
        }
    }
}
