# 13 — MVP e Roadmap

> Escopo do **MVP jogável** de **Cidade do Caos: Mundo Aberto** e roteiro dos primeiros **3 meses**. Definido para entregar diversão real (vertical slice) antes de escalar. Leituras: [01](01-visao-geral.md), [12](12-tecnologia-implementacao.md).

## 13.1 Princípios de Escopo do MVP

- **Jogável e divertível em sessões de 5–15 min** desde o dia 1.
- **Vertical slice:** 1 bairro completo + 1 bairro esqueleto, todos os loops centrais funcionando.
- **Escala conteúdo, não sistemas:** os sistemas do MVP são os mesmos do produto final (só com menos dados).
- **Meta de onboarding:** 1º veículo em ≤12 min (ver [08](08-interface-telas.md)).

## 13.2 O que ESTÁ no MVP

| Área | Conteúdo MVP |
|---|---|
| **Mundo** | **Centro Histórico** (completo) + **Comunidade Vista Alegre** (favela, parcial). ~3 km². |
| **Personagem** | Customização básica (gênero, 4 penteados, 6 tons de pele, 8 roupas). 1 árvore de habilidade (Direção). |
| **Veículos** | **6 veículos** (1 popular, 1 moto, 1 bicicleta, 1 caminhonete, 1 ônibus, 1 viatura). Física, dano, combustível, oficina básica. |
| **Direção** | Controles touch (esquema A botões), dia/noite, clima (sol/chuva). |
| **Tráfego** | DOTS básico: ~30 NPCs motoristas + pedestres nav-mesh. |
| **Economia** | 3 trabalhos (VaiJá, motoboy, pedreiro), gastos (comida/gasolina/aluguel), **IPC-Caos** simplificado. |
| **Atributos** | Fome, Energia, Sanidade, Saúde (com efeitos básicos de Sanidade baixa). |
| **Reputação** | 2 facções (Caminhoneiros, Motoclube) + reputação por bairro. |
| **Eventos** | **15 eventos** do catálogo E01–E50 (enchente, blitz, racha, churrasco 7h, etc.). |
| **Missões** | **Ato 1 completo** (M01–M07) + 5 secundárias + diárias + 3 templates IA. |
| **Combate** | Combate leve (soco/empurrão/objeto) + estrelas de procurado (0–3). |
| **IA** | Pedestres (FSM), Polícia (perseguição até ⭐3), facção leve. |
| **UI** | Telas T01–T08 + T11 HUD + T12 config básica. |
| **Save/Cloud** | Save local + cloud; onboarding FTUE. |
| **Monetização** | CaosCash, 1 pacote cosmético, anúncios opt-in (sem pay-to-win). |
| **Plataformas** | Android (mid-range) alpha; iOS beta interno. |

## 13.3 O que FICA DEPOIS (pós-MVP)

- Demais 4 bairros (Monte Verde, Sítio do Capim, Belvedere, Itaúna) — conteúdos adicionais por temporada.
- 4 facções completas (Milícia Escudo, Frente Popular) + guerras de facção.
- Atos 2 e 3 da história + 5 finais.
- Catálogo completo: 50 eventos, ~25 veículos, 10 NPCs, todas habilidades.
- Crafting/gambiarras avançado, furtividade completa, clima (tempestade/neblina/enchente total).
- Passe de temporada, leaderboards, PC port, dublagem completa, multi-idioma.

## 13.4 Roadmap de 3 Meses (12 semanas)

> Meta: **Alpha jogável fechado ao fim da Semana 12** (MVP definido em 13.2).

### Mês 1 — Fundação técnica (S1–S4)
| Semana | Foco | Entregável |
|---|---|---|
| **S1** | Setup Unity 6, repositório, CI, Assembly Definitions, protótipo de mundo vazio + personagem (cápsula) andando | "Bolacha" jogável: andar/câmera no Centro esqueleto |
| **S2** | Sistema de tempo (dia/noite 48min), clima básico (sol/chuva), save/save-cloud | Ciclo dia/noite + persistência |
| **S3** | 1º veículo (popular) com física (Rigidbody/Wheel), dano, combustível, controles touch | Dirigível jogável |
| **S4** | Oficina básica, posto, sistema de dinheiro (R$) + primeiro trabalho (VaiJá) | Loop econômico mínimo |

### Mês 2 — Mundo vivo e sistemas (S5–S8)
| Semana | Foco | Entregável |
|---|---|---|
| **S5** | Tráfego DOTS (~30 carros) + pedestres NavMesh no Centro | Cidade com vida |
| **S6** | IA policial (perseguição ⭐1–3), Guarda Municipal (multas), combate leve | Crime e consequência |
| **S7** | Atributos (Fome/Energia/Sanidade/Saúde) + efeitos; reputação (2 facções + bairros) | Sobrevivência + reputação |
| **S8** | 15 eventos (E01–E15) com sistema de spawn; **Ato 1** missões M01–M04 | Eventos + início narrativa |

### Mês 3 — Conteúdo, polish e alpha (S9–S12)
| Semana | Foco | Entregável |
|---|---|---|
| **S9** | Missões M05–M07 + 5 secundárias + diárias + 1º template IA; customização de personagem | Progressão completa do MVP |
| **S10** | UI final (T01–T12), HUD, onboarding FTUE (≤12min), acessibilidade | UX pronta |
| **S11** | Arte/áudio: assets finais do Centro + Vista parcial, rádio do carro, SFX; monetização (CC$/ads) | Polish vertical slice |
| **S12** | Otimização mobile (perf mid-range 60fps), telemetria, bug bash, **Alpha fechado jogável** | **ALPHA MVP** |

### Marcos e métricas do alpha
- **Alpha jogável** → test com ~200 jogadores (friends/family/closed beta).
- **KPIs do alpha:** onboarding ≥60% ao 1º veículo; sessão média 8–12 min; crashes <1%; FPS estável em mid-range.
- **Decisão de go/no-go** para produção ampliada (mais bairros, atos 2–3) baseada no alpha.

## 13.5 Pós-MVP (visão além de 3 meses)

| Janela | Conteúdo |
|---|---|
| **M4–M6** | Ato 2 + facções Milícia e Frente Popular; 4º e 5º bairros; catálogo de veículos completo |
| **M6–M9** | Ato 3 + 5 finais; 50 eventos; 6º bairro (orla); passe de temporada S1 |
| **M9–M12** | Live-ops, eventos premium, dublagem, PC port, expansão (nova cidade/área rural) |

## 13.6 Riscos de Escopo (MVP)
| Risco | Mitigação |
|---|---|
| Tentar fazer todos os bairros no MVP | Travar em 1+1; escalar conteúdo depois |
| Física/controle não divertido cedo | S3 prioriza "feel" antes de mais veículos |
| Performance low-end | S12 dedicada a otimização; fallback gráfico desde S1 |
| Conteúdo insuficiente para retenção | Eventos IA + diárias darião replayabilidade infinita mesmo no MVP |

---
*Fim do GDD.* Volte ao [índice README](README.md).
