# Changelog

Todas as mudanças notáveis deste projeto são documentadas aqui.
Formato inspirado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).

## [Unreleased]

### Added

- **Missões diárias (S9, docs/07 §7.5):** catálogo `dailies.json` com 10 tarefas (D01–D10) —
  entregas VaiJá, corre do Motoclube, carga de caminhoneiro, tour fotográfico, bicos de
  pedreiro/garçom, panfletagem e olheiro — com recompensa em R$, XP, reputação de facção
  e bônus de atributo (Sanidade)
- `DailyMissionService`: sorteia 5 diárias por dia de jogo, determinístico por dia+semente
  do mundo (todo mundo do mesmo mundo vê o mesmo lote); virada de dia renova o lote;
  quem começou uma diária termina mesmo depois da virada; fechar as 5 paga bônus de XP
- App **Diárias** no celular: lista as 5 do dia com estado (disponível/NO AR/FEITA),
  recompensa e botão Rastrear/Desistir
- `MissionTracker` em modo diária: beacon, linha do GPS no minimapa e painel de missão
  passam a apontar os passos da diária; a campanha congela no passo atual e volta de
  onde parou ao concluir ou desistir
- Notificações no HUD: diária concluída, lote renovado e bônus do lote completo
- Save v3: lote do dia, concluídas e diária em andamento (com o passo) persistidos;
  saves antigos (v1/v2) abrem normalmente e sorteiam o lote do dia
- `DiariasTests` — 12 testes NUnit do `DailyMissionService` na rede EditMode (sorteio, determinismo,
  recompensas, virada de dia, save)
- **Núcleo de regras sem engine** — `Caos.Core`, `Caos.Data`, `Caos.World` e `Caos.Gameplay` passam a
  compilar sem `UnityEngine` (`noEngineReferences: true`)
- `Caos.Core`: `CaosLog` (log com destino plugável e piso de severidade), `CaosMath` (as funções de
  `Mathf` que o núcleo usava), `IRandomSource`/`CaosRandom` (fluxo de sorteio próprio e semeado) e
  `CaosRuntime.Reiniciar()` (ponto único que zera o estado estático no boot)
- `Caos.Content` — novo assembly só para o `CatalogLoader` (arquivo, `UnityWebRequest`, `JsonUtility`),
  separando I/O da engine dos catálogos em si
- **90 testes NUnit** em `Assets/Tests/EditMode` cobrindo economia/IPC-Caos, XP, missões, atributos,
  reputação, relógio, estado do mundo, eventos, barramento, ServiceLocator e sessão
- `Caos.Tests` (EditMode) + `com.unity.test-framework` — os mesmos testes rodam no Test Runner
- `tools/testes-sem-unity/` — gera projetos .NET a partir dos `.asmdef` e roda os testes sem licença
  de Unity
- `scripts/verificar_arquitetura.py` — guarda do grafo: ciclos, `using` sem referência no asmdef,
  `UnityEngine` vazando para o núcleo, asset sem `.meta`
- `.github/workflows/ci.yml` — arquitetura + testes a cada push e pull request
- `EventBus.LimparTudo()`, `EventBus<T>.Assinantes`, `ServiceLocator.Unregister<T>()` e
  `ServiceLocator.Registrados`
- Portal interativo Docsify em `docs/` (cover, sidebar, busca, tema Caos)
- `docs/architecture.md` — mapa de assemblies, boot e extensão
- Scaffolding de repositório: `CONTRIBUTING`, `LICENSE`, `SECURITY`, `CODE_OF_CONDUCT`
- Templates GitHub (issues, PR) e workflow de Pages
- README profissional com badges, mermaid e atalhos

### Fixed

- `EventBus<T>.Publish` entregava iterando a lista viva: cancelar assinatura dentro de um handler
  fazia o assinante seguinte perder o evento
- `EventBus<T>.Publish` descartava em silêncio um evento publicado de dentro de um handler do mesmo
  tipo; agora entrega e corta só o ciclo infinito (limite de profundidade)
- Um handler que estourasse exceção interrompia a entrega para os demais — a regra de jogo caía junto
  com um erro de UI
- `ReputationService.Hydrate` somava o valor salvo ao corrente em vez de escrever por cima: carregar
  um save sobre uma sessão já jogada dobrava a reputação
- `PlayerAttributes` republicava `PlayerMorreu` a cada frame com a saúde zerada, refazendo o respawn
  60 vezes por segundo durante a tela de WASTED
- `GameManager` assinava `MissaoConcluida` com lambda e nunca cancelava — sem recarga de domínio, um
  handler morto sobrava por sessão e o XP era creditado em duplicata
- `GameManager.RegisterServices()` chamava `ServiceLocator.Reset()` no meio do boot, apagando o
  catálogo de resgate que a Simulation podia ter registrado

### Changed

- Ordem do boot: `CaosRuntime.Reiniciar()` roda em `SubsystemRegistration`, antes de `MobilePerf`,
  `GameManager`, `WorldBuilder` e `MainMenu` — a ordem relativa entre esses pontos de entrada deixa
  de importar
- Logs de `Caos.Bootstrap`, `Caos.Save` e do núcleo passam pelo `CaosLog`; em build de loja o piso
  sobe para `Aviso` e as mensagens de `Info` deixam de montar string
- `EventSystem` recebe `IRandomSource` (opcional) e deixa de sortear do `Random` global que a geração
  da cidade também semeia; o tick também deixou de alocar duas listas por frame

## [0.8.0] — 2026-08 — Slice S1–S8 jogável

### Added

- Cidade runtime **São Genésio** (960×960 m, 9 bairros, malha nomeada)
- Personagem a pé (rig procedural, pulo, agachar, sentar)
- 36 veículos + física Ackermann, combustível, buracos, dano
- Trânsito, pedestres, polícia, procurado, BUSTED/WASTED
- Comércio BR, rádio sintetizada (6 estações), radar, HUD, touch
- 16 missões encadeadas, save/autosave, menu de pausa
- Assemblies `Caos.*` + catálogos JSON em `StreamingAssets/Data`

[Unreleased]: https://github.com/caosstudio/cidade-do-caos/compare/v0.8.0...HEAD
[0.8.0]: https://github.com/caosstudio/cidade-do-caos/releases/tag/v0.8.0
