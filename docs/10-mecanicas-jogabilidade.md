# 10 — Mecânicas de Jogabilidade

> As mecânicas momento-a-momento do jogador em **São Genésio**. Leia junto com a [Bíblia do Mundo](00-biblia-do-mundo.md). Para física detalhada de veículos veja [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md); para sanidade/dinheiro/reputação veja [05-sistemas-jogo.md](05-sistemas-jogo.md); para eventos emergentes veja [06-eventos-aleatorios.md](06-eventos-aleatorios.md).

## 10.1 Visão Geral das Mecânicas

| Mecânica | Risco | Frequência | Seção |
|---|---|---|---|
| Direção de veículos | Médio | Constante | 10.2 |
| Combate leve | Médio | Ocasional | 10.3 |
| Sistema de tempo (dia/noite) | — | Constante | 10.4 |
| Sistema de clima | Variável | Constante | 10.5 |
| Sistema de Caos crescente | Variável | Constante | 10.6 |
| Upgrades (personagem/veículo/casa) | — | Progressão | 10.7 |
| Crafting urbano (Gambiarras) | — | Ocasional | 10.8 |
| Furtividade | Médio | Ocasional | 10.9 |
| Perseguição policial | Alto | Ocasional | 10.10 |
| Facções rivais | Alto | Contextual | 10.11 |

---

## 10.2 Direção de Veículos (jogabilidade)

> Modelo de física e tabela de veículos: [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md). Aqui focamos no **feel** e nas ações do jogador.

**Controles touch (dois esquemas, selecionáveis):**
- **Esquema A — Botões:** acelerador (direita), freio/freio de mão (esquerda), setas esquerda/direita ou joystick virtual, câmera (deslizar), botão de ação (buzina/farol/nitro).
- **Esquema B — Inclinação (gyro):** inclinar o aparelho para virar; botões para acelerar/frear. Recomendado para motos.

**Ações de direção:**
- **Entrar/sair veículo:** botão contextual perto do veículo (também permite roubar — "tomar" o carro, com risco de reação do dono/polícia).
- **Buzina:** assusta pedestres, provoca motoristas (pode iniciar briga de trânsito — evento E05), sinaliza em corte de trânsito.
- **Farol alto:** necessário à noite em bairros sem iluminação (Vista Alegre, Sítio do Capim); sem farol, multa da Guarda Municipal.
- **Nitro (upgrade):** boost curto, gasta combustível extra.
- **Freio de mão + curva:** derrapagem controlada (drift), sobe Sanidade em racha, desgasta pneus.
- **Ré/estacionar:** estacionar correto evita multa; estacionar em vaga deficiente = multa pesada + Caos.
- **Carona/furar bloqueio:** cortar caminho pelo corredor (motos), subir calçada (risco de atropelamento → estrelas de procurado).

**Sensação (game-feel):**
- Carros populares: direção mole, demora a frear, "canela" em ladeira.
- Esportivos: precisos, rápidos, frágeil no toque.
- Caminhão/ônibus: pesados, virada larga, mau de subida, bom para atropelar bloqueios (mas gera muito Caos).
- Moto: ágil, passa no corredor, mas derruba fácil e o jogador sai voando (perde Saúde).
- Bicicleta: grátis, não gasta combustível, exaure Energia, ignora engarrafamento — bom para inícios de jogo.

**Câmera:** traseira seguindo (carro/moto), com leve auto-aim ao mirar; botão de olhar para os lados; câmera cinematográfica em perseguição.

---

## 10.3 Combate Leve

> Filosofia: **cartoon, sem gore**. Nocautes, não mortes. NPCs "apagam" e reaparecem depois. Áreas críticas (cabeça) só geram estrelas ("estreludo").

### 10.3.1 Movimento e esquiva
- Andar / correr (gasta Energia) / agachar / rolar (esquiva, i-frames curtos).
- Trancar alvo (botão) em até 3 inimigos próximos.

