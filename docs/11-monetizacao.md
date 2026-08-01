# 11 — Monetização

> Modelo **Free-to-Play ético** para **Cidade do Caos: Mundo Aberto**. Leituras: [01-visao-geral.md](01-visao-geral.md) (público/KPIs), [05-sistemas-jogo.md](05-sistemas-jogo.md) (economia/economia inflacionária/CC$ como sink).

## 11.1 Princípios de Monetização (não-negociáveis)

1. **Sem pay-to-win competitivo:** CaosCash (CC$) **não compra vantagem** em racha/PvP eventual — só cosmético e QoL.
2. **Tudo jogável de graça:** qualquer veículo/casa/missão é alcançável jogando (R$).
3. **Anúncios sempre opcionais** ("opt-in"), nunca forçados no meio da jogatina.
4. **Transparência de odds:** qualquer item com aleatoriedade mostra probabilidade (caixa/lootbox → regras das lojas).
5. **Limites de gastos:** controles parentais e limites de compra configuráveis (conformidade lojas + LGPD/COPPA).
6. **Respeito à economia:** CC$ não quebra o IPC-Caos (ver [05](05-sistemas-jogo.md)); cosméticos não inflacionam preço.

## 11.2 Moedas

| Moeda | Origem | Compra | Usos |
|---|---|---|---|
| **Real (R$)** | Jogabilidade (trabalhos, missões, eventos) | — | Veículos, casas, comida, gasolina, multas, upgrades funcionais |
| **CaosCash (CC$)** | Compra real + recompensas raras (passe, conquistas) | Pacotes na loja | Cosméticos, skins premium, season pass PRO, aceleradores QoL |

## 11.3 Itens à Venda (catálogo)

### 11.3.1 Cosméticos de personagem (R$ e CC$)
- Roupas, acessórios, penteados, tatuagens (muitos por R$, "premium" por CC$).
- Packs temáticos: Junina, Funk, Praia, Torcida, Politiqueiro.

### 11.3.2 Skins de veículos (CC$ em maioria)
- **Pinturas premium** (neon, chrome, "ferrugem vintage", bandeira do Brasil).
- **Adesivos/ wraps**, rodas exclusivas, escape cromado, **som de buzina personalizado** (hospício), **farol colorido**.
- Skins NÃO alteram desempenho (só visual).

### 11.3.3 Aceleradores / QoL (CC$) — *comunitários, sem vantagem competitiva*
| Item | Efeito | CC$ |
|---|---|---|
| Refil de Energia | Energia → 100 | 20 |
| Protetor de Sanidade (1/sem) | Anula 1 queda de Sanidade | 30 |
| Ganho de XP +50% (1h) | Boost de progressão | 25 |
| Reparo grátis (1 veículo) | Oficina sem custo | 40 |
| Slot extra de garagem | +1 vaga permanente | 80 |

> **Regra:** aceleradores só economizam tempo/ grinding — nunca desbloqueiam conteúdo exclusivo paywalled.

### 11.3.4 Veículos/Casas premium (CC$)
- **Veículos cosméticos premium** (ex.: "Fusca Nitro Dourado") — estatísticas equivalentes a um R$ similar, mas visual único.
- **Casas cosméticas premium** — mesma função, visual/diferente.

### 11.3.5 Pacotes de boas-vindas / ocasionais
- **Pacote Iniciante** (desconto 1× por conta): X CC$ + skin + acelerador.
- **Ofertas sazonais** (Carnaval, Junina, Réveillon, Black Friday Caótica).

## 11.4 Passe de Temporada ("Passe do Caos")

- **Free:** trilha gratuita com ~30 recompensas (R$, cosméticos básicos, 1 veículo por temporada).
- **PRO (CC$ ~90):** desbloqueia trilha premium com ~70 recompensas extras (skins premium, CC$ de volta, exclusivos).
- **Duração:** ~9 semanas por temporada, com tema cultural (S1: "Verão do Caos"; S2: "Junina Caótica"; etc.).
- **Progressão:** XP de temporada por missões/diárias/eventos; **nível a cada ~1h de jogo**.
- **Sem "comprar níveis" abusivo:** limite de níveis compráveis por semana (anti-FOMO).

## 11.5 Eventos Premium (live-ops)
- Eventos temporários com recompensas cosméticas exclusivas (ex.: "Caça aos Pombos", "Rei do Rachão", "Folia no Caos").
- Leaderboards de bairro (não competitivos tóxicos): só recompensa por meta pessoal.

## 11.6 Loja de Roupas (in-world)
- Lojas físicas nos bairros (Centro, Belvedere, Itaúna) vendem roupas por **R$** (jogabilidade) — algumas peças "premium" só por **CC$** na loja do app.

## 11.7 Sistema de Anúncios (NÃO intrusivos)

| Tipo | Quando | Recompensa (opt-in) |
|---|---|---|
| **Anúncio por recompensa** | Botão opcional perto de ações | +50% R$ de missão; refil Energia; continue após nocaute |
| **Anúncio de respawn** | Opcional ao "morrer" (nocaute) | Recuperar R$ perdido no hospital |
| **Banner discreto** | Só em menus (nunca no HUD de jogo) | — |
| **Sem anúncio forçado entre ações** | — | — |

- **Pacote "Sem anúncios"** (CC$ 150): remove banners e habilita o dobro das recompensas opt-in.
- Frequência limite (ex.: máx. 1 anúncio opt-in a cada 3 min) para não cansar.

## 11.8 Pacotes de CaosCash (exemplo de preço)

| Pacote | CC$ | Preço (referência) |
|---|---|---|
| Pitaco | 80 | R$ 4,90 |
| Carterinha | 200 | R$ 14,90 |
| Bônus Mensal | 560 + 20/dia | R$ 29,90 |
| Malote | 1.200 | R$ 59,90 |
| Dinheiro Sujo (melhor custo/benefício) | 2.600 | R$ 119,90 |

*(Preços ilustrativos; ajustar por região/moeda.)*

## 11.9 KPIs de Monetização (metas de design)

| KPI | Meta |
|---|---|
| ARPDAU (receita média/dia/usuario) | US$ 0,12–0,20 |
| Conversão pagante | 2–4% |
| Conversão Passe PRO | 8–12% dos ativos |
| LTV 90 dias | US$ 6–10 |
| % receita cosmética vs passe | 40% / 35% / 25% aceleradores+ads |

## 11.10 Conformidade e Ética
- **LGPD/ANPD (BR), GDPR, COPPA/Google Play Family:** consentimento, dados de menores, transparência.
- **Lootboxes:** mostrar odds; opção de compra direta do item desejado quando possível.
- **Gastos de menores:** controles parentais, senhas de compra, limites.
- **Anti-vício:** lembretes de pausa; nenhum design de "dark pattern" (FOMO agressivo, escassez falsa).
- **Acessibilidade econômica:** conteúdo core 100% jogável sem gastar; ranking sem pay-to-win.

## 11.11 Anti-Inflação (integração com economia)
- Compras de CC$ não injetam R$ no mundo (não infla IPC-Caos).
- Veículos/casas premium têm stats = equivalentes R$ → não desvalorizam progressão.
- Aceleradores respeitam teto de progressão por semana (anti-rush que esvaziaria conteúdo).

---
*Próximo:* [12-tecnologia-implementacao.md](12-tecnologia-implementacao.md) • *Índice:* [README.md](README.md)
