# 04 — Sistemas de Direção e Veículos

> **GDD Técnico — Piloto Veicular Mobile.** Este documento define como veículos funcionam em **Cidade do Caos: Mundo Aberto**, da física de chassis ao tráfego urbano e aos rachas ilegais.
>
> **Fonte de verdade:** [00-biblia-do-mundo.md](00-biblia-do-mundo.md) — nomes, escalas e convenções canônicos.
> **Personagens relacionados:** [03-personagens.md](03-personagens.md) (Dr. Éverton, Seu Otacílio, Tavinho, Tonho da Van).
> **Sequência:** ver também [05-sistemas-jogo.md](05-sistemas-jogo.md) ao final.
>
> **Convenções usadas neste arquivo:**
> - Massa em kg | Potência em cv (cavalos-vapor) | 0–100 em s | Consumo em km/L | Tanque em L | Velocidade em km/h.
> - Preços em **Real (R$)** soft currency, sujeitos ao **IPC-Caos** (inflação semanal; ver [05-sistemas-jogo.md](05-sistemas-jogo.md)).
> - "Dirigibilidade" é uma nota 1–5 (1 = difícil/punishing, 5 = fácil/acessível) refletindo手感 touch e curva de aprendizado no mobile.

---

## Sumário

1. [Visão Geral do Sistema Veicular](#1-visão-geral-do-sistema-veicular)
2. [Física de Direção Realista](#2-física-de-direção-realista)
3. [Sistema de Dano](#3-sistema-de-dano)
4. [Combustível](#4-combustível)
5. [Oficina do Dr. Éverton](#5-oficina-do-dr-éverton)
6. [Tipos de Veículos — Comportamento por Classe](#6-tipos-de-veículos--comportamento-por-classe)
7. [Tabela de Veículos (catálogo de São Genésio)](#7-tabela-de-veículos-catálogo-de-são-genésio)
8. [Tráfego Inteligente](#8-tráfego-inteligente)
9. [Corridas Urbanas Ilegais (Racha)](#9-corridas-urbanas-ilegais-racha)
10. [Apêndices Técnicos (Unity)](#10-apêndices-técnicos-unity)

---

## 1. Visão Geral do Sistema Veicular

### 1.1 Filosofia de Design

Veículos em **Cidade do Caos** não são cascas gráficas que deslizam — são **corpos físicos pesados com personalidade própria**. Cada classe deve sentir diferente nas pontas dos dedos do jogador: a calombada de um Opala saindo de trás, a sensação de "cortar vento" numa moto 125 subindo o morro da Vista Alegre, o peso lento de um Scania arrancando com carreta cheia.

**Três princípios:**

1. **Peso é diversão.** Rigidbody pesado + centro de massa baixo = dirigibilidade que respeita inércia. Nada de "carro de papelão".
2. **Caos no trânsito é sistema, não decoracao.** Engarrafamento, corredor de moto, calombos e Aquaplanagem emergem da física + IA.
3. **Mobile-first sem comprometer profundidade.** Controles de toque com assists configuráveis (freio auto, direção assistida, câmera inteligente), mas opção "Simulação" para_hardcore players.

### 1.2 Classes Suportadas (canônicas)

| ID | Classe | Símbolo | Exemplos típicos |
|---|---|---|---|
| C1 | Carro Popular | 🚗 | Fusca-style, Uno, Chevette, Gol |
| C2 | Carro Esportivo | 🏎️ | Esportivos do Jardim Belvedere |
| C3 | Caminhonete / Pickup | 🛻 | Hilux, saveiro, D-20 |
| C4 | Caminhão | 🚛 | Caminhão de carga, carreta |
| C5 | Ônibus Urbano | 🚌 | Micro-ônibus, ônibus 8123 |
| C6 | Moto | 🏍️ | CG 160, esportivas, custom |
| C7 | Bicicleta | 🚲 | Aro 26, BMX, speed |
| C8 | Serviço / Especial | 🚕 | Táxi, VaiJá (app), entregador |
| C9 | Policial / Oficial | 🚓 | Viatura PM, blindado, rádio |

### 1.3 Componentes do Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                      NÚCLEO VEICULAR                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Física      │  │  Dano        │  │  Combustível │      │
│  │  (Rigidbody) │  │  (Visual+    │  │  (Tanque+    │      │
│  │              │  │   Performance│  │   Consumo)   │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                 │                 │               │
│         └────────────────┬┴─────────────────┘               │
│                          ▼                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │             JOGADOR (input touch)                   │    │
│  └─────────────────────────────────────────────────────┘    │
│                          ▼                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Oficina     │  │  Tráfego IA  │  │  Racha/      │      │
│  │  (Dr. Éverton│  │  (NPCs motor.│  │  Corridas    │      │
│  │   upgrades)  │  │   semáforos) │  │  (Motoclube) │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Física de Direção Realista

### 2.1 Modelo Físico Adotado

**Unity 6 LTS** com **`Rigidbody` 3D** + **Wheel Colliders** (4–18 rodas conforme classe) + **`ArticulationBody`** para articulações de carreta/caminhão. Não usamos "arcade kinematic"; o chassis é um corpo rígido real respondendo a torque, fricção lateral/longitudinal (curva Pacejka simplificada), downforce e carga dinâmica por suspensão.

**Por que Rigidbody e não CharacterController/Vehicle Toolkit custom:**

- Wheel Collider nativo é suficiente e otimizado para mobile após ajustes (`Force ApppliedDistance`, `Substeps`).
- Mantém interoperabilidade com o sistema de dano (deformação por mesh vertex) e IA de tráfego.
- Articulação de carreta usa `ArticulationBody` para estabilidade (joint fixa/revolute em cadeia trator+reboque).

**Stack de física (configuração do projeto):**

| Parâmetro | Valor | Nota |
|---|---|---|
| `Fixed Timestep` | 0,02 (50 Hz) | Padrão; suficiente com substeps. |
| `Solver Iterations` | 8 solver / 6 velocity | Aumenta precisão de colisão. |
| `Default Max Depenetration` | 0,1 | Evita "saltos" em impactos rasos. |
| `Sleep Threshold` | 0,05 | Reduz processamento parado. |
| Wheel Collider Substeps | 3 substeps abaixo de 8 m/s | Cuida de slow-mo em manobras. |
| `Layer Collision Matrix` | otimizada | Veículos ignoramNPC pedestres leves (apenas sensor). |

### 2.2 Atributos Físicos por Veículo (resumo executivo)

| Atributo | Range típico | Efeito no jogo |
|---|---|---|
| **Massa (kg)** | 80 (bike) – 28.000 (carreta Scania) | Inércia, freio, colisão (carro grande "empurra" pequeno). |
| **Centro de Massa (CoM)** | 0,0 a −0,4 m abaixo do pivot | Mais baixo = menos capotamento; motos têm CoM dinâmico por lean. |
| **Tração** | FWD / RWD / AWD | Define understeer/oversteer. Esportivos RWD derrapam mais. |
| **Aderência (μ)** | 0,6 (terra molhada) a 1,4 (pista seca asfalto) | Multiplicador de Pacejja; cai com chuva e óleo. |
| **Suspensão** | spring 20000–70000 N/m, damper 3500–7000 | Antigas (Fusca-style) mais macias; esportivos rígidos. |
| **Downforce** | 0–800 N a 100 km/h | Esportivos colam no chão; populares não têm. |
| **Raio de virada** | 4,2 m (moto) a 14 m (ônibus) | Impacta manobras urbanas. |
| **Raio de rolagem (anti-roll)** | 0,3–0,9 | Reduz tiltação em curva; caminhões têm mais. |

### 2.3 Suspensão, Aderência e Aquaplanagem

**Modelo de suspensão:** mola + amortecedor (`WheelCollider.suspensionSpring`) com parâmetros por classe (ver tabela 2.6).

**Aderência dinâmica (fórmula simplificada em runtime):**

```
μ_efetivo = μ_base × k_pneu × k_pista × k_chuva × k_dano_pneu
```

| Fator | Condição | Multiplicador |
|---|---|---|
| `k_pneu` | Pneu novo / cuidado / careca | 1,00 / 0,90 / 0,65 |
| `k_pista` | Asfalto / paralelepípedo / terra / areia | 1,00 / 0,85 / 0,70 / 0,55 |
| `k_chuva` | Seco / garoa / chuva forte / enchente | 1,00 / 0,92 / 0,78 / 0,60 |
| `k_dano_pneu` | 0% / 50% / 100% dano | 1,00 / 0,85 / 0,55 |

**Aquaplanagem (hidroplanagem):** Acima de **75 km/h em chuva forte** ou **55 km/h em enchente**, veículo entra em estado `AQUAPLANE`: perde 70% da fricção lateral por 0,6–2,2 s (tempo aleatório), direção perde resposta, jogador sente vibração de gamepad/HD e áudio de "shhhh". Recupera quando velocidade cai abaixo de 60 km/h ou pneu atinge asfalto seco.

**Derrapada (drift):** Triggered quando `slip lateral > 0,4` em RWD com acelerador >70% ou freio de mão acionado. Sistema gera partículas de fumaça, marcas de pneu no asfalto (decals persistidos por 90 s) e ganha progresso em conquista **"Rei do Asfalto"**. Carros com upgrade **Pneu Esportivo** driftam mais fácil.

### 2.4 Centro de Massa por Tipo de Veículo

| Tipo | CoM (relativo ao pivot chassis) | Tendência |
|---|---|---|
| Carro popular | (0, −0,35, −0,05) | Subesterço moderado |
| Esportivo | (0, −0,45, 0) | Neutro; overster em curva fechada |
| Pickup | (0, −0,30, +0,10) | Traseira "livre" sem carga |
| Caminhão | (0, −0,50, 0) | Capota em curva se vira brusco a 60+ km/h |
| Ônibus | (0, −0,40, +0,40) | Rolagem alta; freio cobra |
| Moto | dinâmico, acompanha lean | Curva inclinada; capota em freadas com direção |
| Bicicleta | dinâmico (jogador) | Capota em parada sem pé no chão |

### 2.5 Controles Touch

#### 2.5.1 Esquemas de controle (3 modos, alternáveis em Opções)

**Modo A — Botões (padrão, recomendado para iniciantes):**

```
┌──────────────────────────────────────────────┐
│                                              │
│                  CÂMERA (swipe)              │
│                                              │
│  ┌──────┐                          ┌──────┐ │
│  │  ◄   │  Direção (esq/dir)       │ ACEL │ │
│  └──────┘                          └──────┘ │
│  ┌──────┐                          ┌──────┐ │
│  │  ►   │                          │ FREIO │ │
│  └──────┘                          └──────┘ │
│                          ┌──────┐          │
│                          │ MÃO  │ (freio    │
│                          │ FREIO│ de mão)   │
│                          └──────┘          │
│  ┌──────┐  ┌──────┐                        │
│  │ NITRO│  │BUZINA│                        │
│  └──────┘  └──────┘                        │
└──────────────────────────────────────────────┘
```

**Modo B — Inclinação (gyro/accelerômetro):**

- Inclinar o aparelho para esquerda/direita = direção.
- Botões na tela: acelerar, freio, freio de mão, nitro, buzina.
- Calibração automática ao iniciar sessão.

**Modo C — Volante Virtual (swipe):**

- Swipe horizontal na metade inferior = direção; quanto mais longe do centro, maior ângulo.
- Botões de acelerar/frear aparecem como "pedais" fixos.

#### 2.5.2 Câmera

| Modo | Descrição | Gatilho |
|---|---|---|
| `CHASE` (padrão) | 3ª pessoa, atrás do veículo, mira suavizada. | Default |
| `FAR` | Mais recuada para veículos grandes (caminhão/ônibus). | Auto para C4/C5 |
| `HOOD` (capô) | 1ª pessoa no para-brisa; imersivo. | Toggle |
| `CINEMATIC` | Câmera diretor para replays de dano/explosão/racha. | Auto em eventos |
| `TOP` | Vista de cima para manobras apertadas (estacionar). | Toggle ao parar |

**Auto-câmera inteligente:** em curvas fechadas, a câmera afasta 1,2 m e sobe 0,4 m para mostrar trajetória. Em racha, câmera abaixa e approxima para sensação de velocidade.

#### 2.5.3 Assists configuráveis

| Assist | Padrão | Efeito |
|---|---|---|
| Freio automático em colisão iminente | ON (iniciante) | AIAux reduz 40% velocidade antes de impacto com tráfego. |
| Direção auto-corretiva | ON | Reduz oscilação em linha reta. |
| Anti-capotamento | ON | Aplica torque corretor se ângulo >45°. |
| ABS (anti-travamento) | ON | Evita derrapar em freada forte. |
| Controle de tração (TCS) | ON | Reduz patinação na arrancada. |
| Assistência de corredor (moto) | OFF | Auto-alinha moto entre carros parados. |

Modo **"Simulação"** desliga todos os assists (conquista "Piloto de Verdade").

### 2.6 Tabela de Parâmetros de Física por Classe

> Valores de referência usados pelos designers ao instanciar um veículo novo. Ajustes finos por veículo na Tabela 7.1.

| Classe | Massa (kg) | Tração típica | Spring (N/m) | Damper (N·s/m) | Aderência base μ | Raio virada (m) | 0–100 designer (s) | Vel. máx (km/h) |
|---|---|---|---|---|---|---|---|---|
| C1 Popular | 900–1.100 | FWD | 28.000 | 4.000 | 1,05 | 5,4 | 12–16 | 150–170 |
| C2 Esportivo | 1.200–1.500 | RWD/AWD | 45.000 | 6.500 | 1,25 | 5,8 | 4,5–7 | 240–300 |
| C3 Pickup | 1.800–2.400 | 4×4 | 38.000 | 5.200 | 1,10 | 6,4 | 9–13 | 170–190 |
| C4 Caminhão | 6.000–18.000 | 6×2/6×4 | 70.000 | 9.000 | 0,95 | 11,0 | 18–35 | 90–120 |
| C4b Carreta (combo) | 20.000–28.000 | 6×4 tractor | 80.000 | 10.000 | 0,92 | 13,5 | 30–45 | 95–110 |
| C5 Ônibus | 11.000–16.000 | RWD | 60.000 | 7.800 | 0,98 | 12,5 | 20–28 | 85–100 |
| C6 Moto popular | 110–140 | Chain RWD | 18.000 | 2.400 | 1,10 | 4,2 | 6–10 | 110–130 |
| C6b Moto esportiva | 160–200 | Chain RWD | 26.000 | 3.400 | 1,30 | 4,6 | 3,5–5 | 220–260 |
| C7 Bicicleta | 14–18 | Pedal (jogador) | 9.000 | 1.400 | 1,00 | 1,8 | n/a | 35–45 |
| C8 Táxi/VaiJá | 1.100–1.400 | FWD | 30.000 | 4.200 | 1,05 | 5,5 | 11–14 | 165–185 |
| C9 Viatura PM | 1.500–1.900 | AWD | 42.000 | 5.800 | 1,20 | 5,8 | 7–9 | 200–230 |

### 2.7 Diferenças de Comportamento entre Tipos

#### 2.7.1 Carro (C1, C2, C3, C8, C9)
- Quatro rodas com suspensão independente (McPherson simplificada).
- Direção progressiva (ângulo volante × velocidade).
- Capota em colisões laterais a 80+ km/h ou capotamento direto.
- Pode **capotar** em curva se CoM alto + inclinação >45°.

#### 2.7.2 Moto (C6)
- Modelo de **2 rodas + lean dinâmico**. CoM muda com pilotagem.
- **Corredor:** entre carros parados/engarrafados a até 35 km/h sem colisão (assist opcional).
- Capota se: freada brusca com direção virada, impacto lateral, ou velocidade <8 km/h sem pé apoiado (jogador sai automaticamente).
- Empinada (wheelie): segurar acelerador + leve toque de freio dianteiro → mantém empinada por 2–4 s. Bônus de Sanidade (+3) se mantida 5 s.
- Morte por moto é mais branda: dano cai 20% comparado a capotamento de carro (cartoon).

#### 2.7.3 Caminhão (C4) e Carreta (C4b)
- Articulação: tractor (unidade motriz) + reboque (1–2 eixos articulados via `ArticulationBody`).
- **Jackknife:** se frear tractor forte em curva com reboque leve, reboque "dobra" e empina lateral — dano + perda de controle.
- Tempo de mudança de marcha (manual flappy-paddle auto): 0,8 s em caminhão vs 0,25 s em carro.
- Não entra em corredor nem estaciona em vaga pequena.
- Carrega carga (missão de Seu Otacílio); peso da carga altera dinâmica.

#### 2.7.4 Ônibus (C5)
- Alto, longo, pesado. Rolo em curva. Freio longo (até 30 m a 60 km/h).
- Pode carregar pedestres (modo transporte público alternativo, ligado ao Tonho da Van).
- Manobra difícil em cruzamentos apertados do Centro Histórico.

#### 2.7.5 Bicicleta (C7)
- Sem motor. Jogador pedala (toque rítmico no botão acelerar = "pedalada").
- Velocidade máxima 35 km/h plano; 20 km/h subida; 50 km/h descida.
- Não tem dano deformação — apenas quedas.
- Sanidade +1/min enquanto pedala (muito saudável, **conquista "Ciclofaixa"**).
- Sem combustível, sem oficina (só reparo manual ou troca).

---

## 3. Sistema de Dano

### 3.1 Filosofia

Dano é **visual + funcional**. Cada impacto deforma a lataria (mesh vertex) **e** degrada performance. Não há "vida única" — o carro acumula estados e fica visivelmente destruído antes de explodir. Tom cartoon: sem gore, mas fumaça, faísca, capô voando e "amassões" exagerados garantem humor e feedback.

### 3.2 Estados de Dano (0–100%)

| Estado | Faixa | Aparência | Performance | Dirigibilidade |
|---|---|---|---|---|
| **Impecável** | 0–5% | Brilho de loja. | 100% | 100% |
| **Amassado leve** | 6–25% | Arranhões, amassões menores, vidro trincado. | 95% | Leve perda de aderência em curva. |
| **Amassado médio** | 26–50% | Capô torto, faróis quebrados, fumaça leve do capô. | 80% | Direção puxa; motor perde 15% potência. |
| **Crítico** | 51–80% | Porta solta, parabrisa estilhaçado, fumaça preta. | 55% | Pneu arriado aleatório; freio perde 30%. |
| **Incêndio** | 81–95% | Fogo no motor, faíscas, óleo no chão. | 30% | Vel. máx cai 60%; risco de explosão. |
| **Explosão** | 96–100% | Explosão cartoon (sem gore); respawn sem veículo. | 0% | Jogador ejetado (dano 30 HP). |

### 3.3 Subsistemas Atingidos

Cada veículo tem **4 subsistemas independentes** com seu próprio HP (0–100):

| Subsistema | Símbolo | Efeito ao ser danificado | Reparo |
|---|---|---|---|
| **Motor** | 🔧 | Perda de potência (até −60%), fumaça, eventual incêndio. | Oficina (Dr. Éverton) |
| **Pneus (4×)** | ⚫ | Aderência cai; pneu arriado = arrasto forte. | Oficina ou kit reparo (consumível) |
| **Direção** | 🎯 | Volante puxa para um lado; raio de curva errático. | Oficina |
| **Lataria/Estrutura** | 🛡️ | Deformação visual; protege outros subsistemas (absorve dano primeiro). | Oficina (chaparia) |

**Vidros, portas, retrovisores** são elementos cosméticos com dano separado: quebram com impacto lateral, mas não afetam performance. Porta solta = efeito cômico de abre-fecha ao dirigir.

### 3.4 Regras de Propagação de Dano

1. Impacto frontal (ex.: capotagem em mureta a 80 km/h): **+25% Lataria**, **+15% Motor**.
2. Impacto lateral (ex.: T-bone em cruzamento): **+20% Lataria**, **+10% Direção**, possível porta solta.
3. Capotamento (roda 360°): **+35% Lataria**, **+20% Direção**.
4. Queda de altura >4 m: **+30% Lataria**, **+15% Suspensão**.
5. Impacto com pedestre/NPC leve: **+3% Lataria** (cartoon — NPC "voa" e levanta depois).
6. Colisão em alta velocidade (>100 km/h) com veículo maior: dano dobrado.

### 3.5 Incêndio e Explosão

- **Incêndio (81%+):** veículo começa a pegar fogo. Cronômetro de **45 s** até explosão (a menos que jogador saia ou reparo seja aplicado).
- Se jogador **permanecer dentro** em incêndio: perde 1 HP/s; Sanidade −1/s (pânico).
- **Explosão:** cartoon, sem gore. Veículo destruído (não recuperável naquela sessão; respawn no seguro, se pago).
- Explosão causa dano em área (**raio 4 m**, 25 HP) — pode ferir NPCs próximos, gerar estrelas de procurado e reação policial.

### 3.6 Reparo Manual (campo)

- **Kit Reparo Básico** (R$ 80): recupera 25% Lataria + 10% Motor + ignora incêndio por 60 s.
- **Kit Reparo Premium** (R$ 220): recupera 50% em todos subsistemas + apaga fogo.
- **Extintor** (R$ 45): apaga incêndio apenas; não repara.
- Aplicar kit exige **parar o veículo** e clicar por 4 s (mini-qTE no mobile).

---

## 4. Combustível

### 4.1 Visão Geral

Todo veículo motorizado tem **tanque** + **consumo** variável por estilo de direção. Bicicleta não usa combustível. Ficar sem combustível é **gameplay real**: o jogador precisa parar, empurrar, roubar outro veículo ou chamar reboque (com custo).

### 4.2 Mecânica de Tanque

- Cada veículo tem **capacidade de tanque (L)** (ver Tabela 7.1).
- Medidor em HUD (semelhante a real, em %): 100% → 0%.
- Aos **15%**: ícone pisca + sinal sonoro de alerta + cheiro de "/reserva".
- Aos **0%:** motor corta; veículo para; jogador ejetado automaticamente; modo "Empurrar" ativado.

### 4.3 Consumo por Estilo de Direção

Consumo base (km/L) é modificado por estilo:

| Estilo | Condição | Multiplicador |
|---|---|---|
| **Econômico** | Acelerar progressivo, <80 km/h, sem freadas bruscas. | ×0,8 (economiza 20%) |
| **Normal** | Padrão misto. | ×1,0 |
| **Esportivo** | Acelerador >70%, mudanças bruscas, >120 km/h. | ×1,4 |
| **Racha** | Nitro, redline, derrapadas. | ×1,9 |
| **Ocioso (lento)** | <10 km/h por >30 s (engarrafamento). | ×2,0 (consome sem andar) |
| **Clima frio (madrugada)** | Liga antes de aquecer. | ×1,15 (15 min após ligar) |

### 4.4 Tabela de Consumo (referência rápida)

| Veículo | Tanque (L) | Consumo base (km/L) | Autonomia (km) | Abastecimento cheio (R$)* |
|---|---|---|---|---|
| Fusca-style | 45 | 11 | 495 | 215 |
| Uno com escada | 45 | 13 | 585 | 215 |
| Chevette rebaixado | 52 | 10 | 520 | 248 |
| Gol-style popular | 50 | 12 | 600 | 238 |
| Esportivo "Vibora" | 60 | 7 | 420 | 286 |
| Hilux-style | 80 | 8 | 640 | 381 |
| Caminhão Scania | 320 | 3,2 | 1.024 | 1.524 |
| Ônibus urbano | 230 | 3,5 | 805 | 1.096 |
| CG 160 (moto) | 16 | 38 | 608 | 76 |
| Moto esportiva 600cc | 18 | 20 | 360 | 86 |
| Táxi | 55 | 11 | 605 | 262 |
| Viatura PM | 65 | 9 | 585 | 310 |

> *Preço do litro de gasolina: **R$ 4,76** (base IPC-Caos, semana 0). Etanol: R$ 3,45. Flex escolhe automaticamente o mais barato. Sujeito a IPC-Caos semanal.

### 4.5 Postos de Gasolina

**Distribuição:** 14 postos espalhados pela cidade (2 por bairro, exceto Sítio do Capim e Vista Alegre, com 1 cada).

**Marcas ficcionais:**

| Marca | Cor | Bairro predominante | Preço |
|---|---|---|---|
| **PetroCaos** | vermelho/branco | Monte Verde, Centro | ×1,00 |
| **BrasaOil** | verde/laranja | Belvedere, Itaúna | ×1,05 (premium) |
| **Chinapetro** | amarelo/azul | Vista Alegre, Sítio do Capim | ×0,92 (barato, qualidade duvidosa: risco de dano leve) |
| **ShellCaos** | amarelo/vermelho |rodovias,praia | ×1,02 |

**Interação no posto:**

1. Jogador para ao lado da bomba.
2. UI mostra: tipo de combustível, preço/L, quantidade a abastecer, total.
3. Escolher "Tanque Cheio" ou valor parcial.
4. Paga (R$ ou CaosCash).
5. Espera 5–15 s (animação) — pode ser pulado com anúncio opt-in.

**Eventos de posto:**

- **Promoção relâmpago** (5% chance ao entrar): combustível −15% por 2 min de jogo (sinal luminoso).
- **Fila no posto** (IPC-Caos alto >110%): NPCs buzinam, esperiência demora mais (filas de 1 min).
- **Assalto em andamento:** (1% chance): bandidos assaltam o caixa — jogador pode intervir, ignorar ou roubar o butim (reputação + facção Milícia Escudo se ajudar o caixa).

### 4.6 Sem Combustível

Quando tanque atinge 0%:

1. Motor morre; veículo para.
2. Aparece **modal com 3 opções:**
   - **Empurrar** (caminhar empurrando o veículo até próximo posto/Oficina do Dr. Éverton). Velocidade de caminhada −40%. Cansaço (+Energia −1,2/min).
   - **Roubar outro veículo** (estourar vidro, hotwire 3 s). Risco de estrelas de procurado (1–2).
   - **Chamar Reboque (guincho)**: R$ 250 + transporte até Oficina ou posto. Demora 60–90 s para chegar.

---

## 5. Oficina do Dr. Éverton

> **NPC canônico:** Dr. Éverton (ver [03-personagens.md](03-personagens.md)). Localização: **Polo Monte Verde**, galpão grande com neon azul e placa "Mecânica do Doutor — Conserto e Turbinagem 24h".

### 5.1 Serviços Oferecidos

| Serviço | O que faz | Preço base (R$) |
|---|---|---|
| **Reparo total** | Dano → 0% em todos subsistemas. | ver tabela 5.2 |
| **Reparo parcial** | Repara só subsistema escolhido. | 60% do total |
| **Pintura** | Troca cor primária/secundária. | R$ 350 + cor premium R$ 800 |
| **Adesivos/Vinil** | Aplica gráficos no veículo. | R$ 120–500 |
| **Chaparia** | Só lataria (deformação visual). | R$ 200–600 |
| **Troca de óleo** | Reduz consumo em 5% por 7 dias. | R$ 90 |
| **Revisão completa** | Tudo acima + diagnóstico. | R$ 1.200 |

### 5.2 Tabela de Preço de Reparo por % de Dano

> Preço = `Base_Classe × (% Dano / 100) × k_IPC_Caos`.

| Classe | Base reparo cheio (R$) |
|---|---|
| C1 Popular | 800 |
| C2 Esportivo | 2.800 |
| C3 Pickup | 1.500 |
| C4 Caminhão | 4.500 |
| C4b Carreta | 7.200 |
| C5 Ônibus | 3.800 |
| C6 Moto | 450 |
| C7 Bike | 0 (só troca peça: R$ 80) |
| C8 Táxi/VaiJá | 950 |
| C9 Viatura | n/a (proibido; só IA) |

**Exemplo:** Uno com escada (C1) com 60% de dano = `800 × 0,60 × 1,00 = R$ 480`.

### 5.3 Upgrades de Performance

Cada upgrade tem **3 níveis**. Aplicar nível 2 custa (nível 1 + custo nível 2).

| Upgrade | Nível | Efeito | Custo (R$) |
|---|---|---|---|
| **Motor Turbinado** | I | +12% potência | 3.500 |
| | II | +25% potência | 7.800 (+I) |
| | III | +40% potência, ‒5% consumo | 15.000 (+II) |
| **Freio Esportivo** | I | ‒15% distância frenagem | 1.800 |
| | II | ‒28% distância | 4.200 |
| | III | ‒40% distância + ABS reforçado | 8.500 |
| **Pneu Esportivo** | I | +10% aderência (drift mais fácil) | 1.500 |
| | II | +20% aderência | 3.600 |
| | III | +30% + resistência aquaplanagem | 7.200 |
| **Blindagem** | I | ‒30% dano por colisão | 5.000 |
| | II | ‒50% dano + vidro blindado | 12.000 |
| | III | ‒70% dano + anti-explosão (1× por missão) | 24.000 |
| **Nitro (N2O)** | I | 1 carga de nitro (3 s, +30% vel.) | 4.500 |
| | II | 3 cargas + recarga 30 s | 9.500 |
| | III | Cargas ilimitadas (cooldown 20 s) | 22.000 |
| **Som Automotivo** | I | Reproduz rádio custom; +1 Sanidade/min | 800 |
| | II | Subwoofer; +2 Sanidade; medidor de "parei?" | 2.400 |
| | III | Parede de som; funk liberado; +3 Sanidade; **evento "parede de som"** atraiNPCs | 6.000 |
| **Suspensão a Ar** | I | Levanta/rebaixa chassi on-demand | 3.000 |
| | II | + estabilidade em curva | 6.000 |
| | III | + "dança" (modo carnaval) | 10.000 |
| **Escape Esportivo** | I | Som de motor alterado; +5% potência | 1.200 |
| | II | +10% potência; remove limitador vel. | 2.800 |
| | III | +15%; visual de tube chrome | 5.500 |

### 5.4 Customização Visual

| Item | Variedade | Preço (R$) |
|---|---|---|
| Cor primária | 40 cores | 350 (premium: 800) |
| Cor secundária (teto) | 20 cores | 500 |
| Aro liga leve | 15 estilos | 800–2.500 |
| Adesivo/Vinil | 30 estilos | 120–500 |
| Neon subchassi | 8 cores | 1.800 |
| Faróis LED/RGB | 6 tipos | 1.200–3.000 |
| Capeação de banco | 8 estilos (couro, tecido, recuado) | 600–1.800 |
| Volante esportivo | 5 estilos | 400–900 |
| Calota vintage | 4 estilos | 250 |

### 5.5 Veículo Salvo (Garagem)

- Jogador pode ter **até 6 veículos** (initial slot: 2; compra de slot extra: 2.500 R$ ou 50 CaosCash).
- Veículos salvos aparecem na **garagem do jogador** (quitinete inicial: 1 vaga; casa: 2–4; mansão: 6).
- Veículo destruído (explosão) só retorna se jogador tiver **Seguro Caos** (R$ 1.500 mensal ou plano anual 16.000). Sem seguro: perda total.

---

## 6. Tipos de Veículos — Comportamento por Classe

> Esta seção descreve como cada classe **se comporta ao jogar** no mobile. Para specs exatas, ver Tabela 7.1.

### 6.1 Carros Populares (C1)

**Característica:** fracos, baratos, abundantes. É o que o jogador dirige nas primeiras 3–5 horas.
**Sensação touch:** direção leve, freio macio, acelerador preguiçoso. Understeer em curva fechada.
**Como dirigir no mobile:** usar Modo A (botões). Freio automático recomendado ON. Ideal para aprender.
**Onde encontra:** toda a cidade, especialmente Centro Histórico e Vista Alegre (estacionados).
**Exemplos canônicos:** Fusca-style, Uno com escada, Chevette rebaixado, Gol quadrado.

### 6.2 Esportivos (C2)

**Característica:** rápidos, caros, raros. Visivelmente atraentes.
**Sensação touch:** acelerador responsivo, freio firme, direção direta. Tendência a oversteer em RWD.
**Como dirigir:** Modo B (inclinação) + assists OFF recomendado. Cuidado com aquaplanagem em chuva. Nitro crucial em racha.
**Onde encontra:** Jardim Belvedere (estacionados em condomínios, concessionárias).
**Exemplos canônicos:** Vibora (Viper-style), Furioso (GT-style), Dragão (esportivo chinês topo).

### 6.3 Caminhonetes / Pickups (C3)

**Característica:** potentes, altas, confortáveis em terra. Bom para Sítio do Capim.
**Sensação touch:** dirigem como carro grande; traseira leve sem carga; aderência em terra boa (4×4).
**Como dirigir:** Modo A ou B. Boas para missões de entrega e off-road. Cuidado em curva a alta velocidade.
**Onde encontra:** Monte Verde, Sítio do Capim, rodovias.
**Exemplos canônicos:** Hilux-style, D-20 do Capim, Saveiro cross.

### 6.4 Caminhões (C4) e Carretas (C4b)

**Característica:** pesados, lentos, articulados (carreta). Dinâmica de **jackknife**.
**Sensação touch:** acelerador preguiçoso, freio longuíssimo, raio de virada grande.
**Como dirigir:** Modo A (botões) obrigatório. Câmera FAR. Planejar curvas com antecedência. **Não tentar racha.**
**Onde encontra:** Polo Monte Verde (galpões), rodovia perimetral.
**Exemplos canônicos:** Scania de carreta, caminhão baú, caminhão pipa, caminhão de cerveja (missão Seu Otacílio).

### 6.5 Ônibus (C5)

**Característica:** gigantes, altos, cheios de gente (modo transporte).
**Sensação touch:** como caminhão mas mais alto; rolagem em curva intensa; freio longo.
**Como dirigir:** Modo A. Câmera FAR. Manobras em cruzamentos apertados do Centro exigem atenção.
**Onde encontra:** pontos de ônibus em todos os bairros; terminal Centro ↔ Itaúna.
**Exemplos canônicos:** Micro-ônibus "8123", ônibus urbano, van alternativo (Tonho da Van).

### 6.6 Motos (C6)

**Característica:** ágeis, rápidas, fracas em colisão. Fazem **corredor**.
**Sensação touch:** direção por inclinação (lean). Sensação de velocidade alta. Empinada = minigame.
**Como dirigir:** Modo B (inclinação) ideal. Use corredor em engarrafamento. Cuidado em chuva.
**Onde encontra:** Vista Alegre (CG), Praia de Itaúna (esportivas), toda a cidade.
**Exemplos canônicos:** CG 160 (popular), Factor (esportiva entrada), CBRzinha (esportiva), custom "Brazilian Hog" (estilo Harley).

### 6.7 Bicicletas (C7)

**Característica:** lentas, sustentáveis, saudáveis. Não gastam combustível.
**Sensação touch:** pedalada rítmica (toque no acelerador em cadência).
**Como dirigir:** Modo A. Bom para explorer, missões leves, ciclovias de Itaúna.
**Onde encontra:** ciclovia de Itaúna, prédios residenciais, aluguel por app (R$ 5/hora).
**Exemplos canônicos:** Aro 26 (mountain bike), BMX (manobras), Speed (rodovia), Dobrável (urbana).

### 6.8 Táxis (C8a)

**Característica:** populares pretos/amarelos. Podem fazer **missão de táxi** (ganhar R$).
**Sensação touch:** como carro popular; direção um pouco mais firme.
**Como dirigir:** Modo A. Jogador pode roubar táxi e fingir ser taxista (mini-game de gerenciar passageiro).
**Onde encontra:** ponto de táxi no Centro, Belvedere, aeroporto (rodoviária).

### 6.9 VaiJá — App de Transporte (C8b)

**Característica:** carros particulares com adesivo. Modo **"motorista de app"** — jogador recebe corridas via smartphone, ganha R$ por viagem.
**Sensação touch:** depende do veículo (popular ou esportivo).
**Como dirigir:** Modo A. **Mini-game de avaliação:** dirigir bem (sem freadas bruscas, sem bater) = +5 estrelas = +gorjeta.
**Onde encontra:** trigger pelo app no celular do jogador, em qualquer lugar.
**Referência:** sátira a Uber/99; passageiros bizarros (ver eventos em [06-eventos-aleatorios.md](06-eventos-aleatorios.md)).

### 6.10 Viaturas Policiais (C9)

**Característica:** potentes, equipadas (rádio, sirene), tentam te parar em perseguição.
**Sensação touch:** como esportivo + cabine mais alta. Adicionalmente, sirene ativa câmera cinematográfica.
**Como dirigir:** geralmente dirigido pela IA da PM; jogador só dirige se roubar uma (estrelas de procurado sobem rápido).
**Onde encontra:** delegacias (Centro, Belvedere), patrulhando em qualquer bairro.
**Exemplos canônicos:** Viatura da PM (estilo Fusion/Cruze), Rover blindado, moto da BM (batalhão de motos).

---

## 7. Tabela de Veículos (catálogo de São Genésio)

> 25 veículos ficcionais brasileiros. Preços em R$ soft currency, sujeitos a IPC-Caos. Dirigibilidade: 1 (difícil) – 5 (acessível). Spawn = onde o veículo é comumente encontrado/parked no mundo.

| # | Nome | Classe | Massa (kg) | Pot. (cv) | 0–100 (s) | Consumo (km/L) | Tanque (L) | Preço (R$) | Dirig. | Onde Encontra/Spawna |
|---|---|---|---|---|---|---|---|---|---|---|
| V01 | **Fusca-style "Besouro"** | C1 Popular | 850 | 36 | 22,0 | 11 | 45 | 12.000 | 4 | Centro Histórico, Vista Alegre, Sítio do Capim |
| V02 | **Uno com Escada** (Mille) | C1 Popular | 920 | 58 | 14,5 | 13 | 45 | 16.500 | 5 | Toda a cidade; icônico na Vista Alegre |
| V03 | **Chevette Rebaixado** | C1 Popular | 980 | 72 | 12,0 | 10 | 52 | 19.800 | 3 | Vista Alegre (racha), Centro |
| V04 | **Gol Quadrado "Bola"** | C1 Popular | 1.020 | 70 | 12,5 | 12 | 50 | 18.000 | 5 | Centro Histórico, Belvedere (velho) |
| V05 | **Opala Diplomata** | C1 Popular | 1.350 | 121 | 10,5 | 7 | 70 | 35.000 | 3 | Belvedere, Centro (clássico) |
| V06 | **Vibora GT** (esportivo Viper-style) | C2 Esportivo | 1.450 | 410 | 4,2 | 6 | 60 | 280.000 | 2 | Jardim Belvedere (concessionária) |
| V07 | **Furioso RS** (GT-style) | C2 Esportivo | 1.380 | 320 | 4,8 | 7 | 55 | 210.000 | 3 | Belvedere, Praia de Itaúna |
| V08 | **Dragão Vermelho** (cherokee esportivo) | C2 Esportivo | 1.500 | 380 | 4,5 | 6 | 65 | 245.000 | 2 | Belvedere |
| V09 | **Hilux Marrom** ("Latão") | C3 Pickup | 2.100 | 204 | 10,5 | 8 | 80 | 165.000 | 4 | Sítio do Capim, Monte Verde |
| V10 | **Saveiro Cross** | C3 Pickup | 1.350 | 120 | 11,0 | 11 | 55 | 75.000 | 4 | Centro, Sítio do Capim |
| V11 | **D-20 do Capim** | C3 Pickup | 1.850 | 150 | 12,5 | 7 | 90 | 95.000 | 3 | Sítio do Capim |
| V12 | **Scania de Carreta** ("Toco") | C4b Carreta | 24.000 | 410 | 30,0 | 3,2 | 320 | 320.000 | 1 | Polo Monte Verde, rodovia perimetral |
| V13 | **Caminhão Baú "Brecha"** | C4 Caminhão | 9.500 | 230 | 22,0 | 4 | 200 | 145.000 | 2 | Monte Verde |
| V14 | **Caminhão Pipa** | C4 Caminhão | 12.000 | 280 | 28,0 | 3 | 280 | 180.000 | 2 | Monte Verde, Vista Alegre (enchente) |
| V15 | **Caminhão da Cerveja** | C4 Caminhão | 11.000 | 260 | 25,0 | 3,5 | 250 | 165.000 | 2 | Toda a cidade (missão Seu Otacílio) |
| V16 | **Ônibus Urbano 8123** | C5 Ônibus | 13.500 | 220 | 24,0 | 3,5 | 230 | 95.000 | 2 | Toda a cidade (paradas de ônibus) |
| V17 | **Micro-ônibus "Sardinha"** | C5 Ônibus | 8.500 | 160 | 19,0 | 5,5 | 150 | 68.000 | 3 | Centro Histórico, Vista Alegre |
| V18 | **Van Alternativa do Tonho** | C5 Ônibus | 6.500 | 130 | 16,0 | 7 | 110 | 45.000 | 4 | Vista Alegre, Centro (NPC canônico) |
| V19 | **CG 160 "Correria"** | C6 Moto | 118 | 15 | 8,5 | 38 | 16 | 8.500 | 4 | Vista Alegre, Centro (motoboy) |
| V20 | **Factor 250** (esportiva entrada) | C6 Moto | 165 | 22 | 6,5 | 25 | 18 | 22.000 | 3 | Toda a cidade |
| V21 | **CBRzinha 600** (esportiva) | C6b Moto Esp. | 185 | 110 | 3,8 | 20 | 18 | 38.000 | 2 | Praia de Itaúna, Belvedere |
| V22 | **Brazilian Hog** (custom estilo Harley) | C6 Moto | 290 | 65 | 6,0 | 18 | 19 | 58.000 | 2 | Motoclube Cavaleiros (Tavinho), Itaúna |
| V23 | **Bike Aro 26 "Capim"** | C7 Bike | 16 | n/a | n/a | ∞ | n/a | 850 | 4 | Sítio do Capim, Ciclovia Itaúna |
| V24 | **BMX "Manobra"** | C7 Bike | 14 | n/a | n/a | ∞ | n/a | 1.200 | 3 | Praia de Itaúna, Vista Alegre (becos) |
| V25 | **Táxi Amarelo "Bandana"** | C8a Táxi | 1.180 | 95 | 12,0 | 11 | 55 | 22.000 | 5 | Centro Histórico (ponto), aeroporto |
| V26 | **VaiJá Prius-style "Híbrido"** | C8b VaiJá | 1.300 | 120 | 10,0 | 18 (híbrido) | 40 | 38.000 | 5 | Disponível só via app (alugado por corrida) |
| V27 | **Viatura PM "Perseguição"** | C9 Policial | 1.700 | 245 | 7,2 | 9 | 65 | n/a (roubar) | 3 | Delegacias (Centro, Belvedere); patrulha |
| V28 | **Rover Blindado da PM** | C9 Policial | 3.200 | 310 | 8,5 | 5 | 120 | n/a (roubar; 4 estrelas) | 1 | Delegacia Central (em eventos 4+ estrelas) |

> **Convenção de nomenclatura:** nomes com `-style` indicam inspiração direta sem copyright. "VaiJá" é o app fictício do jogo (sátira Uber/99).

### 7.1 Veículos Exclusivos por Facção / Evento

| Veículo | Dono / Facção | Como obter |
|---|---|---|
| **Caminhão de Cerveja personalizado** | Seu Otacílio (Facção 1) | Recompensa missão caminhoneiro, reputação +60 |
| **Brazilian Hog "Caveira"** | Tavinho (Facção 3) | Recompensa racha championship, reputação +70 |
| **Rover Blindado Preto** | Milícia Escudo (Facção 2) | Recompensa missão extorsão, reputação +50 |
| **Furioso "Vereadora" (branco-perolizado)** | Helena Velasco (Facção 4) | Recompensa missão política, reputação +60 |
| **Sandão de Praia** (buggy) | Quiosque de Itaúna | Compra em Quiosque Surf Shop, R$ 28.000 |

---

## 8. Tráfego Inteligente

### 8.1 Filosofia

Trânsito não é decoração: é um **sistema vivo** que reage ao jogador, à hora do dia, ao clima, à economia e às facções. Engarrafamento real em horário de pico; corredor de motos dinâmico; motoristas buzinam, xingam e fogem.

### 8.2 Spawn de NPCs Motoristas

**Pool de tráfego:** até **24 veículos** ativos simultâneos (mobile), com **object pooling** (DOTS-eligible) para manter 60 fps em intermediários.

**Spawn dinâmico por bairro:**

| Bairro | Carros comuns | Motos | Caminhões | Ônibus/Vans | Esportivos |
|---|---|---|---|---|---|
| Vista Alegre | 40% | 35% | 2% | 8% | 0% |
| Centro Histórico | 50% | 20% | 5% | 15% | 2% |
| Polo Monte Verde | 30% | 10% | 35% | 5% | 0% |
| Sítio do Capim | 25% (caminhonete) | 8% | 15% | 5% | 0% |
| Jardim Belvedere | 45% | 8% | 2% | 5% | 25% |
| Praia de Itaúna | 30% | 30% | 1% | 10% | 8% |

### 8.3 Semáforos e Faixas

- **Semáforos sincronizados** por cruzamento principal (sistema de "ondas verdes").
- Vermelho dura 18–35 s (aleatório), verde 22–40 s.
- Atravessar no vermelho = **risco de multa** (se Guarda Municipal próxima) e **risco de colisão** (NPCs não freiam tão rápido).
- Faixas de pedestres: pedestres atravessam; atropelamento leve = sanidade −2, multa se flagrado.
- **Pedestres "atravessam fora da faixa"** em 8% das travessias (caos brasileiro).

### 8.4 Engarrafamento Dinâmico

| Horário (jogo) | Bairro | Densidade |
|---|---|---|
| 07h–09h | Centro, Belvedere, Monte Verde | **Pico** (90% via ocupada) |
| 12h–13h | Centro, praia (fim de semana) | Alto (70%) |
| 17h–19h | Toda a cidade | **Pico** (95%) |
| 22h–05h | Toda a cidade | Baixo (20%) |
| Eventos (jogo de futebol, show) | Entorno do evento | Extremo (100%) |

**Comportamento em engarrafamento:**

- NPCs reduzem velocidade, param, dão "fechadas".
- Aumento de **buzinas** em 400%.
- Motos fazem corredor (incl. jogador em moto).
- Jogador em carro pode: esperar, contornar, subir calçada (risco), roubar moto.

### 8.5 Reações dos Motoristas NPCs

| Evento | Reação do NPC motorista |
|---|---|
| Colisão leve | Buzina + xingamento (chat-bubble) + eventual fuga. |
| Jogador aponta arma | Fuga imediata (deixa veículo), corre a pé. |
| Estrelas de procurado (jogador) | 70% fogem (saem da via); 15% ficam paralisados; 15% tentam bloquear via. |
| Tiro perto | Encostam, deitam no banco (medo). |
| Explosão próxima | Fuga em pânico (atropelam outros), acidentes em cadeia. |
| Polícia atrás | Abram caminho (em tese); 20% não (caos). |
| Chuva forte | Reduzem velocidade 30%, alguns capotam. |
| Filho chora no banco traseiro | (Cosmético) vira a cabeça; jogabilidade: leve redução de atenção = freio brusco ocasional. |

**Sistema de xingamentos (chat-bubble):** banco de 80+ frases satíricas PT-BR (sem palavrões fortes, mantendo 14+):
- "Vê se aprende a dirigir, rapaz!"
- "Ô louco, tá doidão?!"
- "Mas queeitura, véi!"
- "Cê tem carteira?"
- "Tá voando, tchê!"

### 8.6 IA de Evitar Colisão (Avoidance)

Cada veículo NPC tem **3 camadas de IA:**

1. **Pathfinder** (URP navmesh veicular): traça rota entre waypoints do bairro.
2. **Raycast sensors** (5 raios frontais + 2 traseiros): detectam obstáculos em 12 m, reagem em 0,4 s.
3. **State Machine** (Drive, Brake, Honk, Flee, Crash).

**Comportamentos:**

- **Obstáculo à frente:** freia proporcional à distância.
- **Veículo lento atrás:** aguarda, faz sinal, ultrapassa se seguro.
- **Pedestre atravessando:** para; buzinada após 1,5 s.
- **Jogador em colisão iminente:** 70% freia; 30% tenta desviar (caos).

### 8.7 Densidade por Bairro e Horário (resumo)

| Fator | Efeito |
|---|---|
| Densidade veicular (geral) | 24 (mobile) / 40 (PC) |
| Hora de pico (pico) | ×1,7 densidade |
| Madrugada | ×0,4 |
| Chuva | ×0,8 (menos gente dirige) |
| IPC-Caos >130% | ×0,7 (gasolina cara; as pessoas pegam ônibus) |
| Evento "greve de ônibus" | ×1,4 (mais carros) |
| Nível de Caos alto (>70) | ×1,5 (direção caótica, acidentes em +60%) |

### 8.8 Corredor de Moto

- Motos (jogador e NPC) podem passar entre fileiras de carros parados/m lentos com **espaço lateral mínimo 1,2 m**.
- **NPCs motoboys** usam corredor automaticamente em engarrafamento (são ágeis no caos).
- Evento **"Corredor do Tavinho":** motoboys da facção 3 passam em bando — jogador pode seguir para missão Motoclube.

---

## 9. Corridas Urbanas Ilegais (Racha)

> **Facção canônica:** Motoclube Cavaleiros do Asfalto, líder **Tavinho** (ver [00-biblia-do-mundo.md](00-biblia-do-mundo.md) e [03-personagens.md](03-personagens.md)).

### 9.1 Como Funciona o Racha

**Definição:** corridas ilegais em circuitos fechados/temporários dentro da cidade. Marker (ponto de encontro) aparece no mapa após 21h (noite).

**Formatos:**

| Tipo | Descrição | Duração típica |
|---|---|---|
| **Racha Sprint** | 2 veículos, ponto A → B (2–4 km). | 1–3 min |
| **Racha Circuito** | 4–8 veículos, 2–3 voltas em circuito fechado. | 4–7 min |
| **Racha Toca-Rapido** | Speedrun em rodovia, pontos no GPS. | 3–5 min |
| **Racha Drift** | Pontos por derrapagem em circuito curto. | 2–4 min |
| **Campeonato Caos** | 5 etapas em uma semana; ranking. | Evento semanal |

### 9.2 Apostas

- Antes de cada racha, **aposta obrigatória** (entrada). Valores:
  - Racha amador: **R$ 500** de entrada (vencedor leva pool de 2–8 carros = R$ 1.000–4.000).
  - Racha intermediário: R$ 2.500.
  - Racha profissional (campeonato): R$ 10.000+.
- Apostas paralelas entre NPCs: jogador pode **apostar em outro piloto** (até 50% da entrada) — odd calculada por potência/ Dirigibilidade.

### 9.3 Rotas

Rotas pré-definidas (15 circuitos + 12 sprints), todas em São Genésio. Marcadas no GPS durante a corrida. Exemplos canônicos:

| Rota | Percurso | Risco |
|---|---|---|
| **Subida da Vista** | Becos de Vista Alegre → topo do morro | Capotagem, atropelamento, polícia |
| **Volta do Centro** | 3 voltas no Centro Histórico | Trânsito denso, pedestres |
| **Bordo do Mar** | Praia de Itaúna (areia dura) | Aquaplanagem se maré cheia |
| **Rodovia Perimetral** | Sprint 4 km na rodovia | Caminhões, fiscais |
| **Industrial Loop** | Polo Monte Verde, 3 voltas | Caminhões, fiscalização da Milícia |

### 9.4 Recompensas

| Colocação | Recompensa |
|---|---|
| 1º lugar | Pool + 100% + item raro (nitro +1, pneu esportivo) |
| 2º lugar | 60% do pool |
| 3º lugar | 30% do pool |
| 4º+ | Recupera entrada (10%), Sanidade −2 (frustração) |
| **Vitória perfeita** (sem dano) | +CaosCash 5, conquista "Asfalto Rei" |
| **Vitória por mais de 5 s** | Bônus "Humilhação": +50% pool |

### 9.5 Risco Policial

- Rachas têm **chance de 35%** de atrair PM (estrela 1-2) ao término.
- Durante o racha: viaturas podem aparecer em cruzamentos;NPCs buzinam; guardas municipais multam se parar.
- **Fuga pós-racha:** se polícia aparece, mini-perseguição. Fugir com sucesso = recompensa mantida; pego = perde tudo + R$ 1.500 multa + reputação −5 com PM.

### 9.6 Ligação com o Motoclube (Facção 3)

- **Tavinho** organiza rachas especiais às sextas (in-game), 22h, na Praia de Itaúna.
- Respeito com Motoclube (+5 reputação) por vitória em racha oficial.
- Reputação +70 desbloqueia: **"Brazilian Hog Caveira"** (V22 exclusivo).
- Reputação −50: Motoclube hostil, biker atacam jogador no trânsito.

### 9.7 Eventos de Racha (calendário)

| Evento | Frequência | Descrição |
|---|---|---|
| **Racha de Terça** | Toda terça, 21h | Amador, entrada R$ 500, circuito Centro. |
| **Quinta da Derrapagem** | Toda quinta, 23h | Drift no Polo Monte Verde. |
| **Sexta do Tavinho** | Toda sexta, 22h | Pro, entrada R$ 10.000, circuito Praia. |
| **Campeonato Caos** | Mensal | 5 etapas; prêmio R$ 80.000 + troféu. |
| **Racha Relâmpago** | Spawn aleatório (3% chance à noite) | Surpresa, sem marcação prévia; entrada baixa. |

### 9.8 Trapaças e Táticas

- **Nitro:** crucial em sprint; carregar nível 3 = vantagem enorme.
- **Cortar caminho:** atalhos por becos (Centro, Vista Alegre) — risco de capotagem.
- **Bloquear adversário:** empurrar lateralmente; se adversário capota, +15% pool.
- **Roubar:** (rádio interferência) instalar "pinga-pinga" no adversário — proibido, sujo; se descoberto, reputação −20 Motoclube.
- **Traição:** combinar com NPC e quebrar promessa — dá sanidade −5 mas pode render R$.

---

## 10. Apêndices Técnicos (Unity)

### 10.1 Componente `VehicleController` (esqueleto)

```csharp
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour {
    public WheelCollider[] wheels;          // 2 (moto) a 18 (carreta)
    public Transform[] wheelMeshes;         // visual
    public float motorTorque = 800f;
    public float brakeTorque = 3000f;
    public float maxSteerAngle = 30f;
    public float downforce = 200f;
    public FuelTank tank;
    public DamageState damage;
    public VehicleClass @class;             // C1–C9

    public void ApplyThrottle(float input);  // input -1..1
    public void ApplySteer(float input);
    public void ApplyHandbrake(bool on);
    public void ApplyNitro();
    // ...
}
```

### 10.2 Camadas de Colisão (Layer Collision Matrix)

| Layer | Colide com |
|---|---|
| `Vehicle` | Vehicle, Building, PedestrianSensor, Debris, Curb |
| `VehicleNPC` | Vehicle, Building, PedestrianSensor |
| `Pedestrian` | Building, Curb (sensor com Vehicle) |
| `Debris` | Vehicle (apenas) |

### 10.3 Performance Mobile

- **Object pooling** para tráfego (24 veículos + driver/pedestres por veículo).
- **LOD Wheel Collider:** abaixo de 30 m de distância, desativa raycast de detalhe.
- **Frame budget:** 16 ms/frame (60 fps). Física veicular = 4 ms; IA tráfego = 3 ms; render = 7 ms; outros = 2 ms.
- **DOTS (futuro):** Jobs System para IA avoidance de 40+ veículos em PC.

### 10.4 Áudio Veicular

| Som | Sistema |
|---|---|
| Motor | FMOD com parâmetros (RPM, carga, dano, nitro) |
| Freio | Triggered por slip |
| Derrapagem | Triggered por lateral slip > 0,4 |
| Colisão | Banco de 30 sons por intensidade |
| Buzina | Por veículo (5 estilos) |
| Nitro | Wssh + roar |

### 10.5 Conquistas Relacionadas (preview)

| ID | Conquista | Critério |
|---|---|---|
| ACV01 | **"Carteira Provisional"** | Comprar 1º veículo |
| ACV02 | **"Rei do Asfalto"** | Vencer 10 rachas |
| ACV03 | **"Mãe, tô no trânsito"** | Ficar 30 min em engarrafamento |
| ACV04 | **"Empinada Brutal"** | Empinar moto por 5 s |
| ACV05 | **"Gasolina na Flauta"** | Ficar sem combustível 10× |
| ACV06 | **"Drift Master"** | 500 m de drift contínuo |
| ACV07 | **"Fugitivo"** | Vencer racha e fugir da PM 5× |
| ACV08 | **"Ciclista Urbano"** | Andar 50 km de bike |

---

## 11. Encerramento

Este documento cobre **tudo o que o jogador toca, sente e ouve em um veículo** em **Cidade do Caos: Mundo Aberto**, da física de chassis à buzinada do tiozinho no engarrafamento. É a base técnica de design para o time de gameplay veicular. Eventos específicos envolvendo veículos (assalto a carro-forte, sequestro de van do Tonho, jacaré na pista da Itaúna) estão em [06-eventos-aleatorios.md](06-eventos-aleatorios.md). Missões de direção (M01–Mxx) em [07-missoes.md](07-missoes.md).

**Atributos e escalas** referenciados aqui (Energia/Sanidade 0–100, Reputação −100 a +100, IPC-Caos) seguem as definições canônicas em [00-biblia-do-mundo.md](00-biblia-do-mundo.md).

---

*Próximo documento:* [05-sistemas-jogo.md](05-sistemas-jogo.md) — economia, IPC-Caos, reputação, atributos e progressão geral.
