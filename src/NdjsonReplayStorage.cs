using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GkwvxYtMods.Replay
{
    public static class NdjsonReplayStorage
    {
        public const long TamanhoMaximoPadraoBytes = 10 * 1024 * 1024;
        public const int MaximoFramesPadrao = 50_000;
        public const int TamanhoMaximoLinhaBytes = 256 * 1024;

        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static void Salvar(
            string caminhoArquivo,
            FilmeReplay filme,
            long maxBytes = TamanhoMaximoPadraoBytes,
            int maxFrames = MaximoFramesPadrao)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
                throw new ArgumentException("Caminho de arquivo inválido.", nameof(caminhoArquivo));
            if (filme == null)
                throw new ArgumentNullException(nameof(filme));
            if (filme.Frames.Count > maxFrames)
                throw new InvalidOperationException($"Limite de frames excedido ({filme.Frames.Count} > {maxFrames}).");

            string? diretorio = Path.GetDirectoryName(caminhoArquivo);
            if (!string.IsNullOrEmpty(diretorio))
                Directory.CreateDirectory(diretorio);

            using var mem = new MemoryStream();
            using (var writer = new StreamWriter(mem, new UTF8Encoding(false), bufferSize: 16 * 1024, leaveOpen: true))
            {
                filme.Cabecalho.Tipo = "cabecalho";
                writer.WriteLine(JsonSerializer.Serialize(filme.Cabecalho, OpcoesJson));

                long ultimoTempo = -1;
                foreach (var frame in filme.Frames)
                {
                    if (frame.TempoMs < 0 || frame.TempoMs < ultimoTempo)
                        throw new InvalidDataException($"Timestamp fora de ordem no frame: {frame.TempoMs} ms.");

                    ultimoTempo = frame.TempoMs;
                    frame.Tipo = "frame";
                    writer.WriteLine(JsonSerializer.Serialize(frame, OpcoesJson));
                }

                writer.Flush();
            }

            if (maxBytes > 0 && mem.Length > maxBytes)
                throw new InvalidOperationException($"Replay excede o limite de {maxBytes} bytes ({mem.Length} bytes).");

            File.WriteAllBytes(caminhoArquivo, mem.ToArray());
        }

        public static FilmeReplay Carregar(
            string caminhoArquivo,
            long maxBytes = TamanhoMaximoPadraoBytes,
            int maxFrames = MaximoFramesPadrao)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo) || !File.Exists(caminhoArquivo))
                throw new FileNotFoundException("Arquivo de replay NDJSON não encontrado.", caminhoArquivo);

            var info = new FileInfo(caminhoArquivo);
            if (maxBytes > 0 && info.Length > maxBytes)
                throw new InvalidOperationException($"Arquivo muito grande ({info.Length} bytes > {maxBytes} bytes).");

            using var stream = new FileStream(caminhoArquivo, FileMode.Open, FileAccess.Read, FileShare.Read);
            return CarregarDeStream(stream, maxBytes, maxFrames);
        }

        public static FilmeReplay CarregarDeStream(
            Stream stream,
            long maxBytes = TamanhoMaximoPadraoBytes,
            int maxFrames = MaximoFramesPadrao)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 16 * 1024, leaveOpen: true);
            var filme = new FilmeReplay();
            bool leuCabecalho = false;
            long bytesLidos = 0;
            long ultimoTempo = -1;
            int numeroLinha = 0;

            string? linha;
            while ((linha = reader.ReadLine()) != null)
            {
                numeroLinha++;
                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                int bytesLinha = Encoding.UTF8.GetByteCount(linha);
                if (bytesLinha > TamanhoMaximoLinhaBytes)
                    throw new InvalidDataException($"Linha {numeroLinha} excede o limite de {TamanhoMaximoLinhaBytes} bytes.");

                bytesLidos += bytesLinha;
                if (maxBytes > 0 && bytesLidos > maxBytes)
                    throw new InvalidOperationException($"Leitura excedeu o limite máximo de {maxBytes} bytes.");

                using var doc = JsonDocument.Parse(linha);
                if (!doc.RootElement.TryGetProperty("tipo", out var tipoProp))
                    throw new InvalidDataException($"Linha {numeroLinha} sem campo obrigatório 'tipo'.");

                string? tipo = tipoProp.GetString();
                if (tipo == "cabecalho")
                {
                    if (leuCabecalho)
                        throw new InvalidDataException("Cabeçalho duplicado no arquivo NDJSON.");

                    filme.Cabecalho = JsonSerializer.Deserialize<CabecalhoReplay>(linha, OpcoesJson)
                                      ?? throw new InvalidDataException("Cabeçalho inválido.");
                    leuCabecalho = true;
                }
                else if (tipo == "frame")
                {
                    if (!leuCabecalho)
                        throw new InvalidDataException("O arquivo NDJSON deve começar com uma linha de cabeçalho.");

                    if (filme.Frames.Count >= maxFrames)
                        throw new InvalidOperationException($"Limite máximo de {maxFrames} frames atingido.");

                    var frame = JsonSerializer.Deserialize<FrameReplay>(linha, OpcoesJson)
                                ?? throw new InvalidDataException($"Frame inválido na linha {numeroLinha}.");

                    if (frame.TempoMs < 0 || frame.TempoMs < ultimoTempo)
                        throw new InvalidDataException($"Timestamp inválido na linha {numeroLinha}: {frame.TempoMs} ms.");

                    ultimoTempo = frame.TempoMs;
                    filme.Frames.Add(frame);
                }
                else
                {
                    throw new InvalidDataException($"Tipo de registro desconhecido na linha {numeroLinha}: '{tipo}'.");
                }
            }

            if (!leuCabecalho)
                throw new InvalidDataException("Arquivo NDJSON vazio ou sem cabeçalho.");

            return filme;
        }
    }
}
