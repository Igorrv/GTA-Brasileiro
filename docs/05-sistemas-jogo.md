# 05 — Sistemas de Jogo

**Jogo:** *Cidade do Caos: Mundo Aberto*
**Cidade:** São Genésio
**Moeda:** Real (R$) — *soft*; CaosCash (CC$) — *premium*
**Documento:** GDD técnico de subsistemas — Sanidade, Economia, Reputação
**Versão:** 1.0 — 2026-07-28

> **Documentos relacionados**
>
> - ← [00-biblia-do-mundo.md](00-biblia-do-mundo.md) — Lore, geografia, facções e tom canônico.
> - ← [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md) — Modelo de direção, combustível, desgaste e multas de trânsito.
> - → [06-eventos-aleatorios.md](06-eventos-aleatorios.md) — Eventos caóticos, enchentes, greves e gatilhos de IPC-Caos.

---

## 0. Visão Geral de Sistemas

O *Cidade do Caos* opera sobre quatro atributos pessoais (0–100), dois eixos de reputação (−100 a +100) e uma economia inflacionária dinâmica. Os três subsistemas neste documento — **Sanidade**, **Economia** e **Reputação** — são cross-acoplados: sanidade baixa reduz ganhos econômicos; dívidas corroem a sanidade; reputação abre/fecha empregos e modifica preços; eventos aleatórios pulsionam os três simultaneamente.

### 0.1 Atributos base (referência rápida)

| Atributo | Faixa | Decaimento/min (base) | Notas |
|---|---|---|---|
| Fome (saciedade, 100 = cheio) | 0–100 | **−0,5** | Em 0: dano contínuo à Saúde. |
| Energia | 0–100 | **−0,4** (−1,2 correndo ou dirigindo) | Em 0: desmaio after 90 s. |
| Sanidade | 0–100 | Variável (eventos) | Faixas em §1.4. |
| Saúde | 0–100 | Indireto (Fome, Sanidade) | Em 0: *wasted* + multa médica. |

**Ciclo de dia:** 1 dia de jogo = 48 min reais (1 min real = 30 min de jogo). Loop diário recomputa `decaimento_dia`, contas vencidas e reajuste de IPC-Caos semanal.

---

## 1. Subsistema A — Sanidade Mental

A Sanidade é o termômetro da saúde psicológica do protagonista em São Genésio. Diferentemente de Fome/Energia (decaimento determinístico), a Sanidade responde a **eventos discretos** com magnitude variável e a um pequeno decaimento basal em situações crônicas de vulnerabilidade (Fome + Energia baixas, dívidas, exposição repetida ao caos).

### 1.1 O que AUMENTA Sanidade

| Ação / Evento | +Sanidade | Cooldown | Condição |
|---|---|---|---|
| Cafuné em gato de rua | +3 a +5 | 30 min de jogo | Aproximar e segurar interação 3 s |
| Churrasco com vizinho | +10 a +15 | 1 dia | Convidar/compartilhar carne; +Fome |
| Ir à praia (Itaúna) | +8 a +12 | 1 dia | Permanecer ≥ 4 min reais na orla |
| Música no carro | +0,3/min | — | Estação ligada; intensifica em direção noturna |
| Dormir (cama própria) | +15 a +25 | 1 sono | ≥ 6 h de jogo; restaura Energia |
| Dormir (rua/abrigo) | +5 a +8 | — | Energia parcial; risco de roubo |
| Rezar com Dona Cida | +12 a +18 | 3 dias | Visita à casa de Dona Cida em Vista Alegre |
| Vencer racha | +8 a +14 | — | Magnitude × aposta; bônus de adrenalina |
| Resolver missão facção | +5 a +10 | — | Recompensa psicológica |
| Visitar família (Cuti) | +20 | 7 dias | Evento de descanso obrigatório |
| Banho quente | +4 | 1 dia | Quitinete com boiler funcionando |
| Beber com amigos | +8 / −2 Saúde | 2 dias | Cap de +16 por dia |
| Tirar cochilo (10 min jogo) | +3 | 4 h | Energia +5 |
| Comprar presente para NPC amigo | +6 | — | Gatilho narrativo |

### 1.2 O que DIMINUI Sanidade