### 10.3.2 Ataques
| Ataque | Input | Dano | Observação |
|---|---|---|---|
| Soco | Botão ataque | 8 | Combo de 3 (último empurra) |
| Chute/Pisão | Ataque + direção | 12 | Derruba |
| Empurrão | Segurar ataque | 0 (controle) | Empurra para longe/escada/água |
| Objeto improvisado | Pega no ambiente | 10–20 | Vassoura, garrafa, cadeira, cone, cabo de vassoura |
| Arremesso | Segurar objeto + soltar | 8–15 | Garrafa, lata, pedra |
| Correr e bater | Correr + ataque | 15 | Investida |

### 10.3.3 Objetos improvisados (ambientais)
- Qualquer objeto urbano leve pode virar arma: **garrafa, lata, cone, cadeira de bar, vassoura, cabo, jornal enrolado, chinelo (baixo dano, alto humor), saco de pastel**.
- Armas mais "sérias" (taco, cano) existem mas são raras e aumentam estrelas de procurado.

### 10.3.4 Saúde e nocaute
- Saúde 0–100. Ao chegar a 0: **nocaute** → respawn no Hospital São Genésio (custa R$ + perde parte do dinheiro carregado, como em GTA). Sem morte "real".
- NPCs nocauteados caem, som de "estrelas" girando na cabeça, ficam de pé depois de ~20s.

### 10.3.5 Escala de procurado por violência
| Ação | Estrelas |
|---|---|
| Soco em civil sem motivo | 0–1 (testemunhas) |
| Nocaitear policial | 2 |
| Bater em grupo/político | 2–3 |
| Usar arma "séria" em público | 3 |
| Atropelar intencionalmente | 3–4 |

---

## 10.4 Sistema de Tempo (Dia/Noite)

- **1 dia de jogo = 48 min reais** (24 min de dia, 24 min de noite).
- Ciclo: **Amanhecer → Manhã → Meio-dia → Tarde → Pôr do sol → Noite → Madrugada**.

| Faixa (relógio jgo) | Mundo | Jogabilidade |
|---|---|---|
| 06h–10h (manhã) | Rush, ônibus lotado, escolas | Trabalhos de entrega em alta; trânsito pesado |
| 10h–16h (dia) | Comércio aberto, calor | Melhor para missões urbanas, lojas |
| 16h–19h (rush vespertino) | Engarrafamento pior | Eventos de trânsito (+Caos) |
| 19h–22h (noite) | Festas, bares, funk | Vida noturna, rachas, eventos sociais |
| 22h–02h (madrugada) | Quase vazio, perigoso | Crimes leves, milícia ativa, neblina industrial |
| 02h–06h (madrugada funda) | Ruas vazias | Gambás, assaltos (E04), sono regenera |

**Sono:** dormir em casa (quitinete/casa/mansão) avança o relógio 6–8h e regenera Energia e Sanidade. Dormir na rua/veículo regenera menos.

---

## 10.5 Sistema de Clima

> Detalhe visual/arte em [09-arte-estilo-visual.md](09-arte-estilo-visual.md); impacto narrativo em [02-narrativa-ambientacao.md](02-narrativa-ambientacao.md).

**Estados climáticos:** Sol forte, Sol leve, Garoa, Chuva, Tempestade, Enchente, Neblina.

| Clima | Efeito principal |
|---|---|
| Sol forte | Energia cai mais rápido (calor); faróis não obrigatórios |
| Garoa/Chuva | Pista escorregadia (aquaplanagem), visibilidade −30%, pedestres correm |
| Tempestade | Visibilidade −60%, raios, risco de **Apagão** (E02), árvores caem |
| **Enchente** | Ruas alagam (Centro, Itaúna), carros boiam, rota bloqueada, +Caos grande |
| Neblina | Depth fog, visibilidade −50%, comum no Polo Monte Verde de madrugada |

**Transições suaves** (10–20 seg de jogo), com previsão no rádio e no app de mapa do celular do personagem. Clima afeta spawn de eventos (enchente só com chuva forte; neblina só madrugada/industrial).

---

## 10.6 Sistema de Caos Crescente (Nível de Caos)

> Medidor global **0–100** do estado de desordem de São Genésio. É o coração temático e mecânico do jogo.

**O que sobe o Caos:**
- Eventos aleatórios ativos (+2 a +10 cada, ver [06-eventos-aleatorios.md](06-eventos-aleatorios.md)).
- Ações do jogador: crime, racha, atropelamento, destruição, fuga policial, bloqueio de via.
- Greves, apagões, enchentes (gatilhos de mundo).

