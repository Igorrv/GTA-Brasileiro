# 12 — Tecnologia e Implementação (Unity 6 LTS)

> Arquitetura técnica de **Cidade do Caos: Mundo Aberto** (mobile-first). Leituras: [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md) (física de carro), [06-eventos-aleatorios.md](06-eventos-aleatorios.md) (eventos), [09-arte-estilo-visual.md](09-arte-estilo-visual.md) (arte/perf).

## 12.1 Engine e Stack

| Camada | Escolha | Justificativa |
|---|---|---|
| **Engine** | Unity 6 LTS (URP) | Melhor ecossistema mobile, C#, asset store,deploy Android+iOS+iDA |
| **Render** | URP (Universal RP) | Performance mobile, shaders custom, pós-processamento leve |
| **Linguagem** | C# (e HLSL/Shader Graph) | Padrão Unity, equipe fácil |
| **Física** | PhysX (built-in) + Articulation Body para veículos | Estável mobile |
| **Alto desempenho** | DOTS/ECS + Burst + Jobs (tráfego, multidões) | Centenas de NPCs a 60fps |
| **Dados** | ScriptableObjects + JSON | Design-driven, sem recompilar |
| **Streaming** | Addressables | Download de bairros sob demanda (APK leve) |
| **Backend/LiveOps** | Unity Gaming Services (Auth, Cloud Save, Remote Config, Economy, Analytics) ou autoral (PlayFab/Firebase) | F2P, passes, eventos remotos |
| **IA de missão (LLM)** | API externa (opcional) — só texto/diálogo | Ver [07.6](07-missoes.md) |
| **Áudio** | FMOD ou Wwise (opcional) + Unity Audio | Rádio dinâmico, música por bairro |

**Plataformas-alvo:**
- Android: min Android 9 (API 28), OpenGL ES 3.2 / Vulkan, 4 GB RAM+ (low-spec fallback).
- iOS: min iOS 14, A12+ (iPhone XS); Metal.
- PC (porto futuro): Windows/Mac, DX11/Vulkan/Metal, teclado+gamepad.

## 12.2 Arquitetura do Projeto

### 12.2.1 Assembly Definitions (separação de módulos)
```
Caos.Core           (GameManager, Events, Save, State machine)
Caos.World          (City, Districts, Weather, TimeOfDay, TrafficSystem)
Caos.Simulation     (DOTS: Traffic, Pedestrians, Crowd)   <- ECS
Caos.Vehicles       (VehicleController, Damage, Fuel, Physics)
Caos.AI             (NPC AI, Police AI, Faction AI, NavMesh)
Caos.Gameplay       (Missions, Reputation, Economy, Sanity, Crafting)
Caos.UI             (HUD, Menus, Map, Shop)               <- UI Toolkit/uGUI
Caos.Data           (ScriptableObjects: vehicles, items, missions, events)
Caos.Save           (Serializable state, Cloud sync)
Caos.Net            (LiveOps, RemoteConfig, Analytics, IAP, Ads)
```

### 12.2.2 Padrões arquiteturais
- **MVC/MVVM** na UI (UI Toolkit data binding ou uGUI + presenter).
- **Event bus / Scriptable Object events** para desacoplar sistemas (ex.: `OnSanityChanged`, `OnMissionComplete`).
- **State machine** global (Boot → MainMenu → Loading → Playing → Paused → Cutscene).
- **Service Locator** para sistemas singleton (EconomyService, ReputationService, WeatherService).

## 12.3 Estrutura de Dados

### 12.3.1 ScriptableObjects (design data)
- `VehicleSO` (nome, classe, massa, potência, 0–100, consumo, tanque, preço, prefab, stats).
- `ItemSO`, `MissionSO`, `EventSO`, `DistrictSO`, `FactionSO`, `NPCSO`, `RecipeSO` (gambiarras).
- Volumes: `VehicleCatalog`, `MissionCatalog`, `EventCatalog` (listas tipadas).

