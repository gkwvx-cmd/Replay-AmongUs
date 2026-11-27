# gkwvxYT — Among Us Replay (NDJSON Offline & Visualizador)

Projeto em C# (`.NET 6.0`) com identidade e tema **gkwvxYT** para serialização, leitura, validação e visualização offline de replays em formato **NDJSON**, acompanhado de testes unitários com partidas sintéticas.

## Estrutura do projeto
* `src/GkwvxYtReplay.csproj` — projeto principal do gravador/leitor e visualizador em NDJSON.
* `src/ModelosReplay.cs` — modelos de cabeçalho, posições de jogadores, eventos e frames.
* `src/NdjsonReplayStorage.cs` — leitura/escrita em NDJSON com limite de tamanho, limite de frames e validação de ordem temporal.
* `src/VisualizadorReplay.cs` — visualizador independente no terminal com resumo da partida e eventos.
* `src/Program.cs` — gerador de partida sintética de exemplo e ponto de entrada do visualizador.
* `tests/GkwvxYtReplay.Tests.csproj` e `tests/ReplayNdjsonTests.cs` — testes automatizados com partidas sintéticas e cenários inválidos/excedentes.

## Como executar e testar
1. Certifique-se de ter o **.NET 6.0 SDK** (ou superior compatível) instalado.
2. Para gerar/ler uma partida sintética e abrir o visualizador:
   ```bash
   dotnet run --project src/GkwvxYtReplay.csproj
