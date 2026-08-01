# 03 — Personagens

> **Status:** CANÔNICO. Nomes, atributos, escalas e relações descritos aqui estão em conformidade com a [Bíblia do Mundo](00-biblia-do-mundo.md). Em caso de conflito, a Bíblia do Mundo prevalece.
>
> **Escopo:** Este documento detalha o protagonista, os 10 NPCs principais, os 5 tipos de vizinhos, a fauna urbana e as autoridades de **São Genésio ("Cidade do Caos")**. Para sistemas de direção e veículos, consulte [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md); para atributos vivos (Fome, Energia, Sanidade, Saúde, Reputação), veja [05-sistemas-jogo.md](05-sistemas-jogo.md).

---

## Sumário

1. [Protagonista — Caio "Caique" Martins](#1-protagonista--caio-caique-martins)
2. [Os 10 NPCs Principais](#2-os-10-npcs-principais)
3. [Os 5 Tipos de Vizinhos](#3-os-5-tipos-de-vizinhos)
4. [Animais Urbanos](#4-animais-urbanos)
5. [Autoridades](#5-autoridades)
6. [Convenções de Design de Personagens](#6-convenções-de-design-de-personagens)

---

## 1. Protagonista — Caio "Caique" Martins

O avatar do jogador em **Cidade do Caos: Mundo Aberto**. Totalmente customizável em identidade, aparência e progression, Caio "Caique" Martins é o veículo humano através do qual o jogador experimenta o caos cotidiano de São Genésio. O background canônico é apenas a **configuração padrão (default)** — o jogador pode reescrever nome, gênero, visual e história de chegada à cidade sem nunca romper a coerência narrativa (a cidade e os NPCs reagem ao avatar, não à identidade).

### 1.1 Identidade e Background (Default)

| Campo | Valor canônico |
|---|---|
| **Nome** | Caio Martins (apelido "Caique") — **100% editável** |
| **Idade** | 24 anos |
| **Gênero** | Editável (masculino / feminino / não-binário); pronomes se ajustam |
| **Origem** | Cidade natal não especificada ("interior"); voltou a São Genésio após perder o emprego |
| **Moradia inicial** | Quitinete alugada no Centro Histórico (Dona Cleide é a locatária) |
| **Patrimônio inicial** | R$ 150,00 no bolso; sem veículo; celular básico |
| **Atributos iniciais** | Fome 70 · Energia 70 · Sanidade 60 · Saúde 100 · Reputação 0 em tudo |
| **Motivação narrativa** | "Subir na vida" — caminho legal ou ilegal decidido pelo jogador |

### 1.2 Sistema de Customização

A customização ocorre em duas fases: **(a) na criação do avatar** (tela inicial, refundível com item cosmético "Carteira Nova") e **(b) em jogo** via barbearias, estúdios de tatuagem, lojas de roupa e cirurgiões (estética, por CaosCash).

#### 1.2.1 Aparência Física

| Categoria | Opções de base | Observações de design |
|---|---|---|
| **Tipo de corpo** | Magro / Atlético / Robusto / Pesado | Não altera atributos; afeta apenas animações e hitboxes (mantidas idênticas por equidade) |
| **Altura** | Baixo (1,60 m) · Médio (1,72 m) · Alto (1,85 m) | Cosmético; câmera compensa |
| **Tom de pele** | 8 opções (do claro ao escuro) | Sistema HDR-aware |
| **Rosto** | 12 modelos base com deslizadores (queixo, nariz, olhos, sobrancelhas, boca, orelhas) | Até 6 deslizadores por região facial |
| **Olhos** | 8 formatos × 12 cores | Cores não-naturais desbloqueadas via CaosCash |
| **Cabelo** | 24 cortes (curto, médio, longo, tranças, raspado, black power, moicano, undercut, dreads etc.) | Cor à escolha; cores fantasy exigem item premium |
| **Barba / Bigode (masculino)** | 10 estilos | Crescimento diário opcional (visitar barbearia para aparar) |
| **Maquiagem (qualquer gênero)** | 8 estilos (batom, delineador, sombra) | Editável em qualquer espelho |
| **Tatuagens** | 32 desenhos em 6 zonas (braços, costas, peito, pernas, pescoço, rosto) | Custam R$ em estúdio; algumas desbloqueadas por missão |
| **Cicatrizes / marcas** | 10 opções cosméticas | Certa missão tatuagem-only depois de "incidente" narrativo |
| **Voz** | 3 opções por gênero | Affected por nível de Sanidade (cf. §1.5) |

#### 1.2.2 Roupas e Acessórios

O guarda-roupa é segmentado em **6 slots**. Roupas não são apenas cosméticas: algumas peças carregam **bônus leves** (ex.: bota de couro +5% de dano em chute; abrigo impermeável reduz penalidade de chuva) e **afixos sociais** (uniforme de entregador abre missões; terno abre portas no Belvedere).

| Slot | Exemplos | Onde comprar |
|---|---|---|
| **Cabeça (chapéu/boné/óculos)** | Boné aba reta, chapéu de palha (Sítio do Capim), óculos escuros, máscara de coelho | Lojas do Centro, quiosques de praia, contrabando do Tonho |
| **Tronco superior** | Camiseta, regata, camisa, vestido, jaqueta de couro, uniforme de motoboy, colete do Motoclube | Brechó na Vista Alegre, Butique do Belvedere, loja de uniformes |
| **Tronco inferior** | Calça jeans, shorts, calça de moletom, bermuda, saia, macacão | Lojas de departamento do Centro |
| **Calçados** | Chinelo, tênis, bota, sandália, coturno | Sapataria, barraquinhas |
| **Mãos / braços** | Luvas, pulseiras, relógio (mostra hora real do jogo) | Ourives, feira livre |
| **Acessórios especiais** | Mochila (aumenta inventário), bandana (oculta identidade durante crime), colete à prova de balas estilizado | Loja de utilidades, ferro-velho |

**Sistema de Conjunto (Outfit Set):** usar 3+ peças de uma coleção temática (ex.: "Motoboy Profissional", "Herdeiro do Belvedere",Funkeiro da Vista") desbloqueia bônus de Reputação ao entrar em bairros compatíveis e penalidade em bairros rivais.

#### 1.2.3 Identidade Narrativa Editável

O jogador define três opções que moldam (cosmeticamente) falas de NPCs e missões secundárias:

- **Apelido / como quer ser chamado:** até 12 caracteres.
- **Origem declarada:** "Sou daqui" / "Vim do interior" / "Vim de fora" — afeta reação inicial de Tia Marlene e Zé Pequeno.
- **Sonho declarado:** "Ficar rico", "Ficar famoso", "Ter paz", "Vingar" — usado em diálogos espelhados pela Vereadora Helena e por Bia.

### 1.3 Atributos Base

Definidos em conformidade com a Bíblia do Mundo, §4. Todos os valores são **0–100**, exceto Reputação (**−100 a +100**).

| Atributo | Inicial | Decaimento base | Efeito em valor baixo (≤15) | Efeito em valor alto (≥85) |
|---|---|---|---|---|
| **Fome** (saciedade) | 70 | −0,5/min | Perda de HP, visão tremida | +20% regeneração de HP |
| **Energia** | 70 | −0,4/min (−1,2 correndo/dirigindo) | Movimento lento; desmaio em 0 | +15% velocidade de corrida |
| **Sanidade** | 60 | Estável (muda por eventos) | Alucinações cômicas, NPCs xingam | Bônus em diálogos, "intuições" |
| **Saúde (HP)** | 100 | Apenas por dano | Respawn no hospital (custa R$) | — |
| **Reputação (global)** | 0 | Eventos | — | — |
| **Reputação por facção** | 0 em todas | Eventos e missões | Facção inimiga ataca | Facção aliada descontos/reforços |
| **Reputação por bairro** | 0 em todos | Eventos e missões | Comércio recusa serviço | Comércio com desconto |

### 1.4 Sistema de Nível e XP

A progressão do protagonista é orientada por **XP (Experiência)** acumulável em **5 categorias específicas** (uma por árvore de habilidade) mais um **Nível Geral**. XP é ganho por:

| Fonte de XP | XP Geral | XP específico (qual árvore) |
|---|---|---|
| Missão principal concluída | +100 a +500 | Conforme categoria da missão |
| Missão secundária | +30 a +150 | Conforme categoria |
| Evento aleatório resolvido | +10 a +60 | Conforme categoria |
| Trabalho (entrega, van etc.) | +5 a +30 | Geralmente Direção ou Crime |
| Combate vencido | +5 a +25 | Combate |
| Diálogo bem-sucedido (negociação, intimidação) | +5 a +20 | Social |
| Sobreviver 1 dia in-game sem morrer | +20 | Sobrevivência |
| Realizar proeza (corrida, salto, exploração) | +5 a +50 | Conforme proeza |

#### Tabela de Níveis e XP (Nível Geral)

| Nível | XP acumulado | Recompensa | Bônus de Atributo |
|---|---|---|---|
| 1 | 0 | — | Ponto de habilidade inicial: 1 |
| 2 | 100 | Desbloqueia 1ª missão secundária | +1 ponto de habilidade |
| 3 | 250 | Slot de roupa extra | +1 ponto de habilidade |
| 4 | 450 | Desconto 5% em transportes públicos | +1 ponto de habilidade |
| 5 | 700 | Desbloqueia árvore Crime (linha 1) | +2 pontos de habilidade |
| 6–9 | 1.000 / 1.400 / 1.900 / 2.500 | Bônus em atributos escolhidos | +1 ponto cada |
| 10 | 3.200 | **Marco:** desbloqueio da 2ª árvore avançada | +3 pontos |
| 11–19 | Curva de 700 XP/nível | — | +1 ponto por nível |
| 20 | 13.500 | **Marco:** veículo de nível médio liberado | +5 pontos |
| 21–29 | Curva de 1.200 XP/nível | — | +1 ponto por nível |
| 30 | 28.000 | **Marco:** conteúdo endgame (facção máxima) | +5 pontos |
| 31–50 | Curva de 2.000 XP/nível | Cosméticos | +1 ponto por nível |

- **Cap suave:** Nível 50 (design alvo para 60–80h de jogo casual).
- **Reset de habilidades:** disponível por R$ 5.000 no estúdio de tatuagem ("reformulação de vida") ou 1 vez gratuita a cada temporada.

### 1.5 Árvores de Habilidades

Cinco árvores, cada uma com **5 níveis em 4 ramificações** (total: 20 nós por árvore). Cada nó custa 1 ponto de habilidade. Cada árvore tem um **nó mestre (capstone)** desbloqueado ao gastar 12 pontos na árvore.

#### 1.5.1 Tabela Consolidada de Habilidades

| Árvore | Ramificação | Nó (custo) | Efeito |
|---|---|---|---|
| **Direção** | Freio & Curva | Freio Preciso (1) | +10% resposta de frenagem |
| Direção | Freio & Curva | Drift Control (1) | Drift 30% mais estável |
| Direção | Freio & Curva | Tração na Chuva (2) | Reduz penalidade de chuva em 50% |
| Direção | Freio & Curva | Mestre do Corredor (3) | Motos passam entre carros 20% mais rápido |
| Direção | Performance | Economia (1) | −15% consumo de combustível |
| Direção | Performance | turbo Suave (2) | +10% aceleração |
| Direção | Performance | Conserto de Campo (2) | Veículo recupera 5% de dano por parar 10s |
| Direção | Performance | Piloto de Asfalto (3) | +15% velocidade máxima em esportivos |
| Direção | Tráfego & Fuga | Buzina Ameaçadora (1) | NPCs saem da frente 25% mais rápido |
| Direção | Tráfego & Fuga | Leitura de Fluxo (2) | Mini-mapa destaca brechas no trânsito |
| Direção | Tráfego & Fuga | Fuga Limpa (3) | Perde 1 estrela de procurado 30% mais rápido |
| Direção | Tráfego & Fuga | Camuflar Van (3) | Veículos de carga demoram mais a serem detectados |
| Direção | Mentoria (Otacílio) | Carona Segura (1) | +10% XP de Direção |
| Direção | Mentoria | Convoy (2) | Aliados caminhoneiros ajudam em perseguição |
| Direção | Mentoria | Estrada Real (3) | Sem penalidades em estradas de terra |
| Direção | Mentoria | **Capstone: Rei do Asfalto** (4) | **Veículo ignora 50% do dano por colisão** |
| **Combate** | Força | Soco Forte (1) | +15% dano desarmado |
| Combate | Força | Chute Potente (2) | Chute empurra inimigo 1m |
| Combate | Força | Agarrar (2) | Pode agarrar e arremessar inimigo leve |
| Combate | Força | Marretada (3) | Combos de 3 hits com qualquer objeto |
| Combate | Esquiva | Pisão (1) | Esquiva custa 20% menos Energia |
| Combate | Esquiva | Reflexo (2) | Janela de esquiva +200ms |
| Combate | Esquiva | Contra-ataque (3) | Após esquiva perfeita, golpe crítico |
| Combate | Esquiva | Pé de Chumbo (3) | Não cai de moto em colisão leve |
| Combate | Improviso | Tapa com Calçado (1) | Chinelo/soco inglês como arma improvisada |
| Combate | Improviso | Vassourada (2) | Vassoura/escoba empurra múltiplos inimigos |
| Combate | Improviso | Garrafa Cheia (2) | Garrafa causa dano e atordoa |
| Combate | Improviso | Estilingue (3) | Estilingue com pedra causa dano à distância |
| Combate | Defesa | Aparar (1) | Reduz dano de soco em 25% |
| Combate | Defesa | Bloqueio pesado (2) | Reduz dano por objeto em 30% |
| Combate | Defesa | Guarda-costas (3) | Aliado facção aparece em brigas (1 vez/dia) |
| Combate | Defesa | **Capstone: Briga de Rua** (4) | **Converte 30% do dano recebido em Energia** |
| **Social** | Negociação | Gente Boa (1) | +10% desconto em comércios amigos |
| Social | Negociação | Linguagem do Asfalto (2) | +15% desconto com Caminhoneiros e Motoclube |
| Social | Negociação | Politiquê (2) | Diálogos com Helena mais lucrativos |
| Social | Negociação | Jeitinho (3) | Crédito com Tia Marlene (compra fiado) |
| Social | Intimidação | Olhar Sério (1) | NPCs fracos fogem ao encará-los |
| Social | Intimidação | Voz Brutal (2) | Bando rival recua 25% mais rápido |
| Social | Intimidação | Empurrão Verbal (3) | Pode fazer PNJ desistir de cobrança |
| Social | Intimidação | Pesadelo do Beco (3) | +20% Reputação negativa gera medo proporcional |
| Social | Carisma | Sorriso Fácil (1) | +10% XP em diálogos |
| Social | Carisma | Cantada Ruim (2) | 30% chance de NPC rir e dar dica |
| Social | Carisma | Liderança (3) | Pode recrutar 1 aliado aleatório por bairro |
| Social | Carisma | Influência (3) | Reputação alta "vaze" para bairros vizinhos |
| Social | Informação | Informante (1) | Tia Marlene revela 1 fofoca/dia extra |
| Social | Informação | Olho Vivo (2) | Vê eventos aleatórios no mini-mapa 10s antes |
| Social | Informação | Rede do Beco (3) | Acesso a missões secretas |
| Social | Informação | **Capstone: Dono da Fofoca** (4) | **Compradores pagam 2× por informação** |
| **Sobrevivência** | Vigor | Fôlego (1) | +25% stamina de corrida |
| Sobrevivência | Vigor | Perna de Motoboy (2) | +20% velocidade de bicicleta |
| Sobrevivência | Vigor | Corpo de Chumbo (3) | +20 HP máximo |
| Sobrevivência | Vigor | Recuperação (3) | HP regenera 5%/min parado |
| Sobrevivência | Recursos | Catador (1) | Encontra 30% mais itens em lixo |
| Sobrevivência | Recursos | Gambiarra (2) | Crafta ferramentas com 25% menos sucata |
| Sobrevivência | Recursos | Comida de Rua (2) | Comidas baratas saciam +20% |
| Sobrevivência | Recursos | Conserto de Casa (3) | Reduz aluguel em 10% |
| Sobrevivência | Sanidade | Cabeça Boa (1) | Sanidade decai 20% mais devagar |
| Sobrevivência | Sanidade | Café na Veia (2) | Café recupera Sanidade ao invés de só Energia |
| Sobrevivência | Sanidade | Resiliência (3) | Imune a 1 evento de Sanidade/dia |
| Sobrevivência | Sanidade | Meditação (3) | Sanidade sobe 5% ao dormir 4h in-game |
| Sobrevivência | Vida Urbana | Sono Leve (1) | Dorme 30% mais rápido |
| Sobrevivência | Vida Urbana | Mão na Roda (2) | Pede carona a caminhoneiros 1 vez/dia grátis |
| Sobrevivência | Vida Urbana | Lê Sinais (3) | Antecipa enchente/greve em 6h in-game |
| Sobrevivência | Vida Urbana | **Capstone: Filho do Caos** (4) | **Penalidades por baixa Sanidade revertidas em bônus** |
| **Crime** | Furto | Dedos Leves (1) | +25% sucesso em furto simples |
| Crime | Furto | Bolso Fundo (2) | Itens furtados valiosos 20% mais comuns |
| Crime | Furto | Gancho (2) | Pode roubar carro popular sem alarme em 8s |
| Crime | Furto | Rey del Boleo (3) | Furtos em lojas geram 30% menos suspeita |
| Crime | Contrabando | Olho do Tonho (1) | Pagamento 10% maior em contrabando leve |
| Crime | Contrabando | Embalagem Fria (2) | Cargas não detectáveis por Guarda Municipal |
| Crime | Contrabando | Rota Alternativa (3) | Desbloqueia atalhos em canais/ruas estreitas |
| Crime | Contrabando | Distribuidor (3) | Pode vender carga direto ao Zé Pequeno |
| Crime | Arrombamento | Gatilho Macio (1) | Arromba fechaduras básicas sem mini-game |
| Crime | Arrombamento | Mestre Chave (2) | Arromba carros médios silenciosamente |
| Crime | Arrombamento | Coletor (3) | Abre cofres simples em residências |
| Crime | Arrombamento | Fantasma (3) | Alarmes demoram 50% mais a disparar |
| Crime | Networking | Crime Organizado (1) | Missões criminosas pagam +15% |
| Crime | Networking | Bando Próprio (2) | Recruta 2 capangas no Sítio do Capim |
| Crime | Networking | Lavanderia (3) | Dinheiro sujo convertido a 90% (vs. 70%) |
| Crime | Networking | **Capstone: Imperador do Caos** (4) | **Polícia aceita propina 1×/dia sem Reputação perdida** |

#### 1.5.2 Regras de Desbloqueio

- Árvores **Direção, Combate, Social e Sobrevivência** estão disponíveis desde o nível 1.
- A árvore **Crime** é desbloqueada ao atingir **Nível 5 OU** ao concluir a missão M-014 ("Serviço para o Tonho").
- Cada **capstone** exige 12 pontos investidos na árvore + Nível Geral ≥ 20.
- Habilidades de Mentoria/Networking (em itálico conceptual) só se ativam após missão específica com o respectivo mentor NPC (Otacílio para Direção, Tonho/Zé Pequeno para Crime, etc.).

### 1.6 Evolução do Personagem ao Longo do Jogo

| Fase | Nível | Foco | Marcos narrativos |
|---|---|---|---|
| **Quitação** (act 1) | 1–5 | Sobrevivência básica; primeira moto | Conhece Dona Cleide, Tia Marlene, Bira, Tonho |
| **Escolha** (act 2) | 6–15 | Escolha de facção; primeiras missões de crime/social | Conhece Otacílio, Tavinho, Helena, Zé Pequeno |
| **Subida** (act 3) | 16–29 | Domínio de um bairro; casa própria; capstones | Alianças/inimizades consolidadas |
| **Caos Total** (endgame) | 30–50 | Cidade reage às suas escolhas | Final variável por reputação |

### 1.7 Comportamento dos NPCs em Relação ao Protagonista

Toda interação do jogador é observada pelos NPCs por meio de três variáveis derivadas:

1. **Aparência atual** (roupa, sujeira, sangue cartoon) — afeta primeira impressão.
2. **Reputação global + facção + bairro** — afeta disponibilidade de serviços.
3. **Histórico de comportamento** (memória de IA por NPC) — registra agressões, favores e traições.

Essas três variáveis alimentam uma linha de diálogo dinâmica em cada encontro (sistema NLG com templates). Detalhes por NPC na §2.

---

## 2. Os 10 NPCs Principais

Cada NPC abaixo segue o mesmo template de design:

> **Nome** · **Aparência** · **Personalidade** · **Função na história** · **Missões relacionadas** · **Impacto no mundo aberto** · **Relação com facção/bairro** · **Interações possíveis** · **Consequências de escolhas** · **Reação ao comportamento do jogador**

Missões citadas por ID genérico (M-0xx) — numeração detalhada em [07-missoes.md](07-missoes.md).

---

### 2.1 Tia Marlene — A Informante da Vista Alegre

| Campo | Detalhe |
|---|---|
| **Nome completo** | Marlene Aparecida de Souza |
| **Idade** | 58 |
| **Local canônico** | Barraca de pastel na entrada principal da Comunidade Vista Alegre ("a Vista") |
| **Facção alinhada** | Neutra (informante de todos; simpatizante velada da Frente Popular) |

**Aparência**
Senhora baixa, robusta, cabelo grisalho preso num coque coberto por um bandana florida. Veste sempre um avental manchado de óleo e farofa, óculos de grau pendurados no pescoço, chinelão de dedo borboleta. Pastel na mão o tempo todo. Voz rouca de quem fuma "cigarrinho de palha" às escondidas.

**Personalidade**
Sagaz, maternal com quem merece, implacável com quem atrapalha. Conhece todos os segredos da Vista Alegre porque "ninguém repara na velha do pastel". Vende fofoca como commodity, aceita pagamento em pastel (o próprio) ou em favor. Tem humor ácido, ri das próprias piadas, xinga em português e iorubá básico.

**Função na história**
**Hub de informação e tutor de mundo.** É o primeiro NPC amigável que o jogador encontra na Vista Alegre (missão M-003, "Pastel com Conspiração"). Fornece dicas de gameplay (como funciona reputação, onde vender itens), avisa sobre operações policiais, blitze da fiscalização e movimentações da Milícia Escudo. É ela quem conecta o jogador a Zé Pequeno (M-009).

**Missões relacionadas**
- M-003 "Pastel com Conspiração" (tutorial de informação)
- M-008 "Entrega Especial" (levar marmita para Zé Pequeno)
- M-021 "Boato Miliciano" (revela movimento da Milícia Escudo)
- M-034 "A Receita Perdida" (história pessoal — buscar caderno de receitas roubado)
- M-047 "Boca de Forno" (cadeia de informação que move a facção Frente Popular)

**Impacto no mundo aberto**
Nível de **informação** disponível no bairro escala com a Reputação de Tia Marlene. Em Reputação alta (≥+50), ela revela eventos aleatórios antes do tempo no mini-mapa. Em Reputação baixa (≤−20), ela "esquece" de avisar sobre blitze — o jogador é pego desprevenido.

**Relação com facção/bairro**
- Vista Alegre: **+30** implícito (é parte do tecido social).
- Frente Popular: **+15** (vota e simpatiza, mas desconfia de Helena).
- Milícia Escudo: **−40** (a extorsão já lhe custou um fornecedor).
- Zé Pequeno: laço de longa data — funciona como conselheira informal dele.

**Interações possíveis**
- Comprar pastel (R$ 6–12; sacia Fome +15).
- Comprar "pastel especial" com bebida (cura Sanidade +5).
- Pagar R$ 20 por uma "fofoca quente" (revela um evento do dia).
- Trocar favor por informação (ex.: levar encomenda para alguém).
- Conversar sobre a história da Vista (lore pacing).

**Consequências de escolhas**
- Trair a confiança dela (revelar segredo à Milícia) bloqueia permanentemente o hub de informação da Vista Alegre.
- Ajudar no M-034 desbloqueia o **pastel premium** (bônus de Sanidade +10) e um cosmético: "Avental da Marlene".
- Escolher lado da Milícia no M-021 faz com que ela pare de vender para o jogador, mas fornece informações pela Frente Popular indiretamente.

**Reação ao comportamento do jogador**
- Aparecer armado/sangrando cartoon: "Filho, você tá bem? Quer um pastel de carne com banha?".
- Dirigir como louco perto da barraca: ela berra, depois cobra R$ 5 "pelo susto".
- Ser gentil (oferecer carona, ajudar com a barraca): Reputação +5 por encontro.
- Aparecer com roupas do Belvedere: "Veio aqui de Ferrari, moço? Pastel é pastel, viu.".

---

### 2.2 Delegado "Bira" / Aldemir Bira — A Polícia Civil Ambígua

| Campo | Detalhe |
|---|---|
| **Nome completo** | Aldemir Bira da Silva |
| **Idade** | 47 |
| **Local canônico** | Delegacia do Centro Histórico (DP-1) |
| **Facção alinhada** | Institucional (Polícia Civil); alinhamentos laterais variáveis |

**Aparência**
Homem de estatura média, bigode ralo, cabelo preto puxado para trás com brilho de brylcreem. Terno bege amassado, gravata deslocada, anel de formatura na mão direita. Sempre segurando um café em caneca térmica do Coringa. Olhar de quem já viu tudo e já cansou de tudo.

**Personalidade**
Cínico, fala baixo, profissional em público. Em particular, oscila entre honesto e corrupto conforme o contexto (e conforme o jogador). Tem um código pessoal confuso: "Não prendo quem não merece, mas também não solto quem paga bem". Faz piadas sombrias, é viciado em café, dizima o pacote de bala halls.

**Função na história**
**Agente ambíguo do Estado.** Atua como contraponto à Polícia Militar (que é mecânica/gatilho) — Bira é personagem. Oferece missões de cabo eleitoral (sujeira política), investigação e propina. Pode ser aliado, inimigo ou ferramenta. É a porta de entrada para a cadeia "polícia/crime organizado" da história principal.

**Missões relacionadas**
- M-005 "Biroscada no DP" (primeiro encontro — entrega de café/dados)
- M-013 "Arquivo 47" (recuperar processo que o compromete)
- M-022 "Cabo Eleitoral Sujo" (plantar prova contra político rival de Helena)
- M-029 "Quem Matou o Fiscal?" (investigação; revela ocorrências da Milícia)
- M-041 "A Propina" (definir lado: virar informante ou denunciá-lo à Corregedoria)
- M-052 "Mesa de Poker no DP" (linha secundária cômica)

**Impacto no mundo aberto**
Determinar o **nível de tolerância policial** no Centro Histórico. Bira alta Reputação = PM menos agressiva no Centro (−1 estrela efetiva em crimes leves). Bira baixa Reputação = perseguições mais longas, investigação ativa (eventos onde ele "fica no seu pé").

**Relação com facção/bairro**
- Polícia Civil: líder operacional local.
- Frente Popular: +25 (relação "profissional" com Helena; troca de favores).
- Milícia Escudo: −10 publicamente; +30 secretamente (suborno mensal).
- Centro Histórico: bairro de operação direta.

**Interações possíveis**
- Visitar o DP para "conversar" (oferta de missões).
- Pagar propina (R$ 200–2.000) para limpar 1 estrela de procurado.
- Denunciá-lo à Corregedoria (linha narrativa longa).
- Convidá-lo para um café (melhora o relacionamento pessoal).
- Comprar "informações quentes" sobre blitze futuras.

**Consequências de escolhas**
- Traí-lo no M-013: ele vira inimigo — manda PM perseguir você sem aviso.
- Virar informante no M-041: ganha árvore Crime + desbloqueia "Tolerância Policial" (capstone da árvore Crime).
- Denunciá-lo com sucesso: Bira é preso (substituído por um delegado rígido — fim das propinas, mas Reputação +30 com a Frente Popular).
- Decidir não se envolver: ele permanece disponível mas frio.

**Reação ao comportamento do jogador**
- Aparecer com 3+ estrelas: "De novo, Martins? Tá tentando bater record?".
- Cometer crime leve perto dele: oferece "acordo" na hora (R$ 500 e limpa).
- Dirigir embriagado (Sanidade baixa + estar pós-festa): multa + sermão.
- Trazer café (item raro): Reputação +5 instantânea.

---

### 2.3 Seu Otacílio — Mentor de Direção e Líder Caminhoneiro

| Campo | Detalhe |
|---|---|
| **Nome completo** | Otacílio Fernandes de Lima |
| **Idade** | 62 |
| **Local canônico** | Pátio da União dos Caminhoneiros no Polo Monte Verde |
| **Facção alinhada** | Líder da **União dos Caminhoneiros do Caos** (facção 1) |

**Aparência**
Homem alto, magro, queimado de sol. Bigode branco enorme (penteado), boné azul-tempestade do sindicato com aba curva. Camisa xadrez desabotoada, colete jeans, bota de couro desgastada. Cigarro mascado na boca (não aceso). Corrente de ouro grossa escondida sob a barba por fazer.

**Personalidade**
Sábio de estrada, fala por provérbios inventados ("Quem nunca capotou não conhece a vida", "Caminhoneiro que corre, chora"). Mentor nato, paciente com novatos, leal até a traição. Odeia o Motoclube (conflito canônico −40). Tem humor seco e dá conselhos de vida enquanto ensina a dar ré com carreta.

**Função na história**
**Mentor de direção e porta-voz da classe trabalhadora motorizada.** Introduz o sistema de direção avançada, large vehicles e a facção Caminhoneiros. Atua como consciência moral do jogador quando escolhe o lado do crime — desaprova, mas não abandona (a não ser que a quebra de confiança seja extrema).

**Missões relacionadas**
- M-007 "Carona pra Polinha" (primeiro encontro — dirigir carreta até Sítio do Capim)
- M-011 "Primeiros Passos no Asfalto" (tutorial de direção avançada)
- M-019 "Greve Geral" (cadeia faccional — mobilização dos Caminhoneiros)
- M-027 "Comboio Solidário" (escoltar 3 caminhões pela rodovia)
- M-038 "Duelo no Corredor" (confronto com Motoclube)
- M-049 "Última Viagem do Otacílio" (missão emocional, endgame)

**Impacto no mundo aberto**
Em Reputação alta, Caminhoneiros oferecem **carona grátis** em estradas (1 vez/dia), desconto em combustível nos postos do Polo Monte Verde (−20%) e reforço em perseguições (caminhões bloqueiam perseguidores). Em Reputação baixa, postos fecham para o jogador, e carretas podem tentar bloqueá-lo em rodovias.

**Relação com facção/bairro**
- Líder facção 1 (União dos Caminhoneiros): representa 100%.
- Polo Monte Verde: bairro-base, reputação +30 implícita.
- Motoclube: −40 (canônico).
- Frente Popular: +20 (aliança política por votos/logística).
- Sítio do Capim: lá tem família; simpatia +20.

**Interações possíveis**
- Aulas de direção (desbloqueiam nós da árvore Direção por R$ ou favores).
- Pegar carona com ele (desconto de viagem, conversa lore).
- Trabalhos de frete (missão recorrente de carga — renda estável).
- Greve (mecânica: ao apoiar, fechamentos no Polo Monte Verde afetam economia da cidade).
- Conversas de filosofia de estrada (cosmético, sobe Sanidade +3).

**Consequências de escolhas**
- Apoiar a greve no M-019: economia do Centro entra em colapso (preços +30% por 3 dias), Reputação +30 com Caminhoneiros, −20 com Helena (Frente Popular perde votos).
- Trair para o Motoclube: Otacílio corta relações; você perde acesso à árvore Mentoria.
- Salvar o comboio no M-027: ganha capacete "Cavalo de Aço" (capstone).
- Falhar a M-049 (endgame): término amargo com árvore Direção capada.

**Reação ao comportamento do jogador**
- Aparecer de moto do Motoclube: "Ainda não te virei as costas, mas tá demorando, hein?".
- Bater carreta: "Caminhão não é brinquedo, garoto. Outra."
- Ajudar caminhoneiro em problema na estrada: "+10 Reputação na hora, filho.".
- Aparecer bêbado: "Bebe? Não enquanto a chave na ignição, tá?".

---

### 2.4 Vereadora Helena Velasco — Política Corrupta

| Campo | Detalhe |
|---|---|
| **Nome completo** | Helena Velasco Tavares do Nascimento |
| **Idade** | 51 |
| **Local canônico** | Gabinete na Câmara Municipal (Centro) e casa no Jardim Belvedere |
| **Facção alinhada** | Líder da **Frente Popular de São Genésio** (facção 4) |

**Aparência**
Elegante, cabelo platinado na altura do ombro, tailleur verde-e-amarelo impecável (cores da facção). Brincos de pérola, unhas vermelhas, sapato de salto médio vermelho. Sempre com celular na mão e assessor(a) atrás. Sorriso treinado. Óculos de marca no topo da cabeça.

**Personalidade**
Calculista, carismática, fria quando contrariada. Discurso populista, prática oligárquica. Fala em terceira pessoa ("a vereadora acha que..."). Usa palavras como "transformação", "inovação", "acolhimento" sem nunca especificar. Adora ser adulada. Odeia contradição pública.

**Função na história**
**Vilã principal declarada — mas flexível.** Central da cadeia política da história principal. Oferece poder, dinheiro e proteção em troca de serviços (sujeira, dissimulação, propaganda). Pode ser deposta, aliada ou (raro) redimida parcialmente. Helena conecta o jogador a missões no Belvedere, Centro e (secretamente) com a Milícia Escudo (relação de propina).

**Missões relacionadas**
- M-009 "Selfie com a Vereadora" (apresentação via Bia)
- M-018 "Cabine Eleitoral" (campanha — knock doors, panfletagem)
- M-024 "Boato Falso" (plantar fake news sobre rival)
- M-031 "Propina da Milícia" (intermediar pagamento Escudo ↔ Frente)
- M-036 "Debate na Câmara" (mini-game de oratória)
- M-044 "Quem Vota em Helena?" (revelação — escolher expor ou proteger)
- M-053 "A Queda" (final variável — impeachment, fuga ou consolidação)

**Impacto no mundo aberto**
Reputação alta com Helena **abre o Belvedere** (comércio de luxo, contratos, segurança privada). Reputação baixa faz com que a Frente Popular use a mídia contra o jogador (eventos de "cancelamento" — preço de serviços sobe no Centro por 1 dia).

**Relação com facção/bairro**
- Líder facção 4 (Frente Popular).
- Jardim Belvedere: +40 (base eleitoral).
- Centro Histórico: +20 (segunda base).
- Milícia Escudo: −30 público; +50 privado (propina).
- Caminhoneiros: +20 (votos/logística — aliança pragmática).

**Interações possíveis**
- Reuniões no gabinete (atributo Social sobe +1 por reunião útil).
- Doações (R$ 500+) geram Reputação +5.
- Atribuição de "serviços" (missões de cabo eleitoral sujo).
- Eventos de gala (cosmético, mas com mecânica de fama).
- Pedir "favores" (limpar multas, liberar alvará — custa propina).

**Consequências de escolhas**
- Apoiar no M-044: vira braço direito; desbloqueia árvore Social capstone.
- Expor no M-053: Helena é presa/impeachment; Frente Popular entra em crise; Milícia assume mais Centro.
- Aliar-se à Milícia no M-031: Helena descobre e corta relações permanentemente.
- Negar participação: ela respeita distância mas não ajuda.

**Reação ao comportamento do jogador**
- Aparecer sujo/pobre: "Ah, querido, talvez precise de um banho antes de falar com a vereadora...".
- Cometer crime perto do gabinete: manda Bira cuidar — +1 estrela automática.
- Trazer dinheiro/contrato: sorriso largo; Reputação +5.
- Aparecer com Bia: "Vocês dois! As fotos saíram ótimas.".

---

### 2.5 Tavinho — Líder do Motoclube Cavaleiros do Asfalto

| Campo | Detalhe |
|---|---|
| **Nome completo** | Otávio "Tavinho" Bezerra |
| **Idade** | 36 |
| **Local canônico** | Sede do Motoclube na Praia de Itaúna |
| **Facção alinhada** | Líder dos **Motoclube Cavaleiros do Asfalto** (facção 3) |

**Aparência**
Musculoso, cabelo raspado dos lados com topete negro engomado, barba cavada. Colete jeans vermelho-sangue com as costas bordadas ("CAVALEIROS DO ASFALTO · SÃO GENÉSIO"). Tatuagens de caveira e chamas nos braços. Óculos escuros mesmo à noite. Chinelo de couro com meia branca (sua marca registrada cômica).

**Personalidade**
Estereotipo do motoqueiro "bom de briga, bom de copo, mau de cabeça". Honra de bando, código de lealdade antiquado. Ri alto, bebe alto, corre rápido. Tem lado romântico escondido (escreve poemas para a moto). Odeia caminhoneiros (canônico −40) e policiados (Bira sobretudo).

**Função na história**
**Líder de facção volúvel — ferramenta ou inimigo.** Tavinho introduz o mundo das motos, rachas e extorsão leve de corredor. Funciona como o "irmão mais velho bagunceiro": abre portas para a vida noturna, missões de fama com Bia e conflitos territoriais. Pode ser assassinado, deposto ou tornado aliado de ferro.

**Missões relacionadas**
- M-012 "Batida Diferenciada" (primeira corrida no corredor)
- M-020 "Racha na Orla" (corrida ilegal em Itaúna)
- M-026 "Bate-boca com Caminhoneiro" (escalada do conflito faccional)
- M-030 "Cobrança no Beco" (missão de extorsão leve)
- M-039 "A Lei do Asfalto" (decidir código do clube — linha filosófica)
- M-050 "Última Volta" (final variável para o Motoclube)

**Impacto no mundo aberto**
Reputação alta: motociclistas NPCs saúdam você com aceno de mão; desbloqueia garagem compartilhada do clube (motos gratuitas com reabastecimento); abre missões de racha (renda alta). Reputação baixa: motociclistas aleatórios podem bater em você no trânsito; Reputação negativa no Motoclube causa penalidade em Praia de Itaúna.

**Relação com facção/bairro**
- Líder facção 3 (Motoclube).
- Praia de Itaúna: +40 (território de festa).
- Vista Alegre: +15 (relação com mototaxistas).
- Caminhoneiros: −40 (canônico).
- Milícia Escudo: −20 (escaramuças).

**Interações possíveis**
- Corridas (apostar R$ ou veículo).
- Beber na sede (custa Energia, sobe Sanidade, gera lore).
- Pedir reforço (1 motociclista aliado por dia em Reputação +50).
- Customizar moto no estilo clube (cosmético + bônus social).
- Filiação: tornar-se membro (após M-039).

**Consequências de escolhas**
- Vencer M-020: Tavinho oferece colete do clube (cosmético premium).
- Trair o clube no M-026 (lado Otacílio): perde acesso ao clube; motos ficam 30% mais caras.
- Decidir código "honrado" no M-039: clube vira aliado de Zé Pequeno contra a Milícia.
- Matar Tavinho (raro, final extremo): motociclistas caçam você por toda a cidade.

**Reação ao comportamento do jogador**
- Aparecer de carro: "Cê veio de carro? Tá de sacanagem, né?".
- Bater moto perto dele: risada escandalosa + piada.
- Trazer cerveja: "Esse é o meu garoto! Tá dado o clube.".
- Aparecer com Otacílio: conflito imediato (cutscene).

---

### 2.6 Tonho da Van — Contrabandista Leve

| Campo | Detalhe |
|---|---|
| **Nome completo** | Antônio "Tonho" Pereira da Costa |
| **Idade** | 41 |
| **Local canônico** | Estacionamento de vans alternativas no Centro Histórico (e rotas para a Vista Alegre) |
| **Facção alinhada** | Independente; vira subcontratado do jogador em Crime |

**Aparência**
Atarracado, sorriso largo, boné preto virado para trás, camiseta de time (Helênico FC — time fictício). Bermuda cargo com muitos bolsos. Óculos de sol baratos. Chinelo. Sempre com uma garrafa térmica de café no painel da van.

**Personalidade**
Simpático, falante, "amigo de todo mundo". Conhece as rotas ilegais da cidade ("a van passa onde o Waze não sabe"). Vende produtos de contrabando leve (cigarro paraguaio, CDs piratas, peças de celular, remédio sem nota). Tem moralidade elástica: nunca trafica drogas pesadas nem armas, mas "faz um frete" pra quem pagar. Família numerosa, vive falando da esposa e filhos.

**Função na história**
**Ponte entre o mundo legal e o crime organizado.** Tonho é a porta de entrada para a árvore Crime. Missões dele envolvem transporte de carga duvidosa, evasão de fiscalização e Logística de bairro. Também é meio de transporte rápido (van alternativa como fast-travel pago).

**Missões relacionadas**
- M-014 "Serviço para o Tonho" (desbloqueia árvore Crime)
- M-016 "Rota da Van" (fast-travel pago pela primeira vez)
- M-023 "Pacote sem Pergunta" (missão ética — o que tem no pacote?)
- M-032 "Blitz na Avenida" (evasão de fiscal)
- M-040 "Tonho em Apuros" (ajudá-lo ou deixá-lo preso)
- M-048 "A Frota do Tonho" (gerenciar frota de vans — sistema de renda passiva)

**Impacto no mundo aberto**
Tonho é o **fast-travel econômico** do jogo: por R$ 5–20 ele leva você entre bairros (mais barato que táxi, mais rápido que ônibus). Reputação alta desbloqueia "vans especiais" (sem paradas) e contrabando de itens premium. Reputação baixa: ele "não te conhece", e o jogador perde a opção de fast-travel pago.

**Relação com facção/bairro**
- Neutro em todas as facções oficiais (sobrevivência).
- Vista Alegre: +25 (vai e volta o tempo todo).
- Centro: +15 (base).
- Frente Popular: indiferença profissional.
- Crime (árvore): fachada de informante.

**Interações possíveis**
- Pagar van (R$ 5–20, fast-travel).
- Comprar contrabando (cosméticos premium baratos, cigarros que sobem Sanidade −2 mas são aceitos por alguns NPCs).
- Pegar trabalho (missões de entrega).
- Bate-papo sobre futebol (sobe Sanidade +2).
- Compartilhar "pechincha" (rede de contato).

**Consequências de escolhas**
- Abrir o pacote no M-023: descobre contrabando mais pesado (decisão ética); pode denunciar (perde Tonho), entregar a Zé Pequeno (ganha Reputação na Vista) ou fingir que não viu (status quo).
- Salvar Tonho no M-040: ele vira informante vitalício (1 fofoca/dia grátis).
- Denunciá-lo: perde árvore Crime temporariamente; Reputação +10 com Bira, −20 com Vista Alegre.
- Comprar frota no M-048: renda passiva (R$ 50–200/dia in-game).

**Reação ao comportamento do jogador**
- Aparecer com polícia atrás: "Sobe, sobe, sobe! Depois a gente resolve!".
- Pagar com moeda: "Moço, vou trocar onde?".
- Pedir desconto: "Pra você, R$ 4. Só hoje.".
- Oferecer carona (irônico): "Eu é que levo você, rapaz!".

---

### 2.7 Betina "Bia" Reis — Influenciadora Digital

| Campo | Detalhe |
|---|---|
| **Nome completo** | Betina Reis Couto |
| **Idade** | 23 |
| **Local canônico** | Apartamento no Jardim Belvedere; grava em Itaúna, Vista Alegre (conteúdo "periferia autêntica") e Centro |
| **Facção alinhada** | Independente; colab eventual com Frente Popular |

**Aparência**
Jovem, magra, cabelo loiro platinado com mechas coloridas (pink/verde). Maquiagem sempre completa. Roupas de marca "streetwear luxury" — moletom oversized, tênis chunk, brincos maxi. Smartphone sempre na mão com ring light portátil. Bolsa mini de grife.

**Personalidade**
Energia alta, fala rápido, usa gírias da internet ("bye", "fessado", "gourmet", "ES-PEC-TA-CU-LAR"). Vaidosa mas afetuosa. Tem momentos de vulnerabilidade (insegurança real por trás da persona). É mais esperta do que aparenta. Vende "autenticidade" mas é calculista de feed.

**Função na história**
**Cadeia de fama e mídia social.** Bia introduz o **sistema de seguidores** (métrica paralela de progressão) e missões de conteúdo: gravar viral em local perigoso, fazer trend, colaborar com marcas. Conecta o jogador a Helena (apadrinhamento) e abre portas no Belvedere.

**Missões relacionadas**
- M-010 "Live da Bia" (primeira colaboração)
- M-015 "Trend da Vista Alegre" (ir à favela gravar — dilema ético)
- M-025 "Evento de Marca" (presença VIP)
- M-033 "Cancelling" (defender ou abandonar Bia num escândalo)
- M-042 "Documentário Periferia" (com Zé Pequeno — decisões editoriais)
- M-051 "Bia Millions" (atigir 1M de seguidores — capstone fama)

**Impacto no mundo aberto**
Reputação alta com Bia abre **sistema de seguidores** (método de monetização indireta: marcas pagam por post). Ela abre portas para o Belvedere e faz propaganda positiva do jogador (Reputação +5 espalhada). Reputação baixa: ela pode fazer vídeo "expondo" o jogador (Reputação −10 global).

**Relação com facção/bairro**
- Independente (vende imagem).
- Jardim Belvedere: +30 (base).
- Praia de Itaúna: +25 (cenário de gravação).
- Frente Popular: colab publicitária (+10).
- Vista Alegre: +10 (apropriação cultural periódica — tensão com Zé Pequeno).

**Interações possíveis**
- Gravar conteúdo (mini-game de posing/timing).
- Pedir indicação (sobe seguidores).
- Contratar para promoção (Bia posta produto; você ganha R$).
- Conversas profundas (às vezes revela insegurança — sobe Sanidade +5).
- Ajudar em dilema (defender em cancelamento).

**Consequências de escolhas**
- Defender Bia no M-033: vira amizade fixa; desbloqueia linha "Mídia Aliada" (árvore Social capstone parcial).
- Abandoná-la no cancelamento: ela vira inimiga; corte constante de Reputação.
- Recusar gravar na Vista Alegre no M-015: ganha Reputação +20 com Zé Pequeno, mas Bia esfria.
- Decidir tom autêntico no M-042: queda de reputação em bairros pobres, sobe em bairros ricos (e vice-versa).

**Reação ao comportamento do jogador**
- Aparecer sujo de obra: "Foto linda! Só... não me abraça, ok?".
- Fazer algo viral: "Isso é conteúdo, best! Repostei!".
- Estar com fama negativa: corta relações públicas, mas manda DM de apoio.
- Trazer presente: "AI MEU DEUS obrigada! Story pronto.".

---

### 2.8 "Zé Pequeno do Beco" / Josival — Líder Comunitário da Vista Alegre

| Campo | Detalhe |
|---|---|
| **Nome completo** | Josival Bezerra da Silva |
| **Idade** | 39 |
| **Local canônico** | Beco principal da Comunidade Vista Alegre (sede simbólica: laje convertida em ponto de encontro) |
| **Facção alinhada** | Líder comunitário informal; resiste à Milícia Escudo |

**Aparência**
Homem negro, magro, cabelo black power sempre bem cuidado. Camisa de linho branca (estilo afro-brasileiro), calça de sarja, chinelo. Corrente de ouro com símbolo de Orisha (mãe de santo o presenteou). Tatuagem no antebraço com o nome da filha. Voz grave e calma.

**Personalidade**
Paciente, estratégico, carismático. Líder por consenso, não por medo. Fala mansa, mas firme. Faz diplomacia entre traficante pequeno, igreja, associação de moradores e políticas públicas. Odeia a Milícia Escudo (quer expulsá-la da Vista). Relação ambígua com Helena (sabe que ela só aparece em ano de eleição).

**Função na história**
**Consciência moral do jogo.** Zé Pequeno é o personagem que humaniza a Vista Alegre. Suas missões envolvem mediação de conflitos, organização comunitária e resistência à Milícia. É o contraponto à cadeia "crime fácil" — oferece uma via de progressão baseada em Reputação social.

**Missões relacionadas**
- M-008 "Entrega Especial" (marmita da Tia Marlene até ele)
- M-017 "Assembleia do Beco" (mini-game de oratória)
- M-022 "Boato Miliciano" (cadeia anti-Escudo)
- M-035 "Operação Laje" (resgate de familiar preso pela Milícia)
- M-043 "Votação Comunitária" (você como candidato a líder temporário)
- M-046 "Funk da Resistência" (evento cultural — show coletivo)

**Impacto no mundo aberto**
Reputação alta com Zé Pequeno **transforma a Vista Alegre em safehouse estendida**: moradores protegem o jogador (PM não entra sem mandado, mini-mapa revela atalhos). Reputação baixa: a Vista vira zona hostil — moradores fecham portas, podem dedurar você.

**Relação com facção/bairro**
- Vista Alegre: 100% (líder comunitário).
- Milícia Escudo: −60 (inimigo declarado).
- Frente Popular: −20 (desconfiança).
- Tia Marlene: relação de longo prazo (conselheira dele).

**Interações possíveis**
- Assembleias (decisões comunitárias com consequências sistêmicas).
- Mediação (mini-game de diálogo entre facções menores).
- Trabalho social (R$ baixo, Reputação alta, Sanidade +).
- Pedir abrigo (safehouse).
- Conversas sobre política local (lore profundo).

**Consequências de escolhas**
- Apoiar resistência anti-Milícia (M-035): Vista se torna território "limpo"; Milícia revida com mais força no Centro.
- Negociar com Milícia: Zé Pequeno corta relações; Vista hostil ao jogador.
- Vencer M-043: ganha título "Compadre do Beco" (capstone Social local).
- Fracassar em M-046: show é invadido, sobe Caos da cidade em +10.

**Reação ao comportamento do jogador**
- Aparecer armado (raro): "Aqui não, irmão. A gente resolve falando.".
- Ajudar morador: "Valeu. A Vista lembra de quem ajuda.".
- Trair a comunidade: silêncio gélido; perde safehouse.
- Aparecer de roupas de rico: "Tá bonito. Tá precisando de algo?".

---

### 2.9 Dr. Éverton — Dono da Oficina Mecânica

| Campo | Detalhe |
|---|---|
| **Nome completo** | Éverton Magalhães Tavares |
| **Idade** | 45 |
| **Local canônico** | Oficina "Parafuso Mágico" no Polo Monte Verde |
| **Facção alinhada** | Simpatizante União dos Caminhoneiros (+20) |

**Aparência**
Robusto, bigode castanho, óculos de proteção no topo da cabeça sempre. Macacão azul manchado de graxa com o nome "ÉVERTON" bordado no peito. Camiseta regata por baixo. Tatuagem de chave de roda no braço. Sempre com chave inglesa na mão ou no bolso traseiro.

**Personalidade**
Direto, prático, sem-papas. Fala pouco, ouve muito. Mente brilhante para mecânica. Preço justo, mas não gosta de pechincha. Tem código de honra: "Aqui não tem carro de bandido, tem carro quebrado". Odeia peça chinesa falsificada (fonte recorrente de piada).

**Função na história**
**Provedor de upgrades veiculares.** Éverton é o gateway para o sistema de customização de veículos (reparo, paintjob, perfomance, bling). Missões dele envolvem busca de peças raras, testes de veículo e gestão da oficina. Conecta o jogador ao mundo industrial do Polo Monte Verde.

**Missões relacionadas**
- M-006 "Carro Que Não Pega" (tutorial de oficina)
- M-019 "Greve Geral" (cadeia — Ele e Otacílio)
- M-028 "Peça Rara no Ferro-Velho" (busca de item)
- M-037 "Test Drive do Protótipo" (corrida experimental)
- M-045 "Falsificação Chinesa" (cadeia — expor fornecedor desonesto)
- M-049 (cadeia) "Última Viagem do Otacílio" (colabora no conserto final)

**Impacto no mundo aberto**
Reputação alta: **descontos em reparos/upgrades (−25%)**, acesso a peças premium, slot de inventário de ferramentas. Reputação baixa: oficina fecha para o jogador; veículos danificados ficam 30% mais caros de consertar em outras oficinas (genéricas).

**Relação com facção/bairro**
- Polo Monte Verde: +30 (bairro-base).
- União dos Caminhoneiros: +20 (otacílio é amigo).
- Neutro em outras facções.
- Dr. Éverton respeita Bira (não quer problemas), mas não o ajuda ativamente.

**Interações possíveis**
- Reparo de veículo (R$ 50–5.000 conforme dano).
- Upgrades (motor, freio, pneu, paintjob, nitro — detalhes em [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md)).
- Compra/venda de peças (mercado paralelo).
- Customização estética (pintura, adesivos, neon).
- Aulas práticas (XP em Direção +5 por aula).

**Consequências de escolhas**
- Expor falsificação no M-045: Éverton ganha fornecedor legítimo; descontos vitalícios.
- Trair para fornecedor chines (raro): perde oficina; Éverton vira inimigo.
- Ajudar no M-028: desbloqueia "Peça Rara" para moto premium.
- Falhar M-037: protótipo explode (cômico, sem feridos); Sanidade −10 ao jogador.

**Reação ao comportamento do jogador**
- Bater muito o carro: "De novo? Tá fazendo isso de propósito, né?".
- Trazer moto rara: "Que peça! Não vendendo, só consertando.".
- Oferecer suborno: "Aqui é preço justo. Tá errado? Procura outro.".
- Aparecer de terno: "Tá vendendo seguro ou quer consertar o carrão?".

---

### 2.10 Dona Cleide — Vizinha Fofoqueira e Dona do Quitinete

| Campo | Detalhe |
|---|---|
| **Nome completo** | Cleide Aparecida do Nascimento |
| **Idade** | 65 |
| **Local canônico**Quitinete do Centro Histórico (andar de baixo);	controla o prédio |
| **Facção alinhada** | Independente (mas fonte de fofoca sobre todos) |

**Aparência**
Senhora baixa, cabelo azul-turquesa permanente (visível a quarteirões), vestido floral, chinelão de pelúcia rosa. Óculos enormes pendurados no pescoço. Sempre com um copo de café ou garrafa térmica. Aparelho de surdez no ouvido (que ela desliga convenientemente). Bolsa enorme com "tudo que precisa".

**Personalidade**
Fofoqueira compulsória, mas de bom coração. Tem opinião sobre tudo. Reclama de barulho mas faz mais barulho ainda. Faz bolo para os vizinhos quando quer informação. Vota em Helena mas diz que "votaria em outro se tivesse". Guarda rancor de 30 anos por um vizinho que sumiu com o secador.

**Função na história**
**Alívio cômico e hub de lore doméstico.** Dona Cleide é a locatária do quitinete do jogador — relacionamento fixo, impossível de evitar. Ela fornece tutorial de "vida doméstica" (aluguel, sono, alimentação), fofocas sobre outros NPCs ( às vezes úteis) e missões cômicas. É a "tia brasileira" universal.

**Missões relacionadas**
- M-002 "Primeira Noite no Quitinete" (tutorial casa)
- M-004 "O Secador Perdido" (cadeia cômica de investigação doméstica)
- M-014 (cadeia) "Serviço para o Tonho" (ela fofoca e sugere o contato)
- M-026 "Assembleia de Condomínio" (você presidente do condomínio — caos)
- M-041 (cadeia) — ela sabe de algo sobre Bira (informante acidental)
- M-054 "Bolo da Cleide" (missão final afetiva do arco doméstico)

**Impacto no mundo aberto**
Cleide afeta a **vida doméstica** do jogador: ela cobra aluguel em dia (R$ 950/mês in-game), reclama se você fizer barulho após as 22h (multa simbólica de R$ 5), oferece bolo (comida grátis 1x/dia em Reputação alta) e fornece fofoca (1 dica grátis/dia sobre movimento no Centro).

**Relação com facção/bairro**
- Centro Histórico: +30 (vive lá há 40 anos).
- Vota na Frente Popular ("desse ano").
- Odeia Seu Tobias (vizinho mal-humorado) — rixa pessoal.
- Tia Marlene: primas distantes (network de fofoca municipal).

**Interações possíveis**
- Pagar aluguel (obrigatório; atraso gera Reputação −5 com ela).
- Bater papo no corredor (sobe Sanidade +2, food -2 "porque ela te empurra biscoito").
- Pedir informação (R$ 0 em Reputação alta, R$ 10 em baixa).
- Ajudar em tarefas (sobe Reputação +5 cada).
- Fofocar (vendendo informação): economia paralela "indústria de fofoca".

**Consequências de escolhas**
- Ajudar no M-004: ganha "Secador Perdido" (cosmético mas integrado a side quest).
- Virar presidente do condomínio (M-026): mini-jogo de gestão; desbloqueia "quitinete premium" sem aluguel.
- Atrasar 3 meses de aluguel: despejo (perde safehouse do Centro; precisa realocar).
- Trair fofoca: ela vira inimiga doméstica; barulhos viram multas constantes.

**Reação ao comportamento do jogador**
- Chegar tarde: "Já são 22h15, moço! A dona Cleide precisa dormir!".
- Trazer namorado(a): "Eeee, tem alguém novo no prédio! Conte tudo.".
- Aparecer bêbado: "Tô ligada. Café amanhã de manhã.".
- Trazer presente: "Ai que amor! Olha o bolo que vou fazer!".

---

## 3. Os 5 Tipos de Vizinhos

Vizinhos são NPCs recorrentes que habitam o prédio do jogador (Centro) ou aparecem em safehouses espalhadas pela cidade (Vista Alegre, Belvedere, etc.). Eles têm **comportamento aleatório diário** (sistema de rotina), geram micro-eventos e oferecem tanto humor quanto consequências. Cada vizinho é um "arquétipo" — instâncias podem variar em nome, mas mantêm o tipo (ex.: todo prédio tem seu "Seu Arlindo").

### 3.1 Seu Arlindo — O Fofoqueiro

| Campo | Detalhe |
|---|---|
| **Nome canônico** | Arlindo Pereira |
| **Idade** | 58 |
| **Local** | Andar intermediário do prédio do Centro (em outros prédios: variação com mesmo tipo) |

**Descrição**
Aposentado, veste regata branca, calça de moletom, chinelo. Sempre na janela com binóculos (não esconde). Caderninho onde anota placas de carro, horários e visitantes. Vende informação por R$ 5 a R$ 50.

**Comportamento aleatório (1d6 por dia)**
1. Acorda 6h, posiciona-se na janela; observa movimento até 22h.
2. Sai às 9h para o bar da esquina; volta bêbado às 17h.
3. Visita Tia Marlene (cross-bairro) para "trocado de fofoca".
4. Briga com Seu Tobias (rival).
5. Organiza Bingo do prédio (você convidado).
6. Dia quieto: lê jornal. (raro)

**Humor**
Cômico cotidiano; cópia o estereótipo do "vizinho fofoqueiro brasileiro".

**Interações**
- Comprar fofoca (R$ 5–50).
- Trocar informação (negociação).
- Convidar para churrasco (gera Pontos Social).
- Ignorar (ele reclama no corredor).

**Consequências**
- Reputação alta: ele alerta sobre blitze/movimentação.
- Reputação baixa: ele espalha boato ruim (Reputação global −5).
- Trair segredo: vira inimigo; fofocas viram prejudiciais.

### 3.2 Vanderlei — O Churrasqueiro

| Campo | Detalhe |
|---|---|
| **Nome canônico** | Vanderlei "Vandinho" Gomes |
| **Idade** | 42 |
| **Local** | Área de churrasqueira do prédio (ou quintal em safehouse) |

**Descrição**
Veste camiseta regada do Helênico Futebol Clube, calça cargo, chinelão. Carvão, sal grosso e cerveja sempre à mão. Acende churrasqueira em qualquer clima, qualquer horário. Especialidade: picanha mal-passada, linguiça defumada, queijo coalho.

**Comportamento aleatório (1d6 por dia)**
1. Acende churrasco às 7h da manhã (clássico).
2. Faz "churrasco-relâmpago" às 15h (sem motivo).
3. Convida o prédio inteiro.
4. Briga com Seu Tobias por causa da fumaça.
5. Vai ao ferro-velho comprar peça (cross-bairro).
6. Dia sem churrasco (raríssimo — Reputação +5 com Seu Tobias).

**Humor**
Estereotipo "churrasqueiro de domingo"; sempre faz piada com carne.

**Interações**
- Aceitar churrasco (sobe Fome +30, Sanidade +5).
- Recusar educação (sem penalidade).
- Ajudar (ganha Reputação +5 + porção extra).
- Levar carne (sobe Reputação +10).

**Consequências**
- Reputação alta: sempre convida (fonte de comida grátis).
- Reputação baixa: bloqueia acesso à churrasqueira (penalidade social).
- Traição: ele desliga o churrasco e vira mal-humorado também.

### 3.3 Dona Cida — A Religiosa

| Campo | Detalhe |
|---|---|
| **Nome canônico** | Aparecida "Cida" Maria dos Santos |
| **Idade** | 72 |
| **Local** | Andar térreo (sempre com terço na mão e santinho na porta) |

**Descrição**
Vestido florão longo, véu branco na cabeça, terço no pescoço. Rezas altas às 6h, 12h e 18h. Prega para todos. Julga roupa curta, tatuagem, "más companhias". Mas tem lado bom: benze, cura males leves e é a primeira a aparecer com sopa quando alguém adoece.

**Comportamento aleatório (1d6 por dia)**
1. Reza do terço em voz alta às 18h (audiível em 3 andares).
2. Visita a igreja do Centro (cross-evento).
3. Distribui santinhos no corredor.
4. Conversa com Dona Cleide (cumplicidade fofoqueira).
5. Leva sopa para jogador (se Sanidade baixa).
6. Jejum e silêncio (raro — peacefully).

**Humor**
Estereótipo da "tia evangélica/católica brasileira" — alternar reverência e julgamento.

**Interações**
- Aceitar benção (sobe Sanidade +5).
- Pedir oração (cura HP pequeno).
- Recusar julgamento (Reputação neutra, ela persiste).
- Discutir teologia (mini-game intelectual; raro).

**Consequências**
- Reputação alta: ela reza por você (bônus de Sanidade contínuo).
- Reputação baixa: ela "adivinha" seus pecados (Reputação −5 global periódica).
- Conversão (extremo raro): título "Crente do Prédio" (capstone Social local).

### 3.4 Seu Tobias — O Mal-humorado

| Campo | Detalhe |
|---|---|
| **Nome canônico** | Tobias Lima |
| **Idade** | 64 |
| **Local** | Andar imediatamente acima do jogador |

**Descrição**
Carrancudo, bigode caído, camisa xadrez abotoada até o pescoço. Tapa na orelha sempre pronto. Reclama de barulho, música, cheiro de comida, cheiro de carro, cheiro de pessoa. Mantém um caderno de "Registros de Perturbação" que apresenta ao síndico.

**Comportamento aleatório (1d6 por dia)**
1. Bate no teto com vassoura às 22h01.
2. Liga para a Guarda Municipal (ReportBarulho).
3. Distribui panfletos "Silêncio é Ouro".
4. Reclama da fumaça do Vanderlei.
5. Vai à sede da Frente Popular reclamar de trânsito (cross-bairro).
6. Dia "bom humor" (raríssimo — fala "bom dia" uma única vez).

**Humor**
Estereótipo do "vizinho rabugento"; riso pelo desconforto que causa.

**Interações**
- Pedir desculpas (sobe Reputação +2, ele desconfia).
- Subornar (R$ 20 = silêncio por 1 dia).
- Provocar (sobe Caos +1, penalidade Reputação).
- Confrontar (mini-game de diálogo).

**Consequências**
- Reputação alta (extremamente difícil): ele "tolera" sua existência.
- Reputação baixa: chama Fiscal da Prefeitura (multa R$ 100) ou Bira (investigação).
- Estratégia ótima: ignorar + manter bom comportamento à noite.

### 3.5 Vó Ivone — A Idosa Gente-Boa

| Campo | Detalhe |
|---|---|
| **Nome canônico** | Ivone Aparecida Borges |
| **Idade** | 78 |
| **Local** | Térreo (sempre na cadeira de balanço na área comum) |

**Descrição**
Pequena, frágil, sorriso enorme. Cabelo branco preso, vestido leve, chinelinho. Tricô na mão sempre. Vive rodeada de netos (eventualmente aparecem NPCs "Neto(a) da Vó"). Cozinha o dia todo; cozinha mais do que come.

**Comportamento aleatório (1d6 por dia)**
1. Faz bolo de fubá e distribui (Fome +20, Sanidade +10).
2. Tricota cachecol para o jogador (cosmético, após 3 ingests).
3. Conta história de São Genésio antigo (lore pacing).
4. Convida para jogo de canastra (mini-game).
5. Sobe no terraço para ver pôr do sol (cross-evento com Vista).
6. "Reclama" carinhosamente do barulho do Vanderlei.

**Humor**
Afetuoso, saudoso, levemente demente (humor sem maldade).

**Interações**
- Aceitar comida (sempre positivo).
- Conversar (sobe Sanidade +5).
- Ajudar nas compras (Reputação +5).
- Pedir conselho (revela dica de vida/NPC).

**Consequências**
- Reputação alta: vira "neto(a) adotivo(a)" — acesso a quitinete premium.
- Reputação baixa: ela se preocupa (sem penalidade — ela é incapaz de ser má).
- Morte natural (endgame, evento roteirizado): reação emocional obrigatória; sobe Caos em +5.

### 3.6 Tabela de Comportamentos Aleatórios por Vizinho

| Vizinho | Comportamento mais comum | Hora pico | Consequência típica | Recompensa possível |
|---|---|---|---|---|
| **Seu Arlindo** (Fofoqueiro) | Observação na janela + venda de fofoca | 9h–17h | Reputação +/− por informação | Informação útil, R$ por venda |
| **Vanderlei** (Churrasqueiro) | Acende churrasco | 7h ou 15h | Fome +, Sanidade + | Comida grátis |
| **Dona Cida** (Religiosa) | Reza alta + benze | 6h, 12h, 18h | Sanidade +, Reputação moral | Cura pequena,或ação |
| **Seu Tobias** (Mal-humorado) | Reclama e registra | 22h | Reputação −, multa possível | Silêncio com suborno |
| **Vó Ivone** (Idosa Gente-Boa) | Distribui bolo e sabedoria | 15h | Sanidade +, Fome + | Cosmético, lore, XP Social |

### 3.7 Tabela de Sinergias e Conflitos entre Vizinhos

| Par | Relação | Evento gerado |
|---|---|---|
| Arlindo × Cleide | Aliados (fofoca compartilhada) | Troca de informação amplifica Reputação |
| Vanderlei × Tobias | Inimigos (fumaça × silêncio) | Mini-evento "Guerra do Churrasco" |
| Cida × Tobias | Tensão (religião × rabugice) | Cida reza por ele, ele reclama da reza |
| Ivone × todos | Matriarca afetiva | Evento "Bolo da Vó" une prédio por 1 dia |
| Arlindo × Vanderlei | Neutros | Vizinhos dividos em "esportes" e "futebol" |

---

## 4. Animais Urbanos

A fauna urbana de São Genésio é parte do tecido caótico da cidade. Animais não são apenas cosméticos: cada um oferece interações que afetam atributos (especialmente **Sanidade**) e geram micro-eventos cômicos. **Nenhum animal pode ser morto pelo jogador** (regra de design 14+; atropelamento causa apenas penalidade de Sanidade, e o animal "sai correndo").

### 4.1 Tabela de Animais Urbanos

| Animal | Bairros comuns | Comportamento base | Humor | Reação a provação |
|---|---|---|---|---|
| **Pombos** | Centro, Praia de Itaúna | Bando em praças; piam; voam em bando quando assustados | Cômico (ataque bando tipo "Os Pássaros", sem dano) | Se alimentados: seguem jogador por 30s. Se espantados: voltam em 1min |
| **Cachorros de rua** | Vista Alegre, Sítio do Capim | Latem, seguem jogador, dormem em calçadas | Afetivo/cômico | Se alimentados: lealdade; se agredidos: latem (sem dano), Reputação −3 |
| **Gatos** | Todos (telhados, muros) | Indiferentes, dormem, espreitam | Displicente clássico | Cafuné sobe Sanidade +5 (sem penalidade) |
| **Gambás** | Sítio do Capim, Polo Monte Verde (noturnos) | Noturnos, vasculham lixo, assustam | Susto cômico ("ECA, GAMBÁ!") | Aproximação: correção;item "Lata Velha" assusta |
| **Cavalos de carroça** | Sítio do Capim | Puxam carroças dos NPC; pacíficos | Rústico, saudoso | Alimentar (cenoura) sobe Reputação com cavaleiro +5; atropelamento (raro, evitável) gera penalidade forte (Sanidade −10, Reputação −20 com Sítio) |

### 4.2 Interações Detalhadas e Impacto em Atributos

| Interação | Animal | Custo | Efeito |
|---|---|---|---|
| **Alimentar** | Pombo | 1× Fome item (milho R$ 2) | Sanidade +2; bando segue por 30s (distrai PM leve) |
| **Alimentar** | Cachorro | 1× Comida (R$ 5) | Sanidade +5; cachorro segue por 5min (mini-companhia) |
| **Adotar** | Cachorro (Reputação +30 bairro) | R$ 100 (coleira) | Cachorro vira pet em safehouse; Sanidade +5/dia ao interagir |
| **Cafuné** | Gato | 5s de animação | Sanidade +5 (1 vez por gato a cada 30min) |
| **Aproximar** | Gambá | — | 50% chance de "spray" (visão turva 5s, Sanidade −2) |
| **Evitar** | Gambá | — | Sem efeito (comportamento passivo se ignorado) |
| **Alimentar** | Cavalo de carroça | Cenoura (R$ 3) | Reputação +5 com cavaleiro; cavalo relincha (Sanidade +2) |
| **Espantar** | Pombo | Energia −5 | Pombos voam em bando (curto cinematográfico; pode distrair PM) |
| **Atropelar (acidente)** | Qualquer | — | Sanidade −10 (NPCs reagem mal); não há morte visível |

### 4.3 Eventos Especiais Envolvendo Animais

- **E-027 "Bando de Pombos Agressivos"** — Praça do Centro; bando "ataca" jogador se passar muito rápido. (Cômico; Sanidade −2 se não escapar.)
- **E-031 "Cachorro Perdido"** — Vista Alegre; jogador pode devolver a dono (Reputação +15).
- **E-038 "Gambá na Van do Tonho"** — Tonho pede ajuda; mini-game de captura.
- **E-042 "Cavalo Solto na Rodovia"** — Otacílio pede ajuda; tráfego trava.

---

## 5. Autoridades

As autoridades são sistemas vivos que reagem ao **Nível de Procurado (0–5 estrelas)**, à **Reputação faccional** e ao **Caos global**. Cada uma tem gatilho, nível de ameaça e contramedidas próprias.

### 5.1 Tabela Geral de Autoridades

| Autoridade | Símbolo | Nível de Ameaça | Função | Comportamento |
|---|---|---|---|---|
| **Polícia Militar (PM)** | Farda azul-trepante | Alta (perseguição armada cartoon) | Responder a crime violento, racha, fuga | Patrulha, persegue, prende por estrelas |
| **Polícia Civil (Bira)** | Camisa social + coldre | Média (investigativa) | Investigar, missões políticas, propina | Aparece em cutscenes e missões; raramente patrulha |
| **Guarda Municipal** | Farda verde | Baixa (multas) | Trânsito, ordem urbana, pequenas infrações | Multa por velocidade, estacionamento, barulho |
| **Fiscal da Prefeitura** | Crachá e prancheta | Nenhum (econômico) | Cobrar taxas, embargar comércio informal | Abordagem em barracas, mercado paralelo |
| **Político (Helena / Frente Popular)** | Bandeira verde-e-amarela | Variável (político) | Manipular mídia, contratar/ameaçar | Apresente em eventos, contrata serviços |

### 5.2 Polícia Militar (PM)

| Campo | Detalhe |
|---|---|
| **Função** | Resposta direta ao crime; equivalente GTA-style |
| **Nível de ameaça** | Alto (a partir de 2 estrelas) |
| **Gatilho** | Crime violento (agressão, roubo visível), fuga, direção perigosa prolongada |
| **Comportamento** | Patrulha em viaturas (Camburão) e a pé; persegue em motos e helicóptero a partir de 4 estrelas |
| **Como reagir** | Fugir fora de visão por 30s; entrar em safehouse (Vista Alegre +30 com Zé Pequeno, paga suborno a Bira); usar Paint de carro |
| **Como evitar** | Não cometer crime perto deles; usar bandana; árvore Crime → "Fantasma" |

**Escalada de Estrelas**

| Estrelas | Resposta |
|---|---|
| 0 | Sem perseguição |
| 1 | 1 viatura busca área; pode ser evitada sair de visão |
| 2 | 2 viaturas + PM a pé; persegue a pé |
| 3 | Motos + bloqueios em rotas principais |
| 4 | Helicóptero + tiros cartoon (sem sangue); estrategicamente posicionam |
| 5 | Caçada total; Bira convocado (intervenção civil) |

### 5.3 Polícia Civil (Delegado Bira)

Detalhado em §2.2. Em termos sistêmicos, Bira **reduz ou aumenta a tolerância da PM** no Centro conforme Reputação.

### 5.4 Guarda Municipal

| Campo | Detalhe |
|---|---|
| **Função** | Infrações leves de trânsito e convivência |
| **Nível de ameaça** | Baixo (multa econômica) |
| **Gatilho** | Excesso de velocidade urbana (>20 km/h do limite), estacionar em vaga proibida, barulho (Seu Tobias), dirigir sem capacete (moto) |
| **Comportamento** | A pé ou em pequenas viaturas brancas; persegue apenas por multa |
| **Como reagir** | Pagar multa (R$ 80–300); ou ignorar (gera +1 estrela PM se recusiva) |
| **Como evitar** | Respeitar limites; árvore Direção → "Leitura de Fluxo" (antecipa) |

### 5.5 Fiscal da Prefeitura

| Campo | Detalhe |
|---|---|
| **Função** | Embargo de comércio informal |
| **Nível de ameaça** | Nenhum (apenas econômico) |
| **Gatilho** | Vender itens na rua sem alvará; ter "barraca" informal ativa |
| **Comportamento** | Aborda o jogador, aplica multa e/ou confisca mercadoria; pode ser subornado |
| **Como reagir** | Subornar (R$ 50–200) ou aceitar embargo |
| **Como evitar** | Não vender em rua; usar rede do Tonho (árvore Crime → "Embalagem Fria") |

### 5.6 Político (Helena Velasco e Assessores)

| Campo | Detalhe |
|---|---|
| **Função** | Manipulação midiática e burocrática |
| **Nível de ameaça** | Variável (alto em narrative, baixo em combate) |
| **Gatilho** | Envolvir-se em política; Reputação alta/baixa com Frente Popular |
| **Comportamento** | Apresente em eventos, contrata serviços, paga propina; usa mídia contra inimigos |
| **Como reagir** | Alinhar-se (missão M-024+), excluir (não interagir), ou denunciar (M-053) |
| **Como evitar** | Manter Reputação Frente Popular neutra (0) e evitar Centro Histórico em horário político |

### 5.7 Consequências Cruzadas

| Autoridade | Pago suborno | Negocia | Foge | Confronta |
|---|---|---|---|---|
| **PM** | Limpa estrelas (via Bira, R$ 500–5.000) | Não disponível | Funciona se +5s escondido | Sobe Caos +5 |
| **Bira** | Reduz estrela e abre missão | Sim, em DP | Indireto | Reputação Frente −10 |
| **Guarda Municipal** | R$ 50–200 cancela multa | Sim | +1 estrela PM | Não recomendado |
| **Fiscal** | R$ 50–200, confisca evitado | Sim | Embargo | Sobe Caos +1 |
| **Político** | Vira suborno reverso (você paga para apoiar) | Múltiplas opções | Reputação midiática | "Cancelamento" público |

---

## 6. Convenções de Design de Personagens

Para manter coerência em todos os documentos do GDD, todas as adições futuras de personagens devem seguir estas regras:

### 6.1 Nomenclatura

- **Nome + apelido** (ex.: "Caio 'Caique' Martins", "Tia Marlene").
- **Idade** sempre declarada (ajuda voice acting).
- **Facção alinhada** sempre explícita (ou "Independente").
- **Bairro-base** sempre citado.

### 6.2 Templates

- NPCs principais usam o template de 10 campos (Nome, Aparência, Personalidade, Função, Missões, Impacto, Relação, Interações, Consequências, Reações).
- Vizinhos usam o template de 5 campos (Nome, Descrição, Comportamento, Humor, Interações, Consequências) + tabela de comportamento aleatório.
- Animais e autoridades usam tabela tabular simples.

### 6.3 Voice e Tom

- Sátira afetuosa, não deboche cruel.
- Gírias válidas: "tá ligado", "mano", "minha nossa", "rapaz", "ih, rapaz".
- Evitar: gore, racismo, capacitismo, misoginia explícita.
- Diálogos têm três versões por gênero do protagonista (masculino / feminino / neutro).

### 6.4 Escala Numérica

- Atributos sempre **0–100**, exceto Reputação (**−100 a +100**).
-XP sempre número positivo inteiro.
- Dinheiro sempre **R$** (soft) ou **CC$** (hard premium).

### 6.5 Diversidade e Representação

- Distribuição étnica, de gênero e regional segue a demografia brasileira.
- Personagens com deficiência aparecem orgânicamente (ex.: Vó Ivone tem deficiência auditiva leve; um futuro NPC pode ter deficiência motora).
- Religiões afro-brasileiras (Candomblé, Umbanda) tratadas com respeito (Zé Pequeno, Dona Cida em diálogos cruzados).

---

*Próximo documento:* [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md) — detalha veículos, física, customização, garagens e o sistema de direção mobile-first usado por Seu Otacílio, Dr. Éverton e Tavinho.
