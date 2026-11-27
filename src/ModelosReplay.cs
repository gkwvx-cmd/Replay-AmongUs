using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GkwvxYtMods.Replay
{
    public sealed class CabecalhoReplay
    {
        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "cabecalho";

        [JsonPropertyName("versao")]
        public int Versao { get; set; } = 3;

        [JsonPropertyName("origem")]
        public string Origem { get; set; } = "gkwvxyt-br";

        [JsonPropertyName("tema")]
        public string Tema { get; set; } = "gkwvxYT";

        [JsonPropertyName("partidaId")]
        public string PartidaId { get; set; } = string.Empty;

        [JsonPropertyName("mapa")]
        public string Mapa { get; set; } = "The Skeld";

        [JsonPropertyName("inicioUtc")]
        public DateTime InicioUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class PosicaoJogador
    {
        [JsonPropertyName("id")]
        public byte JogadorId { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("cor")]
        public string Cor { get; set; } = "laranja";

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("vivo")]
        public bool Vivo { get; set; } = true;
    }

    public sealed class EventoReplay
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("autor")]
        public string? Autor { get; set; }

        [JsonPropertyName("alvo")]
        public string? Alvo { get; set; }

        [JsonPropertyName("detalhe")]
        public string? Detalhe { get; set; }
    }

    public sealed class FrameReplay
    {
        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "frame";

        [JsonPropertyName("tMs")]
        public long TempoMs { get; set; }

        [JsonPropertyName("jogadores")]
        public List<PosicaoJogador> Jogadores { get; set; } = new();

        [JsonPropertyName("eventos")]
        public List<EventoReplay> Eventos { get; set; } = new();
    }

    public sealed class FilmeReplay
    {
        public CabecalhoReplay Cabecalho { get; set; } = new();
        public List<FrameReplay> Frames { get; set; } = new();
    }
}
