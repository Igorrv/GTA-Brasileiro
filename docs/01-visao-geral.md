# 01 — Visão Geral

> Fundação conceitual do jogo. Leia junto com a [Bíblia do Mundo](00-biblia-do-mundo.md).

## 1. Conceito (Elevator Pitch)

**Cidade do Caos: Mundo Aberto** é um sandbox urbano mobile ambientado numa metrópole brasileira fictícia — **São Genésio** — onde o jogador vive o cotidiano caótico, cômico e perigoso do Brasil: pegar ônibus lotado, ser motoboy, fugir da chuva que alaga a rua, fazer um extra transportando "pacote" duvidoso, brigar por vaga, participar de racha, lidar com milícia, político corrupto e vizinho churrasqueiro às 7h da manhã. Tudo com **humor brasileiro**, **caos crescente** e **liberdade total**.

É "GTA", mas com **saudade de casa**: identidade cultural brasileira em primeiro lugar, jogabilidade otimizada para toque (touch) e sessões curtas, e um tom que nunca leva o crime a sério demais (sem gore, sem trauma — caos estilo desenho/cartoon).

## 2. Pilares de Design

1. **Caos brasileiro é a mecânica.** O trânsito, o clima, as enchentes, as greves e os eventos absurdos não são cenário — são sistemas que mudam como você joga a cada sessão.
2. **Liberdade com consequência.** Você pode ser motoboy honesto ou contrabandista; cada escolha move reputação com **facções** e **bairros**, e a cidade reage.
3. **Sobrevivência leve, não chata.** Fome, energia e sanidade existem para dar ritmo e humor, nunca para punir com grind.
4. **Mobile-first de verdade.** Controles de toque, sessões de 5–15 min, progressão salvável a qualquer momento, performance 60 fps em intermediários.
5. **Humor e identidade.** Gírias, memes, funk, futebol, novela e churrasco são sistemas de conteúdo, não decoração.

## 3. Diferenciais (USP)

- **Brasil autêntico e atual:** nenhum jogo AAA mobile faz cidades brasileiras vivas com essa profundidade cultural (enchente, ônibus 8123, corredor de moto, político de promessa absurda).
- **Economia inflacionária dinâmica (IPC-Caos):** preços sobem semana a semana e com eventos — inflação é um problema real que o jogador precisa administrar.
- **Sistema de Sanidade com humor:** quando baixa, o mundo reage de forma cômica (pombo fala, NPCs xingam, visão tremida de "ressaca").
- **50+ eventos aleatórios roteirizados** com decisões e consequências, não apenas spawn de inimigos.
- **Direção realista e caótica:** física com peso, dano, combustível, oficina — e tráfego que realmente engarrafa.
- **Progressão de "vida"**: comprar quitinete → casa → mansão; bicicleta → moto → esportivo.

## 4. Tom e Conteúdo

| Dimensão | Decisão |
|---|---|
| Violência | Leve, cartoon, sem sangue/gore. Soco, empurrão, objetos improvisados. Mortes são "nocautes" (NPC volta depois). |
| Crime | Presente mas satirizado — assalto é mini-game cômico, não realista. |
| Humor | Brasileiro, autodepreciativo, com memes e gírias. Sempre com leveza. |
| Sexualidade/Drogas | Implícito/sugestivo, nunca explícito. |
| Política | Sátira; partidos e políticos são ficcionais (Frente Popular de São Genésio). |
| Sensibilidades | Evitar estereótipos cruéis; diversidade regional e de classe como força do mundo. |

## 5. Público-alvo

- **Primário:** 16–34 anos, jogadores casuais/core de mobile no Brasil, fãs de sandbox, simuladores de vida e humor nacional.
- **Secundário:** latino-americanos e jogadores de GTA mobile globais curiosos pelo setting.
- **Dispositivos:** Android (intermediário 4 GB RAM para cima) e iPhone (A12+). Baixa-spec com fallback gráfico.

## 6. Plataformas e Distribuição

- **Mobile:** Google Play e App Store. APK ~1,2 GB + download sob demanda (Addressables) por bairro.
- **PC (porto futuro):** Steam, controles completos.
- **Monetização:** Free-to-play com monetização ética (cosméticos, season pass, anúncios opcionais). Detalhe em [11-monetizacao.md](11-monetizacao.md).

## 7. Loop Central (Core Loop)

```
    EXPLORAR a cidade (a pé/veículo)
            │
            ▼
   TRABALHAR / MISSÃO  ──►  ganha R$ + reputação + XP
            │
            ▼
   SOBREVIVER (fome/energia/sanidade) + lidar com EVENTOS
            │
            ▼
   INVESTIR (veículo, casa, upgrades, roupas, facção)
            │
            ▼
   EVOLUIR status + abrir novo conteúdo (bairros, missões, veículos)
            │
            └──── (repete, com caos crescendo) ◄────
```

- **Sessão curta típica (5–15 min):** entra → faz 1 trabalho/missão → resolve 1–2 eventos → gasta/compra → sai (salvo automático).

## 8. Objetivos do Jogador

1. Explorar **São Genésio** livremente.
2. Completar missões para **dinheiro** e **reputação**.
3. Comprar **veículos, casas, roupas, melhorias**.
4. **Sobreviver** ao caos urbano (clima, trânsito, facções).
5. Interagir com **facções, polícia, vizinhos e moradores**.
6. Evoluir o personagem e **dominar a cidade**.

## 9. Métricas de Sucesso (KPIs de design)

| KPI | Meta |
|---|---|
| Retenção D1 / D7 / D30 | 45% / 20% / 8% |
| Sessão média | 8–12 min |
| Sessões por dia por usuário | 3–4 |
| Tempo para 1º veículo (onboarding) | ≤ 12 min |
| Conversão FTUE → missão secundária | ≥ 60% |

## 10. Riscos e Mitigações (alto nível)

| Risco | Mitigação |
|---|---|
| Escopo "AAA mobile" irreal | MVP enxuto bem definido ([13-mvp-roadmap.md](13-mvp-roadmap.md)); escalar conteúdo, não sistemas. |
| Performance em low-end | LOD agressivo, pooling, DOTS para tráfego ([12-tecnologia-implementacao.md](12-tecnologia-implementacao.md)). |
| Sensibilidade cultural | Consultoria/revision; satirear com afeto, não deboche. |
| Monetização invasiva | Só cosméticos/aceleradores; anúncios opt-in. |

---
*Próximo:* [02-narrativa-ambientacao.md](02-narrativa-ambientacao.md) • *Índice:* [README.md](README.md)
