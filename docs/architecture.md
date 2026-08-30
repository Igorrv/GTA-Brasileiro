# Arquitetura técnica — Cidade do Caos

> Visão de engenharia do projeto Unity 6. Complementa [12-tecnologia-implementacao.md](12-tecnologia-implementacao.md).

## Princípios

1. **Assemblies isolados** — cada `Caos.*.asmdef` tem dependências explícitas e unidirecionais.
2. **Núcleo sem engine** — Core, Data, World e Gameplay compilam sem `UnityEngine`, e por isso são testados fora do Editor.
3. **Data-driven** — conteúdo jogável vive em JSON (`StreamingAssets/Data`), não hardcoded em MonoBehaviour.
4. **Runtime-first** — cidade, texturas e áudio são gerados no boot (sem pack de assets de terceiros).
5. **Mobile budget** — static batching, materiais compartilhados, pooling, HUD throttled, radar a 12 Hz.
6. **Composition root único** — `GameManager` registra serviços, roda o tick ordenado e faz autosave.

## Grafo de assemblies

```
                Caos.Core          ← sem engine
                    ↑
                Caos.Data          ← sem engine (só DTOs + GameCatalogs)
                    ↑
                Caos.World         ← sem engine
                    ↑
                Caos.Gameplay      ← sem engine
                 ↑   ↑   ↑
       Caos.Save ┘   │   └ Caos.Tests (EditMode)
            ↑        │
  Caos.Simulation ───┘
            ↑
     Caos.Bootstrap  → Core, Data, Content, World, Gameplay, Save

  Caos.Content (Unity: arquivo, UnityWebRequest, JsonUtility) → Core, Data
```

| Assembly | Engine? | Depende de | Papel |
|---|---|---|---|
| `Caos.Core` | não | — | `EventBus<T>`, `ServiceLocator`, `ITickable`, `GameStateMachine`, `CaosLog`, `CaosMath`, `IRandomSource`, `CaosRuntime` |
| `Caos.Data` | não | Core | DTOs + `GameCatalogs` |
| `Caos.World` | não | Core, Data | Relógio, clima, Caos, estrelas, bairro |
| `Caos.Gameplay` | não | Core, Data, World | Atributos, economia, reputação, eventos, missões |
| `Caos.Content` | **sim** | Core, Data | `CatalogLoader` — lê `StreamingAssets/Data` |
| `Caos.Save` | **sim** | Core, Data, World, Gameplay | Persistência JSON |
| `Caos.Simulation` | **sim** | Core…Save (+ URP) | Cena, física, cidade, UI |
| `Caos.Bootstrap` | **sim** | Core…Save + Content | `GameBootstrapper` + `GameManager` |
| `Caos.Tests` | Editor | Core, Data, World, Gameplay | Testes EditMode das regras |

`Simulation` **não** referencia `Bootstrap`. Serviços são resolvidos por `ServiceLocator` após o boot.

### Por que "sem engine" importa

Quatro assemblies estão marcados com `"noEngineReferences": true`. Não é purismo: é o que permite
compilar e rodar as regras do jogo num runner comum de .NET, sem licença de Unity — que é como o CI
testa economia, IPC-Caos, XP, missões, atributos, reputação e barramento a cada pull request
(`tools/testes-sem-unity/`).

Para isso o núcleo tem substitutos das poucas APIs de engine que usava:

| Antes | Agora | Onde |
|---|---|---|
| `UnityEngine.Debug.Log` | `CaosLog.Info` / `.Aviso` / `.Erro` | `Caos.Core/CaosLog.cs` |
| `UnityEngine.Mathf` | `CaosMath` (mesmas fórmulas, inclusive a tolerância do `Approximately`) | `Caos.Core/CaosMath.cs` |
| `UnityEngine.Random` | `IRandomSource` / `CaosRandom` (fluxo próprio, semeado) | `Caos.Core/IRandomSource.cs` |

O `CaosLog` só imprime através de um **destino**, instalado pelo Bootstrap. Isso dá também um corte
de custo no celular: em build de loja o piso sobe para `Aviso` e as mensagens de `Info` somem.

## Guardas automáticas

| Comando | O que garante |
|---|---|
| `python3 scripts/verificar_arquitetura.py` | Grafo acíclico e unidirecional, `using` batendo com o asmdef, núcleo sem `UnityEngine`, todo asset com `.meta` |
| `python3 tools/testes-sem-unity/gerar_projetos.py && dotnet test tools/testes-sem-unity/gerado/Caos.Testes/Caos.Testes.csproj` | Regras de jogo (90 casos) compilando e passando sem Unity |

Ambos rodam no CI (`.github/workflows/ci.yml`).

## Boot sequence