### 12.3.2 Estado do jogo (salvável)
```jsonc
SaveGame {
  player: { nome, genero, aparencia{}, atributos{saude,fome,energia,sanidade},
            nivel, xp, habilidades{}, dinheiroR$, caosCash,
            casa, posicao{x,y,z}, rot, bairroAtual, veiculoAtual },
  reputacao: { faccoes{caminhoneiros, milicia, motoclube, frente},
               bairros{vista, centro, monte, sitio, belvedere, itauna} },
  mundo: { dia, hora, clima, nivelCaos, estrelasProcurado,
           eventosAtivos[], cooldowns{} },
  garagem: [ {veiculoId, dano, combustivel, upgrades{}, skin} ],
  inventario: [ {itemId, qtd} ],
  missoes: { ativas[], completas[], diarias{data, feitas[]} },
  flags: { historia[], desbloqueios[] }
}
```
- **Save local:** JSON + criptografia leve; **Cloud Save** síncrono (Unity Cloud Save / Firebase).
- **Checkpoint:** autosave a cada evento de mundo e ao pausar/sair.

## 12.4 Sistema de Física (veículos)
- Rigidbody + **ArticulationBody/WheelCollider** por roda; centro de massa por classe (ver [04](04-sistemas-direcao-veiculos.md)).
- **Layer Collision Matrix** otimizada (veículo×carro, veículo×pedestre, etc.).
- Sub-stepping fixo (60 Hz) para estabilidade.
- **Aquaplanagem:** raycast na água + redução de aderência dinâmica.
- **Drift:** freio de mão + lateral friction curve ajustada.

## 12.5 Sistema de Tráfego (DOTS/ECS)
- **Por quê ECS:** centenas de carros/ônibus/motos a 60fps.
- **Entidades:** `Car` (posição, rota, velocidade, tipo, estadoAI).
- **Sistema de spawn** por bairro+horário (densidade em [04](04-sistemas-direcao-veiculos.md) e [02](02-narrativa-ambientacao.md)).
- **Grafo de vias (nav-mesh de carros):** waypoints + lanes; pathfinding A* pré-computado por OD (origem-destino).
- **AI de avoidance:** 3 camadas (curto prazo: não bater; médio: seguir lane; longo: rota).
- **Reações:** buzina, fuga, xingamento (áudio + animação simples).
- **Pooling:** carros reciclados fora da view; spawn/despawn por distância do jogador.
- **Corredor de moto:** motos usam lane "gap" entre carros.
- **Engarrafamento:** detectado por densidade baixa de velocidade numa via → dispara evento de trânsito / sobe Caos.

## 12.6 IA de NPCs (pedestres)

- **NavMesh** por bairro (baked, com off-mesh links para escadas/becos da favela).
- **Utility AI / FSM leve:** estados (Wander, Flee, Panic, Watch, Talk, Work, React).
- **Reatividade:** som (tiro/buzina) → Flee; ação do jogador (soco/arma) → Panic/testemunha → chama polícia.
- **Comportamentos cômicos:** anim/idle contextual (dançar no show, rezar no culto, comer no bar).
- **Crowd (ECS opcional):** multidões em eventos (show, comício) como entidades simples.
- **Variedade:** arquetipos (ver [03-personagens.md](03-personagens.md)) sorteados com seeds de aparência.

## 12.7 IA de Polícia (perseguição)

- **Sistema de estrelas (0–5)** como estado global de ameaça (ver [10.10](10-mecanicas-jogabilidade.md)).
- **Spawn progressivo:** quanto mais estrelas, mais unidades e tipos (Guarda → PM → Viatura → Bloqueio → Civil/Cerco).
- **Comportamento:** busca (patrulhar último local visto), perseguição (chase no veículo), bloqueio (fechar via), cerco (ânulo).
- **Detecção:** cones de visão + ouvir; barra de "fuga" ao quebrar linha de visão.
- **Pathfinding** via nav-mesh (a pé) e vias (de carro); previsão de interceptação simples.
- **Encerramento:** preso (respawn delegacia) OU escapou (estrelas zeram após tempo fora de visão).
- **Suborno/dispensa:** hooks com sistema de diálogo/Social.

## 12.8 IA de Facções

- **Controle territorial dinâmico:** cada distrito/quarteirão tem `controle[faccao]` (0–1); muda por ações do jogador e por eventos de escaramuça.
- **Relações entre facções:** matriz de simpatia (ver [00](00-biblia-do-mundo.md)); abaixo de −60 gera escaramuças (E20).
- **Comportamento de membro:** parambular no território, reagir a intrusos, reforçar em briga (se Rep aliada), hostilizar (se Rep inimiga).
- **Sistema de scheduler:** "Game Master" leve decide gatilhos narrativos/faccionais (sem ser LLM) com base no estado.