**O que reduz o Caos:**
- Resolver eventos a favor da ordem (ajudar, desviar, consertar).
- Tempo sem incidentes (decaimento lento −1/min).
- Pagamento de "contribuição" à Frente Popular (políticos) — corrupção baixa Caos temporariamente (satírico).

**Efeitos do Caos:**
| Faixa | Cidade |
|---|---|
| 0–20 | Tranquila: trânsito normal, poucos eventos |
| 21–50 | Agitada: +eventos, buzinas, pedestres estressados |
| 51–80 | Caótica: múltiplos eventos simultâneos, IA agressiva, trânsito travado, lojas fecham |
| 81–100 | Colapso: tiroteio leve generalizado, polícia em massa, evacuação — momento "peak Caos" |

**Caos e dificuldade:** Caos alto = mais recompensas (trabalhos pagam mais, mas tudo fica mais perigoso e caro). Cria o dilema central: lucrar com o caos ou restaurar a ordem.

---

## 10.7 Sistema de Upgrades

Três eixos de progressão material:

### 10.7.1 Personagem (habilidades, ver [03-personagens.md](03-personagens.md))
- Árvores: **Direção, Combate, Social, Sobrevivência, Crime** — XP por ação → pontos por nível.

### 10.7.2 Veículo (na oficina do Dr. Éverton, ver [04](04-sistemas-direcao-veiculos.md))
- Motor (aceleração/topo), Freio, Pneu (aderência), Suspensão, Blindagem (resistência a dano/estrelas), Nitro, Som (rádio/buzina premium), visual (pintura/adesivos/rodas).

### 10.7.3 Habitação
- **Quitinete (R$ 950/mês, Centro)** → **Casa (R$ 180.000, periferia/Belvedere médio)** → **Mansão (R$ 1.200.000, Jardim Belvedere)**.
- Cada nível: mais regeneração ao dormir, mais slots de guarda-veículos (garagem), cofre (guardar R$ seguro de assalto), decoração (cosmético).

**Cofre e proteção de dinheiro:** guardar R$ em casa reduz perda ao ser assaltado/nocauteado. Mansão tem cofre maior.

---

## 10.8 Sistema de Crafting Urbano ("Gambiarra")

> Improvisar ferramentas/itens com sucata urbana. Sátira ao "jeitinho brasileiro".

**Coleta de sucata:** lixo, ferro-velho (Polo Monte Verde), sobras de oficina, itens descartados em eventos (carro queimado, caminhão tombado). Materiais: **fita isolante, arame, cano, plástico, sucata eletrônica, pano, garrafa pet**.

**Receitas (exemplos):**

| Item | Materiais | Efeito |
|---|---|---|
| Reparador de pneu (garrafa + fita) | Garrafa pet + fita isolante | Repara pneu furado (1 uso) |
| Galão de combustível improvisado | Garrafa pet x5 + arame | Transporta combustível extra |
| Taco improvisado | Cabo + cano | Arma leve (+dano) |
| Kit de primeiros socorros (gambiarra) | Pano + garrafa + fita | Cura +20 Saúde |
| Botão de "pressione" (isca) | Sucata eletrônica | Distrai pedestres/NPCs |
| Sirene falsa | Sucata eletrônica + fita | Finge ser viatura (curto, gera estrelas se descoberto) |
| Capa de chuva (pet) | Garrafa pet x3 | Reduz perda de Energia na chuva |

**Estação de gambiarra:** bancada em casa (quitinete tem básica; casa/mansão têm avançada, mais receitas). Quanto melhor a bancada, mais receitas e menos falha.

**Falha:** gambiarras têm % de falha (explode, não funciona, gera Caos pequeno). Habilidade "Sobrevivência" reduz falha.

---

## 10.9 Sistema de Furtividade

** Quando usar:** roubo leve (pegar item sem ser visto), entrar em área restrita (galpão, casa nobre, comitê político), evitar polícia/milícia, seguir alvo (missão de Bira).