| Ação / Evento | −Sanidade | Recorrência |
|---|---|---|
| Estresse no trânsito (congestionamento ≥ nível 3) | −0,2/min | Acumula enquanto preso no trânsito |
| Receber multa de trânsito | −4 a −8 | Por evento |
| Fome < 20 e Energia < 20 simultâneos | −0,5/min | Estado crônico de vulnerabilidade |
| Ser preso | −25 a −35 | Evento único; + trauma |
| Presenciar tiroteio | −10 a −18 | Por evento; amortiza após 3ª exposição (−3) |
| Contrair dívida (aluguel atrasado) | −2/dia | Por dia de atraso |
| Vizinho mal-humorado (interação negativa) | −3 a −6 | Por interação |
| Evento caótico repetido (enchente, assalto, bleque-bleque) | −5 a −12 | Por evento; magnitude × frequência |
| Ver personagem conhecido morrer | −30 | Cinemática única |
| Falhar missão de facção | −8 a −12 | Por falha |
| Ficar ferido (Saúde < 25) | −0,3/min | Estado |
| Usar entorpecente (vício) | +15 curto / −8 longo | Rebound piora |
| Multa por dirigindo alcoolado | −12 | Compõe com "ser preso" se for o caso |
| Ser xingado por NPC (baixa reputação) | −1 a −2 | Por interação |

### 1.3 Eventos que afetam Sanidade (magnitude consolidada)

A fórmula geral para cada pulso de evento é:

```
ΔSanidade = Magnitude_base × Multiplicador_contexto × (1 − Tolerância_acumulada)
```

- **Magnitude_base**: ver tabelas §1.1/§1.2.
- **Multiplicador_contexto**: 1,0 padrão; 1,3 se Energia < 20; 1,5 se Fome < 20; 0,7 se já dormiu no dia; 1,2 à noite (22h–4h).
- **Tolerância_acumulada**: eventos idênticos repetidos sofrem atenuação de 0,15 por repetição dentro de 24 h, até o piso de 0,25 (75% de redução). Ex.: o 4º tiroteio do dia aplica ×0,55.

### 1.4 Faixas de Sanidade — efeitos

| Faixa | Rótulo | Efeitos |
|---|---|---|
| **0–15** | Crise ("ressaca") | Visão turva e levemente tremida (pós-processamento chromatic aberration + blur); NPCs reagem mal, xingam, recusam diálogo; **alucinações cômicas leves** (pombo fala, paredes respiram, postes curvam-se em slow-motion); diálogos geram **decisões automáticas ruins** (personagem escolhe opção agressiva/anti-social); +35 % de chance de erro em mini-games; Energia decai 1,5×; Saúde decai −0,1/min. |
| **16–30** | Esgotado | Visão levemente saturada; NPCs percebem cansaço; −10 % XP; +15 % chance de erro em mini-games; diálogos perdem opção "diplomática". |
| **31–50** | Abatido | Sem efeitos visuais; −5 % XP; sem bônus. |
| **51–70** | Estável | Estado neutro; sem penalidades nem bônus. |
| **71–84** | Bem-disposto | +5 % XP; pequena intuição (tag opcional em 30 % dos diálogos). |
| **85–100** | Iluminado | **Intuições em diálogos** (opção dourada disponível); +15 % XP global; **calma sob pressão** (cronômetro de mini-games +25 %); NPCs amigáveis aparecem com maior frequência; preço de remédios 10 % off (efeito placebo narrativo). |

### 1.5 Acoplamento com outros sistemas

- **Sanidade ↔ Economia:** estado de crise (≤15) reduz ganho de salário em 20 % e bloqueia trabalhos "frentista" e "garçom" (NPCs não contratam quem está visivelmente mal).
- **Sanidade ↔ Reputação:** decisões automáticas ruins (faixa 0–15) causam −2 de reputação por bairro a cada 24 h mantido nesse estado.
- **Sanidade ↔ Veículos:** ao dirigir com Sanidade ≤15, há 8 % de chance por minuto de micro-alucinação causando desvio de rota; ver [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md).

---

## 2. Subsistema B — Dinheiro / Economia

São Genésio tem uma economia dupla: **R$** (soft, ganho por trabalho e atividades) e **CC$** (premium, comprada com dinheiro real ou raramente premiada). R$ é a moeda de gameplay cotidiano; CC$ acelera, desbloqueia cosméticos e oferece proteção contra inflação.