## 12.9 Sistema de Eventos Aleatórios

- Cada evento = `EventSO` com: `id, peso, bairros[], horarios[], climas[], prereq, opcoes[], consequencias[]`.
- **Spawner global** roda o cálculo de probabilidade em [06](06-eventos-aleatorios.md) a cada ~30s.
- **Cooldown e anti-repetição** por tipo; máx. 2–3 eventos simultâneos em low-end.
- **Resolução de opções:** UI radial/diálogo → aplicar consequências (atributos, R$, Rep, Caos, estrelas) via Event Bus.
- **NPCs/veículos de evento:** pooling; despawn ao resolver/timeout.

## 12.10 Otimização para Mobile

| Alvo | Low (4GB) | Mid (6GB) | High (8GB+) |
|---|---|---|---|
| Resolução | 0,7× (720p) | 1,0× | 1,5×/adaptive |
| Draw calls | < 150 | < 300 | < 500 |
| Tris/frame | < 200k | < 400k | < 800k |
| LOD | 4 níveis agressivo | 3 níveis | 3 níveis + HLOD |
| Sombra | 1 cascade, só perto | 2 cascade | 2 cascade + soft |
| NPCs em tela | ~40 | ~90 | ~150 (DOTS) |
| Alvo FPS | 30 estável | 60 | 60/120 |

**Técnicas-chave:**
- **GPU Instancing** (árvores, carros de tráfego, NPCs similares).
- **Object Pooling** universal (veículos, NPCs, projéteis, FX).
- **LOD + HLOD + Impostor** para prédios distantes.
- **Addressables** streaming por bairro (carrega/descarrega conforme jogador move).
- **Texture compression ASTC 6×6** (mobile), mip streaming.
- **Baked lighting (lightmaps)** + real-time só para sol/clima dinâmico.
- **Culling** (frustum + occlusion) por bairro.
- **DOTS** para tráfego/multidão; Mono/GO para gameplay (mistura controlada).
- **Frame Pacing / Adaptive Performance** (Samsung) para manter FPS sem superaquecer.
- **Garbage:** evitar alloc em hot path; struct em vez de class em loops; pooled lists.
- **Áudio:** compressed (Vorbis), instanciado, com limite de vozes simultâneas.

**Budget de calor/bateria:** perfil de 60fps sem throttaring em mid-range por ≥20 min; modo "Economia" (30fps) optativo.

## 12.11 Fluxo de Carga / Streaming de Mundo
```
GameManager → carrega bairro atual + adjacentes (Addressables)
           → instancia NPCs/veículos (pool) por densidade
           → ativa clima/tempo
           → jogador spawna na posição salva
```
- Transição suave entre bairros (pre-cache).
- Quit/suspend: salva e descarrega; retoma sem perda (Android lifecycle).

## 12.12 Multiplataforma e Controles
- **Touch:** esquemas A (botões) e B (gyro) — ver [10.12](10-mecanicas-jogabilidade.md).
- **PC:** mapeamento teclado+gamepad; mesmas ações.
- **i18n:** strings externalizadas (PT-BR primário); pronto para EN/ES.

## 12.13 Testes e CI
- **Testes unitários** (Unity Test Framework): economia (IPC-Caos), reputação, eventos (probabilidade).
- **Playmode tests:** missões (aceitar→objetivos→recompensa), perseguição policial.
- **CI:** build Android+iOS a cada PR; test em device farm (Firebase Test Lab) para low/mid/high.
- **Telemetria:** analytics de funil (onboarding, retenção, economia) → ajuste por Remote Config.

## 12.14 Riscos Técnicos (resumo)
| Risco | Mitigação |
|---|---|
| Física de carro instável mobile | Articulation Body + sub-step fixo; presets por classe |
| Tráfego pesado | DOTS/ECS + pooling + spawn por distância |
| Memória low-end | Addressables + ASTC + mip streaming |
| LLM de missão (offline/custo) | Fallback de templates estáticos; só prosa opcional |
| Save grande/corrompido | Save versionado + schema migration + cloud |

---
*Próximo:* [13-mvp-roadmap.md](13-mvp-roadmap.md) • *Índice:* [README.md](README.md)
