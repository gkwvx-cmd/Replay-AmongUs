using System;
using System.IO;
using System.Text;
using GkwvxYtMods.Replay;
using Xunit;

namespace GkwvxYtMods.Replay.Tests
{
    public class ReplayNdjsonTests
    {
        [Fact]
        public void SalvarECarregar_PartidaSintetica_PreservaDados()
        {
            string arquivoTemp = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.ndjson");
            try
            {
                var original = Program.CriarPartidaSintetica();
                NdjsonReplayStorage.Salvar(arquivoTemp, original);

                var carregado = NdjsonReplayStorage.Carregar(arquivoTemp);

                Assert.Equal("gkwvxyt-demo-001", carregado.Cabecalho.PartidaId);
                Assert.Equal("gkwvxYT", carregado.Cabecalho.Tema);
                Assert.Equal(3, carregado.Frames.Count);
                Assert.Equal("Faxineiro", carregado.Frames[0].Jogadores[0].Nome);
                Assert.False(carregado.Frames[2].Jogadores[0].Vivo);
            }
            finally
            {
                if (File.Exists(arquivoTemp))
                    File.Delete(arquivoTemp);
            }
        }

        [Fact]
        public void Carregar_QuandoExcedeLimiteDeBytes_LancaExcecao()
        {
            string arquivoTemp = Path.Combine(Path.GetTempPath(), $"replay_grande_{Guid.NewGuid():N}.ndjson");
            try
            {
                var filme = Program.CriarPartidaSintetica();
                NdjsonReplayStorage.Salvar(arquivoTemp, filme);

                Assert.Throws<InvalidOperationException>(() =>
                    NdjsonReplayStorage.Carregar(arquivoTemp, maxBytes: 120));
            }
            finally
            {
                if (File.Exists(arquivoTemp))
                    File.Delete(arquivoTemp);
            }
        }

        [Fact]
        public void CarregarDeStream_SemCabecalhoPrimeiro_LancaInvalidDataException()
        {
            string ndjsonInvalido = "{\"tipo\":\"frame\",\"tMs\":100,\"jogadores\":[],\"eventos\":[]}\n";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonInvalido));

            Assert.Throws<InvalidDataException>(() =>
                NdjsonReplayStorage.CarregarDeStream(ms));
        }

        [Fact]
        public void CarregarDeStream_TimestampForaDeOrdem_LancaInvalidDataException()
        {
            string ndjsonForaDeOrdem =
                "{\"tipo\":\"cabecalho\",\"versao\":3,\"origem\":\"gkwvxyt-br\",\"tema\":\"gkwvxYT\",\"partidaId\":\"p1\",\"mapa\":\"Polus\"}\n" +
                "{\"tipo\":\"frame\",\"tMs\":500,\"jogadores\":[],\"eventos\":[]}\n" +
                "{\"tipo\":\"frame\",\"tMs\":300,\"jogadores\":[],\"eventos\":[]}\n";

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(ndjsonForaDeOrdem));

            Assert.Throws<InvalidDataException>(() =>
                NdjsonReplayStorage.CarregarDeStream(ms));
        }
    }
}