**Salário inicial canônico:** R$ 150 por sessão de trabalho simples (motoboy nível 1).
**Referência de custo de vida:** lanche R$ 12; combustível cheio R$ 180; aluguel quitinete R$ 950/mês.

### 2.1 Trabalhos legais — tabela completa

| Trabalho | Como desbloqueia | Pagamento/sessão (R$) | Tempo/sessão | Requisitos | Risco |
|---|---|---|---|---|---|
| **Motoboy** | Tutorial inicial; alugar ou comprar moto | 80–150 + gorjetas | 12–18 min | Moto, CNH-A (ou informal) | Médio (trânsito); Fome −15 |
| **VaiJá (motorista de app)** | App no smartphone + carro próprio/alugado | 120–220 | 20 min | Carro, smartphone 4G | Baixo-médio (multas) |
| **Entregador (van/cesta)** | Falar com o comerciante | 90–160 | 15 min | Veículo qualquer | Baixo |
| **Pedreiro** | Balcão de vagas no Polo Monte Verde | 110–180 | 25 min | Energia ≥ 40 | Alto desgaste (Energia −35, Fome −25) |
| **Frentista** | Posto "Posto do Zé" | 70–110 | 18 min | Sanidade ≥ 40 | Baixo |
| **Garçom** | Bar/boate no Centro Histórico | 60–100 + gorjeta | 20 min (noturno) | Sanidade ≥ 50; Energia ≥ 50 | Médio (clientes bêbados) |
| **Caixa de mercado** | Mercado "SuperBom" | 65–95 | 20 min | Sanidade ≥ 30 | Baixo |
| **Pescador** | Cais de Itaúna | 70–200 (sazonal) | 30 min | Barco ou linha; Energia ≥ 50 | Médio (clima) |
| **Catador de recicláveis** | Sem requisito | 30–60 | 15 min | Nenhum | Baixo |
| **DJ de festa** | Missão secundária noturna | 150–400 | 30 min | Sanidade ≥ 60; famoso (rep ≥ 30 Vista Alegre) | Médio |
| **Táxi informal** | Carro + placa | 100–180 | 20 min | Carro, CNH-B | Médio |
| **Empregada doméstica (caso escolhida)** | Jardim Belvedere | 90–140 | 25 min | Sanidade ≥ 40 | Baixo |

**Ganho médio horário (R$/h de jogo real):** varia de ~R$ 120 (catador) a ~R$ 800 (DJ com fama).

**Energia经济学:** empregos físicos (pedreiro, pescador) trocam R$ por Energia e Fome — o jogador deve prever comida + sono.

### 2.2 Trabalhos ilegais leves — tabela completa

| Atividade | Como desbloqueia | Pagamento/sessão (R$) | Tempo | Risco | ΔReputação (facção/bairro) |
|---|---|---|---|---|---|
| **Contrabando (Tonho da Van)** | Missão "primeira entrega" no Sítio do Capim | 250–600 | 15 min | Médio (blitz); +estrela de procurado | +Caminhoneiros +5; Milícia −3 |
| **Missão de facção (qualquer)** | Relação ≥ 0 com líder | 300–900 | 20–30 min | Variável conforme facção | +facção escolhida +10 a +20; −facção rival −5 a −15 |
| **Racha com aposta** | Encontro noturno (Vista Alegre/Polo) | 200–1.500 (aposta) | 5–10 min | Alto (acidente, polícia) | +Cavaleiros +8 se vencer; bairro +3 |
| **Vender na rua (produtos salvos)** | Comprar lote barato e revender | 80–250 margem | 10 min | Médio ( Guarda Civil) | −Milícia −2; bairro do comércio −1 |
| **Transporte de encomenda "quente"** | Helena Velasco recruta | 400–1.000 | 25 min | Alto | +Frente Popular +12; Milícia −10 |
| **Furto de carga (em caminhão estacionado)** | Skill "aluizio" | 300–700 | 8 min | Muito Alto | −Caminhoneiros −15; Milícia +2 |
| **Apostador ilegal (casa de jogo)** | Aposta alta; sorte | −500 a +2.000 | 5 min | Médio | Neutro |
| **Caça-recompensa informal** | Bounty de NPC procurado | 200–800 | 15 min | Alto | Milícia ±10 conforme método |
| **Vigilância/segurança freelancer** | Skill combate | 150–400 | 20 min | Médio | +Milícia +5 |

