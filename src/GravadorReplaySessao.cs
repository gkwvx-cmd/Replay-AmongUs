using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GkwvxYtMods.Replay
{
    /// <summary>
    /// Ponte leve para capturar frames e eventos durante uma partida do Among Us
    /// e salvar em NDJSON no tema gkwvxYT.
    /// </summary>
    public sealed class GravadorReplaySessao
    {
        private readonly FilmeReplay _filme;
        private readonly Stopwatch _relogio = new();
        private readonly List<EventoReplay> _eventosPendentes = new();
        private readonly int _intervaloMinimoMs;
        private long _ultimoFrameMs = -1;

        public bool Gravando => _relogio.IsRunning;
        public int TotalFramesGravados => _filme.Frames.Count;

        public GravadorReplaySessao(string mapa, string? partidaId = null, int intervaloMinimoMs = 100)
        {
            _intervaloMinimoMs = Math.Max(10, intervaloMinimoMs);
            _filme = new FilmeReplay
            {
                Cabecalho = new CabecalhoReplay
                {
                    PartidaId = string.IsNullOrWhiteSpace(partidaId)
                        ? $"gkwvxyt-{DateTime.UtcNow:yyyyMMdd-HHmmss}"
                        : partidaId,
                    Mapa = string.IsNullOrWhiteSpace(mapa) ? "The Skeld" : mapa,
                    Origem = "gkwvxyt-br",
                    Tema = "gkwvxYT",
                    Versao = 3,
                    InicioUtc = DateTime.UtcNow
                }
            };
        }

        public void Iniciar()
        {
            if (_relogio.IsRunning)
                return;

            _relogio.Start();
            RegistrarEvento("inicio_partida", detalhe: _filme.Cabecalho.Mapa);
        }

        public void RegistrarEvento(string nome, string? autor = null, string? alvo = null, string? detalhe = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return;

            _eventosPendentes.Add(new EventoReplay
            {
                Nome = nome,
                Autor = autor,
                Alvo = alvo,
                Detalhe = detalhe
            });
        }

        /// <summary>
        /// Chame no Update/FixedUpdate do mod passando a lista atual de jogadores da partida.
        /// </summary>
        public void CapturarTick(IEnumerable<PosicaoJogador> jogadoresAtuais, bool forcar = false)
        {
            if (!_relogio.IsRunning)
                return;

            long agoraMs = _relogio.ElapsedMilliseconds;
            if (!forcar && _ultimoFrameMs >= 0 && (agoraMs - _ultimoFrameMs) < _intervaloMinimoMs && _eventosPendentes.Count == 0)
                return;

            var snapshotJogadores = new List<PosicaoJogador>();
            foreach (var j in jogadoresAtuais)
            {
                snapshotJogadores.Add(new PosicaoJogador
                {
                    JogadorId = j.JogadorId,
                    Nome = j.Nome,
                    Cor = j.Cor,
                    X = j.X,
                    Y = j.Y,
                    Vivo = j.Vivo
                });
            }

            var frame = new FrameReplay
            {
                TempoMs = agoraMs,
                Jogadores = snapshotJogadores,
                Eventos = new List<EventoReplay>(_eventosPendentes)
            };

            _eventosPendentes.Clear();
            _filme.Frames.Add(frame);
            _ultimoFrameMs = agoraMs;
        }

        public FilmeReplay FinalizarESalvar(string pastaSaida)
        {
            if (_relogio.IsRunning)
            {
                RegistrarEvento("fim_partida");
                CapturarTick(Array.Empty<PosicaoJogador>(), forcar: true);
                _relogio.Stop();
            }

            Directory.CreateDirectory(pastaSaida);
            string caminhoArquivo = Path.Combine(pastaSaida, $"{_filme.Cabecalho.PartidaId}.ndjson");
            NdjsonReplayStorage.Salvar(caminhoArquivo, _filme);
            return _filme;
        }
    }
}
