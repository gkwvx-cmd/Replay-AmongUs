using System;
using System.Linq;

namespace GkwvxYtMods.Replay
{
    public static class VisualizadorReplay
    {
        public static void ExibirResumo(FilmeReplay filme)
        {
            long duracaoMs = filme.Frames.Count > 0 ? filme.Frames[^1].TempoMs : 0;
            int totalEventos = filme.Frames.Sum(f => f.Eventos.Count);

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

            foreach (var frame in filme.Frames)
            {
                Console.WriteLine($"[t = {frame.TempoMs,6} ms]");
                foreach (var j in frame.Jogadores)
                {
                    string estado = j.Vivo ? "VIVO" : "ELIMINADO";
                    Console.WriteLine($"  • {j.Nome} ({j.Cor}) -> X:{j.X:F2}, Y:{j.Y:F2} [{estado}]");
                }

                foreach (var ev in frame.Eventos)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ! EVENTO [{ev.Nome}]: autor={ev.Autor ?? "-"}, alvo={ev.Alvo ?? "-"} ({ev.Detalhe ?? ""})");
                    Console.ResetColor();
                }
            }
        }
    }
}