> **Design note:** todo trabalho ilegal leve emite "calor" (estrelas de procurado acumuladas, 0–5). Calor ≥ 3 dispara perseguidores em qualquer bairro (ver [06-eventos-aleatorios.md](06-eventos-aleatorios.md)).

### 2.3 Gastos obrigatórios — tabela

| Categoria | Item | Custo base (R$) | Periodicidade | Notas |
|---|---|---|---|---|
| **Alimentação** | Lanche (pastel/suco) | 12 | Sob demanda | Fome +15 |
| | Marmita | 25 | Sob demanda | Fome +35 |
| | Compra mensal (básica) | 320 | Mensal | Suficiente p/ 1 semana |
| | Restaurante (Jardim Belvedere) | 80 | Sob demanda | Fome +50, Sanidade +5 |
| **Habitação** | Quitinete (Vista Alegre) | 950 | Mensal | Padrão inicial |
| | Casa (Sítio do Capim) | 1.800 | Mensal | +Energia ao dormir |
| | Apartamento (Centro) | 2.400 | Mensal | +Sanidade, fácil acesso |
| | Mansão (Jardim Belvedere) | 8.500 | Mensal | +status, garage 4 carros |
| **Transporte** | Combustível cheio (moto) | 90 | ~3 dias uso | Ver doc 04 |
| | Combustível cheio (carro) | 180 | ~3 dias uso | — |
| | Manutenção (óleo/pneu) | 200–600 | 1.500 km | — |
| **Multas** | Excesso de velocidade | 180–880 | Por evento | −Sanidade |
| | Estacionamento irregular | 120 | Por evento | — |
| | Dirigindo alcoolado | 2.500 + prisão | Por evento | −Sanidade grande |
| **Contas** | Luz | 180 | Mensal | Quitinete padrão |
| | Água | 95 | Mensal | — |
| | Internet/4G | 70 | Mensal | Necessária p/ VaiJá |
| **Saúde** | Remédio (Dipirona) | 8 | Sob demanda | Saúde +5 |
| | Remédio (ansiolítico) | 35 | Sob demanda | Sanidade +10 (cura crise) |
| | Atendimento PSF | 0 (gratuito) | Fila 20 min | Saúde +20 |
| | Clínica privada | 350 | Imediato | Saúde +50, Sanidade +5 |
| **Lazer** | Ingresso futebol | 60 | Semanal | Sanidade +12 |
| | Cerveja | 12 | Sob demanda | Sanidade +3 / Saúde −2 |

### 2.4 Economia inflacionária dinâmica — IPC-Caos

O **IPC-Caos** (Índice de Preços ao Consumidor — Caos) é recalculado **toda semana** (7 dias de jogo = 5 h 36 min reais) e reajusta todos os preços não-fixos do mundo.

#### 2.4.1 Modelo do índice

```
IPC-Caos_semana = Base_estrutural + Σ(Gatilho_i × Peso_i)
```

- **Base_estrutural:** +1,5 %/semana (saiu do Acordo de Metas do Banco Central de São Genésio).
- **Gatilhos** (somam ou subtraem):

| Gatilho | Tipo | Impacto típico |
|---|---|---|
| Enchente em Polo Monte Verde | Evento climático | +3,0 % |
| Greve dos caminhoneiros | Evento laboral | +2,5 % (escassez) |
| Tiroteio massivo (3+ facções) | Evento de caos | +1,8 % |
| Intervenção da Milícia em bairro | Política | +1,2 % |
| Eleições / promessa de aumento salarial | Política | +0,8 % |
| Boom turístico (verão em Itaúna) | Demanda | +0,7 % |
| Calmaria (semana sem eventos caóticos) | Estabilidade | −0,5 % |
| Subsídio da Frente Popular (reforço alimentar) | Política | −1,0 % |
| Quebra de safra no Sítio do Capim | Oferta | +1,5 % |

#### 2.4.2 Fórmula de reajuste

Para cada bem `i` cujo preço é flutuante:

```
preço_novo(i) = preço_base(i) × (1 + IPC-Caos_semana) × Multiplicador_bairro(b)
```

Onde `Multiplicador_bairro(b)` ajusta por contexto local (ex.: combustível 1,15× na Praia de Itaúna por demanda turística; comida 0,9× no Sítio do Capim por produção local). Itens de **aluguel contratado**, **financiamento** e **CC$** são fixados no momento da assinatura e não sofrem reajuste semanal.