```
SubsystemRegistration                      ← antes de qualquer cena
  └─ GameBootstrapper.PrepararRuntime()
       ├─ instala o destino do CaosLog (Console) e o piso de log
       └─ CaosRuntime.Reiniciar()          ← zera EventBus + ServiceLocator + GameSession

BeforeSceneLoad
  ├─ MobilePerf (60 fps, sombras, fixedDt) │ a ordem entre estes dois é
  └─ GameBootstrapper.EnsureGameManager()  │ indiferente: o runtime já está limpo
       └─ cria [GameManager] (DontDestroyOnLoad)

Play (cena Bootstrap vazia)
  └─ GameManager.Awake / Start
       ├─ carrega catálogos (Caos.Content) + save
       ├─ registra serviços no ServiceLocator (sem Reset: a tabela é compartilhada)
       └─ inicia tick loop (Order)
  └─ WorldBuilder
       ├─ CityGenerator (layout + quarteirões + props)
       ├─ sol / névoa / DayNightLighting
       ├─ player + câmera + veículo
       ├─ tráfego · pedestres · polícia
       ├─ comércio · missões · rádio
       └─ HUD · minimapa · touch · pause
```

**Estático e Play Mode.** O jogo guarda estado em estáticos de propósito (barramento, registro de
serviços, sessão), porque são coisas que atravessam assemblies sem criar dependência entre eles. O
preço é que estático não morre com a cena: sobrevive ao "Enter Play Mode" sem recarga de domínio e ao
retorno ao menu numa build. `CaosRuntime.Reiniciar()` é o único ponto que zera tudo isso, e roda na
primeira fase da inicialização — antes de MobilePerf, GameManager, WorldBuilder e MainMenu, de modo
que a ordem relativa entre esses pontos de entrada deixa de importar.

## Fluxo de um frame (tick)

Serviços que implementam `ITickable` avançam em ordem fixa no `GameManager` — evita corridas entre economia, eventos e mundo.

```
TimeOfDay → WorldState → Attributes decay → Events → Missions → (Simulation Update separado)
```

UI escuta **eventos** (`EventBus`) em vez de pollar atributos todo frame (`HudController` ~10 Hz).

## Cidade em runtime

| Classe | Responsabilidade |
|---|---|
| `CityLayout` | Malha viária, bairros, nomes, rotas |
| `CityGenerator` | Quarteirões, fachadas, marcos, `StaticBatchingUtility.Combine` |
| `CityPalette` / `CityTextures` | Materiais e tiles procedurais (128²) |
| `CityProps` | Poste, orelhão, ponto de ônibus, quebra-molas… |
| `VehicleFactory` | Silhueta por `BodyStyle` do catálogo |

## Sistemas de simulação (mapa mental)

```
PlayerController ──► ThirdPersonCamera
       │
       ├── PlayerVehicleLink ──► VehicleController + VehicleHealth
       ├── InteractionScanner ──► Interactable (lojas, posto, banco…)
       └── PlayerLifecycle (BUSTED / WASTED)

TrafficSystem · PedestrianSystem · PoliceSystem  ← ObjectPool
CrimeSystem → Stars → PoliceSystem
MissionTracker → MissionService (Gameplay)
AudioManager + RadioSystem (PCM sintetizado)
MinimapSystem · HudController · TouchControls · PhoneUI · PauseMenu
```

## Dados

```
Assets/StreamingAssets/Data/
  vehicles.json   items.json   shops.json
  radio.json      events.json  missions.json
  districts.json  streets.json factions.json
```

Carregados por `CatalogLoader` (`Caos.Content`) → DTOs em `Caos.Data` → consumidos por
Simulation/Gameplay. O carregador mora fora de `Caos.Data` justamente porque ele é a única parte dos
dados que precisa da engine (arquivo, `UnityWebRequest`, `JsonUtility`); assim o catálogo em si
continua sendo C# puro e testável.

## Performance (checklist)

- Static batching por quarteirão
- Materiais quantizados (`CityPalette.Mat` + `CorViva`)
- Props decorativos sem Collider
- Pooling em tráfego / pedestres / polícia
- Radar: câmera off + `Render()` a 12 Hz
- Densidade de tráfego × hora do dia
- EventBus com structs (zero alloc no publish)

## Extensão segura

| Quero adicionar… | Onde |
|---|---|
| Novo item / loja / missão | JSON em `StreamingAssets/Data` (+ DTO se campo novo) |
| Nova regra de economia | `Caos.Gameplay` — **com teste** em `Assets/Tests/EditMode` |
| Novo prop urbano | `CityProps` / `CityGenerator` |
| Nova tela UI | `Caos.Simulation` (MonoBehaviour + EventBus) |
| Novo serviço global | Registrar no `GameManager` + `ITickable` se precisar de tick |
| Nova regra que precisa de sorteio | Receber `IRandomSource` no construtor, nunca `Random` global |

**Regra de ouro:** não criar dependência de `Simulation` → `Bootstrap`, nem de camadas baixas → `Simulation`.
Se `Caos.Gameplay` precisar de `UnityEngine`, a regra está no lugar errado — ela pertence à Simulation,
ou falta um substituto em `Caos.Core`.