**Mecânicas:**
- **Modo furtivo:** agachar reduz ruído e visibilidade; indicador de "detecção" (olho que abre/fecha, como em muitos sandboxes).
- **Cobertura:** paredes, carros, arbustos, lixo — quebrar linha de visão zera detecção.
- **Ruído:** correr/quebrar objetos atrai; chinelo faz menos barulho que bota.
- **Distração:** arremessar objeto (lata/garrafa) desvia o olhar do NPC/guarda.
- **Cones de visão:** mostrados (opcional, ajuda de acessibilidade) para autoridades/guardas.

**Roubo leve (furtar):** aproximar furtivo de NPC/veículo → mini-game simples (toque rítmico) → sucesso pega item/R$; falha gera reação (grita, chama polícia, estrelas). Risco x recompensa.

---

## 10.10 Sistema de Perseguição Policial

> Estrelas de procurado **0–5**. Estilo GTA, adaptado ao Brasil.

**Gatilhos (sobe estrelas):** crime visível, atropelamento, fuga, bater em viatura, resistir à blitze, porte de item ilegal visível.

| Estrelas | Resposta |
|---|---|
| ⭐ | Guarda Municipal observa/pode multar |
| ⭐⭐ | PM patrulha procura a pé |
| ⭐⭐⭐ | Viaturas perseguem de carro |
| ⭐⭐⭐⭐ | Mais viaturas + bloqueio na via |
| ⭐⭐⭐⭐⭐ | Polícia Civil (Bira) coordena; cerco total; helicóptero (raro, satírico "não tem verba") |

**Como escapar (reduz estrelas):**
- Sair da linha de visão por X segundos (barra de "fuga").
- Pintar o carro na oficina (reseta descrição) — se não te viram sair.
- Entrar em casa/quitinete e esperar.
- Suborno: pagar policial/R$ (se reputação/Social alta) — satírico.
- Disfarce: trocar de roupa em loja (loja de roupas reseta "descrição" do suspeito).

**Consequências ao ser preso:**
- Respawn na delegacia, perde armas ilegais e parte do R$ carregado, pequena perda de reputação com facções do bem, Sanidade −5.

**Blitze (E06):** pontos fixos/eventuais; passar bêbado/sem cinto/com item ilegal = multa ou estrelas. Desviar custa tempo mas evita problema.

---

## 10.11 Sistema de Facções Rivais

> As 4 facções (ver [02](02-narrativa-ambientacao.md) e [05](05-sistemas-jogo.md)) disputam São Genésio. O jogador é o "fiel da balança".

**Território dinâmico:**
- Cada bairro tem **controle parcial** por facção (ex.: Vista Alegre disputada entre Josival/comunidade e Milícia Escudo; estradas entre Caminhoneiros e Motoclube; Centro entre Frente Popular e Milícia).
- Ações do jogador (ajudar/sabotar) mudam o **controle territorial** e a **reputação** cruzada.

**Consequências do território:**
- Bairro dominado por facção aliada: descontos, missões exclusivas, NPCs acolhedores, reforço em briga.
- Bairro dominado por facção inimiga: preços altos, span hostil, missões bloqueadas, risco de emboscada.

**Eventos de guerra de facções:**
- Quando duas facções estão em relação ≤ −60, surgem **escaramuças** (tiroteio leve, bloqueio de rua, perseguição de moto) — eventos vivos que o jogador pode ignorar, intermediar ou explorar.

**Equilíbrio e dilema:** ajudar muito uma facção aliena as rivais. O jogo premia tanto o "especialista" (uma facção dominante) quanto o "diplomata" (todas equilibradas). Cada caminho abre um final diferente (ver [02](02-narrativa-ambientacao.md)).

---

## 10.12 Resumo de Controles (Mobile)

| Contexto | Botões principais |
|---|---|
| A pé | Joystick mover; Correr; Pular; Atacar; Interagir; Furtivo; Mapa; Inventário; Celular |
| Dirigindo | Acelerar; Freio/Freio de mão; Virar (botões/gyro); Buzina; Farol; Nitro; Sair |
| Combate | Atacar; Esquiva; Pegar objeto; Trancar alvo; Arremessar |
| Furtivo | Agachar; Distrair (arremessar); Roubar (mini-game) |
| Mapa/Menu | Pinça zoom; Toque ícone (rota); Filtros (facção, loja, missão) |

---
*Próximo:* [11-monetizacao.md](11-monetizacao.md) • *Índice:* [README.md](README.md)