#### 2.4.3 Como o jogador *hedgeia* (protege-se)

1. **Comprar a prazo** — fixar preço de combustível, aluguel antecipado ou alimentos em promoção antes do reajuste semanal.
2. **Investir em bens duráveis** — carros, motos, eletrodomésticos se valorizam ≥ IPC-Caos e podem ser revendidos.
3. **Estocar** — comprar marmitas congeladas, combustível em galão; cap de armazenamento = espaço da moradia.
4. **Converter R$ → CC$** — em semanas de IPC-Caos ≥ 4 %, a taxa de câmbio R$→CC$ sobe, então CC$ armazenada preserva poder de compra.
5. **Trabalhos premium** — missões de facção indexadas ao IPC-Caos (pagamento `= base × (1 + IPC-Caos acumulado)`).
6. **Diversificação geográfica** — comprar insumos no bairro mais barato da semana.

#### 2.4.4 Exemplo numérico — 4 semanas

Considere jogador iniciante com quitinete, marmita como referência de custo de vida.

| Semana | IPC-Caos (semana) | IPC-Caos acumulado | Marmita (preço_base R$ 25) | Combustível cheio carro (R$ 180) | Aluguel quitinete (R$ 950) |
|---|---|---|---|---|---|
| **S0** | — | 0,0 % | R$ 25,00 | R$ 180,00 | R$ 950,00 (fixo) |
| **S1** | Base 1,5 % + 1,0 % (pequeno evento) = **+2,5 %** | 2,5 % | R$ 25,63 | R$ 184,50 | R$ 950,00 |
| **S2** | Base 1,5 % + 3,0 % (enchente Polo) = **+4,5 %** | 7,1 % | R$ 26,78 | R$ 192,80 | R$ 950,00 |
| **S3** | Base 1,5 % − 1,0 % (subsídio Frente Popular) = **+0,5 %** | 7,7 % | R$ 26,91 | R$ 193,76 | R$ 950,00 |
| **S4** | Base 1,5 % + 2,5 % (greve caminhoneiros) = **+4,0 %** | 12,0 % | R$ 27,99 | R$ 201,51 | R$ 950,00 |

Após 4 semanas, **marmita subiu 12 %** e **combustível 12 %**, mas aluguel (fixado) não mudou. Jogador que converteu R$ 500 em CC$ na S0 manteve valor; jogador que estocou 10 marmitas em S0 "ganhou" R$ 30 em poder de compra. Jogador sem hedge perdeu ~12 % do salário real se o salário não foi reajustado.

> **Design note:** o salário mínimo simbólico é reajustado automaticamente a cada 4 semanas pelo IPC-Caos acumulado para evitar espiral de miséria. Salários premium (DJ, freelancer) sofrem reajuste manual pelo jogador via negociação narrativa.

---

## 3. Subsistema C — Reputação

A reputação do protagonista é dual: uma por **facção** (4 facções) e uma por **bairro** (6 bairros), ambas na faixa **−100 (ódio) a +100 (ídolo)**. As duas matrizes interagem — reincidir em furtos contra Caminhoneiros em Polo Monte Verde drena rep com a facção e com o bairro simultaneamente.

### 3.1 Matriz facção × bairro (matriz de ponderação de propagada)

Ações que afetam reputação propagam entre as duas matrizes com coeficientes:

| Ação em... | Peso na Rep_Facção | Peso na Rep_Bairro |
|---|---|---|
| …missão canônica de facção | 1,00 | 0,30 (bairro-base da facção) |
| …ação livre em bairro | 0,15 | 1,00 |
| …crime de rua visível | 0,25 | 1,00 |
| …evento caótico massivo | 0,50 | 0,75 |
| …decisão de diálogo | 0,40 | 0,10 |

**Bairros-base das facções** (referência cruzada com [00-biblia-do-mundo.md](00-biblia-do-mundo.md)):

- **Caminhoneiros do Caos (Seu Otacílio)** — Polo Monte Verde + estradas (Sítio do Capim).
- **Milícia Escudo (Coronel Bento)** — Vista Alegre (controle paralelo).
- **Motoclube Cavaleiros do Asfalto (Tavinho)** — Centro Histórico (sede) e estradas.
- **Frente Popular de São Genésio (Helena Velasco)** — Praia de Itaúna + Sítio do Capim (base social).

