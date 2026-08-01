# 🏙️ Cidade do Caos: Mundo Aberto — Game Design Document (GDD)

> Sandbox urbano **mobile** ambientado numa metrópole brasileira fictícia — **São Genésio**, a "Cidade do Caos". Humor, caos crescente, direção, sobrevivência leve, economia inflacionária e liberdade total. Inspirado em GTA, com identidade 100% brasileira (SP + Rio + Recife + BH). Engine: **Unity 6 LTS**.

Este é o **Game Design Document completo**, em **Português (PT-BR)**, com profundidade máxima, pronto para uma equipe iniciar o desenvolvimento.

**Portal interativo:** abra [`index.html`](index.html) (ou `pwsh scripts/serve-docs.ps1`) · **Arquitetura:** [`architecture.md`](architecture.md)

---

## 📑 Índice de Documentos

> **Comece sempre pela Bíblia do Mundo** — é a fonte de verdade canônica (nomes, números, escalas). Em caso de conflito entre qualquer seção e a Bíblia, **a Bíblia prevalece**.

| # | Documento | Conteúdo |
|---|---|---|
| **00** | [Bíblia do Mundo](00-biblia-do-mundo.md) | ⭐ Fonte de verdade: cidade, moeda, atributos, bairros, facções, NPCs, glossário, convenções |
| **01** | [Visão Geral](01-visao-geral.md) | Conceito, pilares, USP, tom, público, plataformas, loop central, KPIs |
| **02** | [Narrativa & Ambientação](02-narrativa-ambientacao.md) | História em 3 atos + 5 finais, 4 facções, 6 bairros, clima, trânsito, cultura BR |
| **03** | [Personagens](03-personagens.md) | Protagonista customizável, 10 NPCs, 5 vizinhos, animais, autoridades |
| **04** | [Direção & Veículos](04-sistemas-direcao-veiculos.md) | Física, dano, combustível, oficina, tráfego, racha + **tabela de ~28 veículos** |
| **05** | [Sistemas de Jogo](05-sistemas-jogo.md) | Sanidade, Dinheiro/Economia inflacionária (IPC-Caos), Reputação (matrizes e fórmulas) |
| **06** | [Eventos Aleatórios](06-eventos-aleatorios.md) | **50 eventos** (E01–E50) com descrição, opções, consequências e impacto |
| **07** | [Missões](07-missoes.md) | Principais (3 atos), secundárias, de facção, diárias, geradas por IA (pipeline) |
| **08** | [Interface & Telas](08-interface-telas.md) | 14 telas com **wireframes ASCII**, UX mobile, acessibilidade, fluxos |
| **09** | [Arte & Estilo Visual](09-arte-estilo-visual.md) | Direção de arte, paletas (hex) por bairro/facção, personagens, cenários, animação, clima |
| **10** | [Mecânicas de Jogabilidade](10-mecanicas-jogabilidade.md) | Direção, combate leve, tempo, clima, Caos crescente, upgrades, gambiarras, furtividade, perseguição, facções |
| **11** | [Monetização](11-monetizacao.md) | F2P ético, cosméticos, skins, passe de temporada, anúncios opt-in, CaosCash |
| **12** | [Tecnologia & Implementação](12-tecnologia-implementacao.md) | Unity: arquitetura, dados, física, tráfego DOTS, IA (NPCs/polícia/facções), eventos, otimização mobile |
| **13** | [MVP & Roadmap](13-mvp-roadmap.md) | Escopo do MVP (vertical slice) + **roadmap de 3 meses** (12 semanas) |

---

## 🎯 Resumo Executivo

- **Gênero:** Sandbox urbano de mundo aberto (ação-aventura / vida sim), **mobile-first**.
- **Cidade:** São Genésio ("Cidade do Caos") — ~9 km², 6 bairros, 4 facções.
- **Protagonista:** Caio "Caique" Martins (customizável), recém-chegado, sobe na vida legal ou ilegalmente.
- **Diferenciais:** Caos crescente como mecânica, economia inflacionária (IPC-Caos), 50 eventos roteirizados com decisões, Sanidade com humor, direção caótica brasileira.
- **Tom:** Humor brasileiro, violência cartoon leve (sem gore, 14+), sátira social/política com afeto.
- **Moedas:** Real (R$, soft) e CaosCash (CC$, premium); **sem pay-to-win**.

## 🧩 Como usar este GDD

1. **Designers de conteúdo:** leiam [00](00-biblia-do-mundo.md) → [02](02-narrativa-ambientacao.md) → [03](03-personagens.md) → [06](06-eventos-aleatorios.md)/[07](07-missoes.md).
2. **Designers de sistemas:** [00](00-biblia-do-mundo.md) → [04](04-sistemas-direcao-veiculos.md)/[05](05-sistemas-jogo.md)/[10](10-mecanicas-jogabilidade.md).
3. **UX/UI:** [08](08-interface-telas.md) + [09](09-arte-estilo-visual.md).
4. **Engenharia/Unity:** [12](12-tecnologia-implementacao.md) → [13](13-mvp-roadmap.md) para priorização.
5. **Live-ops/Business:** [01](01-visao-geral.md) (KPIs) + [11](11-monetizacao.md) + [13](13-mvp-roadmap.md).

## 📌 Convenções de numeração

- **Eventos:** E01–E50 ([06](06-eventos-aleatorios.md))
- **Telas:** T01–T14 ([08](08-interface-telas.md))
- **Missões:** M (principais), S (secundárias), F (facção), D (diárias), G (IA) ([07](07-missoes.md))
- **Veículos:** V01–V28 ([04](04-sistemas-direcao-veiculos.md))

---

*Documento vivo — versão 1.0. Atualizações devem manter coerência com a [Bíblia do Mundo](00-biblia-do-mundo.md).*
