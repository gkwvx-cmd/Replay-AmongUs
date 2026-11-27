# gkwvxYT — Among Us Replay (NDJSON Offline & Visualizador)

​Projeto em C# (.NET 6.0) com identidade e tema gkwvxYT (gkwvx-cmd) para serialização, leitura, validação e visualização offline de replays em formato NDJSON, acompanhado de testes unitários com partidas sintéticas e integração com o mod GkwvxYtAmongUsReplay.dll para Among Us via BepInEx (IL2CPP) e HarmonyX.

# ​Requisitos

​SDK .NET 6.0 para compilar, executar o visualizador/leitor offline e rodar os testes unitários (xUnit).
​Among Us (PC ou Android) com BepInEx (BepInEx.Unity.IL2CPP e HarmonyX) configurado caso você vá carregar o mod dentro do jogo.
​Ferramenta de gerenciamento de arquivos (como ZArchiver no celular) caso esteja movendo a DLL compilada para a pasta do jogo.

#​ Estrutura do projeto

​src/GkwvxYtReplay.csproj — projeto principal do gravador/leitor e visualizador em NDJSON.

​src/ModelosReplay.cs — modelos de cabeçalho, posições de jogadores, eventos e frames.

​src/NdjsonReplayStorage.cs — leitura/escrita em NDJSON com limite de tamanho, limite de frames e validação de ordem temporal.

​src/VisualizadorReplay.cs — visualizador independente no terminal com resumo da partida e eventos.

​src/Program.cs — gerador de partida sintética de exemplo e ponto de entrada do visualizador.

​src/Plugin.cs — ponto de entrada do plugin gkwvxYT Replay no BepInEx.

​Replay/ReplayPatches.cs — patches do Harmony 
via reflexão (AccessTools) para capturar início, movimentação, eliminações, reuniões e encerramento sem exigir DLLs do jogo em tempo de compilação.

​mod/GkwvxYtAmongUsReplay.csproj — projeto de compilação da DLL do mod GkwvxYtAmongUsReplay.dll.

​tests/GkwvxYtReplay.Tests.csproj e tests/ReplayNdjsonTests.cs — testes automatizados com partidas sintéticas.

​.github/workflows/build.yml — pipeline de CI que executa os testes offline, compila a DLL do mod e publica o artefato.

# ​Instalação do Mod (GkwvxYtAmongUsReplay.dll)

​1. Abra a aba Actions no repositório do gkwvxYT (gkwvx-cmd) e selecione a execução concluída com sucesso.
​2. Baixe o artefato contendo GkwvxYtAmongUsReplay.dll na seção Artifacts.
​3. Acesse a pasta onde o BepInEx está instalado no Among Us (no Android, use o ZArchiver para navegar até a pasta do jogo).
​4. Copie GkwvxYtAmongUsReplay.dll para BepInEx/plugins/.
​5. Inicie o Among Us para carregar o mod.

# ​Onde os Replays São Salvos

​Durante a partida, o gravador registra frames de posição dos jogadores e eventos da sessão (eliminacao, reuniao_emergencia, fim_partida e saida_partida).

​Ao terminar ou sair da partida, o arquivo .ndjson é salvo em BepInEx/GkwvxYtReplays/.

# ​Como Compilar e Rodar Offline

​Testes automatizados: dotnet test tests/GkwvxYtReplay.Tests.csproj
​Visualizador offline: dotnet run --project src/GkwvxYtReplay.csproj
​Build do mod: dotnet build mod/GkwvxYtAmongUsReplay.csproj -c Release
​Exemplo Prático em NDJSON