### 3.2 Ações e seus deltas de reputação

| Ação | Rep_Caminhoneiros | Rep_Milícia | Rep_Cavaleiros | Rep_Frente Popular | Rep_Bairro (local) |
|---|---|---|---|---|---|
| Ajudar caminhoneiro a trocar pneu | **+6** | 0 | +1 | +1 | +1 |
| Roubar carga de caminhão | **−18** | +2 | −2 | 0 | −4 |
| Pagar pedágio da Milícia sem reclamar | −1 | **+4** | −2 | −3 | +1 |
| Desafiar miliciano em cobrança | 0 | **−12** | +3 | +2 | −2 |
| Vencer racha patrocinado pelo Motoclube | +2 | −1 | **+10** | 0 | +3 |
| Perder racha propositalmente | 0 | 0 | **−4** | 0 | +1 |
| Doar comida para mutirão da Frente Popular | +1 | −2 | 0 | **+8** | +3 |
| Denunciar corrupção da Milícia | +1 | **−22** | +1 | **+6** | +2 |
| Entregar contrabando para Tonho da Van | **+8** | −4 | +2 | −2 | +1 (Sítio) |
| Salvar NPC civil em tiroteio | +3 | ±4 (depende do lado) | +2 | +3 | **+8** |
| Atropelar pedestre | −2 | −1 | −2 | −3 | **−10** |
| Pintar mural da Frente Popular | 0 | −3 | +1 | **+5** | +2 |
| Pichar muro da Milícia | +1 | **−15** | +2 | +1 | 0 |
| Vencer aposta grande no Motoclube | 0 | 0 | **+6** | 0 | +1 |
| Pagar cerveja para facção no bar | +1 | +1 | **+3** | +1 | +1 |
| Trabalhar como motoboy honesto | +2 | 0 | +1 | +1 | +2 |
| Servir de informant para Milícia | −3 | **+10** | −2 | −8 | −2 |
| Distribuir panfletos da Frente Popular | 0 | −4 | 0 | **+6** | +1 |

### 3.3 Benefícios e penalidades por faixa

#### 3.3.1 Por facção

| Faixa | Etiqueta | Consequência |
|---|---|---|
| **+50 a +100** | Aliado / Ídolo | Missões exclusivas; desconto 15 % em lojas da facção; **reforços em combate** (2 NPCs aparecem quando início de tiroteio com rival); chamada de reforço por rádio; presente semanal (item ou R$); prefixo de saudação ("Chefe", "Parceiro"). |
| **+20 a +49** | Amigo | Desconto 8 %; missões premium liberadas; saudação calorosa; rádio-amigo. |
| **+1 a +19** | Simpatizante | Acesso a trabalhos menores; diálogo amigável. |
| **0** | Neutro | Estado inicial; sem desconto. |
| **−1 a −19** | Suspeito | Sem desconto; vigilância leve; alguns NPCs evitam. |
| **−20 a −49** | Inimigo menor | Preços 10 % acima; spawn ocasional de inimigo (1 a cada 30 min no território); rádio bloqueado. |
| **−50 a −100** | Inimigo jurado / Alvo | **Spawn hostil** (2–4 NPCs atacam ao entrar no território); **recusa total de serviço**; contrato de eliminação pode ser aberto; NPCs da facção xingam à distância. |

#### 3.3.2 Por bairro

| Faixa | Etiqueta | Consequência |
|---|---|---|
| **+50 a +100** | Filho da comunidade | Desconto 20 % em todo comércio; NPCs oferecem presentes; casa de apoio; missões de líder local; **spawn de inimigos do bairro é zero** (a comunidade protege). |
| **+20 a +49** | Vizinho querido | Desconto 10 %; saudações constantes; missões secundárias. |
| **+1 a +19** | Conhecido | Sem desconto; diálogos cordiais. |
| **0** | Estranho | Estado neutro. |
| **−1 a −19** | Estranho indesejado | Preços 5 % acima; murmúrios. |
| **−20 a −49** | Encrenqueiro | Preços 15 % acima; alguns comércios recusam; polícialmais atento. |
| **−50 a −100** | Persona non grata | **Recusa de serviço**; chamadas de polícia ao aparecer; spawn de gangues hostis; teto de missão local bloqueado. |

