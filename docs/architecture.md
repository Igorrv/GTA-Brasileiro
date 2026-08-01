# Arquitetura técnica — Cidade do Caos

> Visão de engenharia do projeto Unity 6. Complementa [12-tecnologia-implementacao.md](12-tecnologia-implementacao.md).

## Princípios

1. **Assemblies isolados** — cada `Caos.*.asmdef` tem dependências explícitas e unidirecionais.
2. **Data-driven** — conteúdo jogável vive em JSON (`StreamingAssets/Data`), não hardcoded em MonoBehaviour.
3. **Runtime-first** — cidade, texturas e áudio são gerados no boot (sem pack de assets de terceiros).
4. **Mobile budget** — static batching, materiais compartilhados, pooling, HUD throttled, radar a 12 Hz.
5. **Composition root único** — `GameManager` registra serviços, roda o tick ordenado e faz autosave.

## Grafo de assemblies

```
Caos.Core
    ↑
Caos.Data
    ↑
Caos.World
    ↑
Caos.Gameplay
    ↑           ↖
Caos.Save        Caos.Simulation
    ↑                 ↑
Caos.Bootstrap ───────┘ (Simulation → Save; Bootstrap → todos exceto Simulation)
```

| Assembly | Depende de | Papel |
|---|---|---|
| `Caos.Core` | — | `EventBus<T>`, `ServiceLocator`, `ITickable`, `GameStateMachine` |
| `Caos.Data` | Core | DTOs + `CatalogLoader` |
| `Caos.World` | Data | Relógio, clima, Caos, estrelas, bairro |
| `Caos.Gameplay` | World | Atributos, economia, reputação, eventos, missões |
| `Caos.Save` | Data, World, Gameplay | Persistência JSON |
| `Caos.Simulation` | Save (+ camadas abaixo via locator) | Cena, física, cidade, UI |
| `Caos.Bootstrap` | Core…Save | `GameBootstrapper` + `GameManager` |

`Simulation` **não** referencia `Bootstrap`. Serviços são resolvidos por `ServiceLocator` após o boot.

## Boot sequence

```
BeforeSceneLoad
  └─ GameBootstrapper.EnsureGameManager()
       └─ cria [GameManager] (DontDestroyOnLoad)

Play (cena Bootstrap vazia)
  └─ MobilePerf (60 fps, shadows, fixedDt)
  └─ GameManager.Awake / Start
       ├─ carrega catálogos + save
       ├─ registra serviços no ServiceLocator
       └─ inicia tick loop (Order)
  └─ WorldBuilder
       ├─ CityGenerator (layout + quarteirões + props)
       ├─ sol / névoa / DayNightLighting
       ├─ player + câmera + veículo
       ├─ tráfego · pedestres · polícia
       ├─ comércio · missões · rádio
       └─ HUD · minimapa · touch · pause
```

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

Carregados por `CatalogLoader` → DTOs em `Caos.Data` → consumidos por Simulation/Gameplay.

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
| Nova regra de economia | `Caos.Gameplay` |
| Novo prop urbano | `CityProps` / `CityGenerator` |
| Nova tela UI | `Caos.Simulation` (MonoBehaviour + EventBus) |
| Novo serviço global | Registrar no `GameManager` + `ITickable` se precisar de tick |

**Regra de ouro:** não criar dependência de `Simulation` → `Bootstrap`, nem de camadas baixas → `Simulation`.