### 3.4 Comportamento NPC por faixa de reputação

Tabela de reação aplicada a NPCs comuns e de facção ao detectar o protagonista:

| Faixa de Rep | Saudação | Medo | Presente / Vantagem | Spawn Hostil | Diálogo |
|---|---|---|---|---|---|
| **≥+85** | Efusiva ("Meu brother!", "Vai lá, mestre!") | 0 | Frequente (a cada visita) | 0 | Opção dourada sempre disponível |
| **+50 a +84** | Calorosa | 0 | Ocasional | 0 | Bônus de informação |
| **+20 a +49** | Simpática | 0 | Raro | 0 | Diálogo normal |
| **+1 a +19** | Neutra | 0 | Nunca | 0 | Padrão |
| **−1 a −19** | Fria / olha de lado | Baixo | Nunca | 0 | Opções reduzidas |
| **−20 a −49** | Hostil | Médio | Nunca | 1 NPC/30 min no território | Diálogo agressivo |
| **−50 a −100** | Xingamento | Alto | Nunca | 2–4 NPCs/15 min | Diálogo só via suborno ou missão de reparação |

### 3.5 Acoplamento com outros sistemas

- **Rep × Sanidade:** Rep ≤ −50 em qualquer bairro adiciona −0,2/min de Sanidade enquanto o protagonista estiver naquele bairro (perseguição social).
- **Rep × Economia:** Rep ≥ +50 em um bairro habilita trabalho freelancer local (ex.: taxista em Jardim Belvedere paga 25 % acima do normal).
- **Rep × Veículos:** Rep ≤ −20 com Cavaleiros do Asfalto faz mecânicos da facção cobrarem 30 % a mais; rep ≥ +20 libera upgrades com 10 % de desconto (ver [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md)).
- **Rep × Eventos:** regimes de Rep baixos em 3+ bairros disparam eventos "caçada" (ver [06-eventos-aleatorios.md](06-eventos-aleatorios.md)).

---

## 4. Cross-sistema — tabela consolidada de gatilhos

| Gatilho | Sanidade | Economia (R$) | Rep (facção) | Rep (bairro) |
|---|---|---|---|---|
| Dormir bem | +15 a +25 | 0 | 0 | 0 |
| Vencer racha | +8 a +14 | +200 a +1.500 | +6 a +10 Cavaleiros | +2 a +4 |
| Ser preso | −25 a −35 | −300 a −2.500 (fiança) | −3 a −10 (varia) | −2 |
| Enchente em Polo | −5 a −8 (testemunha) | R$ itens +3 % (IPC-Caos) | 0 | −1 |
| Greve caminhoneiros | −2 (escassez sentida) | combustível +12 % | 0 | 0 |
| Comprar marmita estocada | +1 | +12 % economizado | 0 | 0 |
| Doar para Frente Popular | +6 | −R$ 50 a 200 | +6 a +8 FP | +3 |
| Pintar mural | +5 | 0 | +5 FP / −3 Milícia | +2 |

---

## 5. Notas de balanceamento

- **Sink de Sanidade:** redundante com Economia (dívida) e combate, mas com piso de 0 e teto de 100; nunca negativo.
- **Sink de R$:** IPC-Caos + aluguel semanal indexado à categoria habitacional + decaimento de veículos formam o coração de *retention* mensal.
- **Sink de CC$:** cosméticos, acelerador de energia, protetor de sanidade (1 uso/semana). CC$ NÃO compra vantagem competitiva em PvP eventual (rachas) — apenas cosmético e QoL.
- **Pegada inicial recomendada (Tutorial → Semana 1):**
  - Salário VaiJá/motoboy: R$ 150 × 8 sessões = R$ 1.200
  - Custos mínimos: aluguel R$ 950 + lanche 5×R$ 12 + combustível R$ 90 = R$ 1.100
  - **Saldo:** R$ 100 → força o jogador a explorar trabalho ilegal leve (Tonho da Van) ou reduzir alimentação (punição em Fome).

---

> **Próximo documento:** [06-eventos-aleatorios.md](06-eventos-aleatorios.md) — Catálogo de eventos caóticos, enchentes, greves e gatilhos que pulsionam Sanidade, Economia (IPC-Caos) e Reputação simultaneamente.
