# 09 — Arte & Estilo Visual

> **Direção de Arte de *Cidade do Caos: Mundo Aberto*.** Este documento define a linguagem visual completa do jogo — da paleta global aos animais de estimação — e **deve** ser usado como referência por arte, tech art, UI/UX e QA visual.
>
> **Leia junto com:** [00-biblia-do-mundo.md](00-biblia-do-mundo.md) (fonte de verdade canônica) • [08-interface-telas.md](08-interface-telas.md) (aplicação da identidade na UI)
>
> **Tom visual resumido:** "Semi-realista vibrante cartoon" — estilizado, saturado, legível em telas pequenas, com pé no Brasil urbano. **Não** é fotorrealista, **não** é pixel art, **não** é low-poly PS1.

---

## Sumário

1. [Direção de Arte e Linguagem Visual](#1-direção-de-arte-e-linguagem-visual)
2. [Paleta de Cores](#2-paleta-de-cores)
3. [Estilo dos Personagens](#3-estilo-dos-personagens)
4. [Estilo dos Cenários](#4-estilo-dos-cenários)
5. [Estilo dos Veículos](#5-estilo-dos-veículos)
6. [Estilo dos Ícones](#6-estilo-dos-ícones)
7. [Animações Desejadas](#7-animações-desejadas)
8. [Efeitos Climáticos e Atmosféricos](#8-efeitos-climáticos-e-atmosféricos)
9. [Especificações Técnicas de Arte (resumo)](#9-especificações-técnicas-de-arte-resumo)

---

## 1. Direção de Arte e Linguagem Visual

### 1.1 Pilares Visuais

A identidade visual de *Cidade do Caos* nasce de uma triagem cruel de prioridades: **mobile primeiro**, **legibilidade acima de fidelidade**, **humor acima de escuridão**. Cada decisão estética precisa passar por três filtros:

| # | Pilar | O que significa na prática |
|---|---|---|
| **V1** | **Vibrante, não fotorrealista** | Cores saturadas, formas esculpidas, sombras deliberadas. A realidade é ponto de partida, não destino. |
| **V2** | **Legível em 5 polegadas** | Silhuetas únicas, valores de cinza contrastantes, ping-pong visual entre jogador, inimigos e cenário. |
| **V3** | **Brasileiro sem estereótipo barato** | Tipografia popular, paleta de moeda, materiais vernaculares (azulejo, concreto queimado, lata ondulada). |
| **V4** | **Humor no design de forma** | Proporções levemente exageradas, expressões amplas, gestos largos. NPC nunca é "genérico urbano". |
| **V5** | **Otimizado para intermediários** | 60 fps em Snapdragon 660 / A12; queda graciosa para low-end via LOD, fog e texture budget. |

### 1.2 Estética Geral — "Semi-realista Vibrante Cartoon"

Definimos o ponto exato do espectro com referências cruzadas:

| Eixo | Não é… | É… | Inspiração declarada |
|---|---|---|---|
| **Silhueta** | Anime/chibi | Proporção **7–8 cabeças**, levemente estilizada | *Spider-Man: Into the Spider-Verse* (volume) / *Fortnite* (sapateado) / *GTA V* (movimento urbano) |
| **Cor** | Dessaturada cinematográfica | Saturação média-alta, contraste controlado | *Spider-Verse*, *Arcane* (em escala reduzida), cartazes de cinema nacional |
| **Material** | PBR pura | **PBR estilizado + cel-shading leve** (rim light + 2-bands de sombra) | *Zelda: BotW* / *Genshin* (em intensidade menor) / *Spider-Verse* |
| **Luz** | Ray-traced realista | Real-time direcional + SSAO leve + contact shadows | *GTA V* mobile-port / *PUBG Mobile* |
| **Tratamento** | Hyper-realismo granular | **Superfícies limpas, bordas definidas, texturas de leitura rápida** | *Team Fortress 2* (legibilidade de classe) / *Overwatch* (silhueta) |

### 1.3 Por que NÃO fotorrealismo em mobile?

A escolha contra fotorrealismo não é estética — é estratégica. As razões:

1. **Custo de produção inviável.** Scan e photogrammetry exigiriam 4–6x o budget de arte para cobrir 9 km² de cidade. Estilização permite **reutilização modular** e atalhos de leitura (silhueta > textura).
2. **Performance em intermediários.** Fotorrealismo pede texturas 4K, PBR completo, SSR, GTAO. Em Snapdragon 660 isso vira 18 fps. Estilização troca textura por **shader math** (mais barato em GPU mobile do que amostragem de textura pesada).
3. **Envelhecimento mais lento.** Fotorrealismo envelhece em 2 anos; estilizado vibrante dura 8–10 anos (caso *Wind Waker*, *TF2*).
4. **Legibilidade em tela pequena.** Em 5,5", detalhes realistas viram ruído. Silhueta + cor + luz definida lêem em 0,3s — velocidade de decisão do jogador mobile.
5. **Identidade de marca.** Fotorrealismo é commodity; **semi-realista vibrante brasileiro** é um espaço não ocupado no mercado mobile global.
6. **Tom do jogo.** Humor e cartoon combinam; sangue fotorrealista quebraria a classificação 14+.

### 1.4 Pipeline de Renderização (Unity 6 / URP)

| Estágio | Decisão | Por quê |
|---|---|---|
| **Render pipeline** | **URP** (Universal Render Pipeline) | Mais leve que HDRP; suporte amplo a Android/iOS; shader graph amigável. |
| **Shaders base** | Shader Graph custom: `StylizedPBR`, `StylizedCharacter`, `CelFoliage`, `GlassStylized`, `WetAsphalt` | Reutilizável, sem custo de asset store, otimizado para mobile. |
| **Iluminação** | **1 directional light (sun)** + **baked GI** (Enlighten ou GPU Lightmapper) + **realtime shadow mask** | Sombras de sol dinâmicas no jogador e veículos; cenário usa baked. |
| **Sombras** | **2 cascades** (até 30 m / 60 m), resolução 1024×1024 total | Equilíbrio mobile entre nitidez e fill-rate. |
| **Pós-processamento** | Color ACES tonemap / Bloom (suave) / Vignette 0.18 / Chromatic Aberration 0.05 (off em low-end) / SSAO (alto+normais, off em low) | Volume sutil — cartunesco, não cinematográfico pesado. |
| **Anti-aliasing** | **FXAA** (mobile), MSAA 2× em high-end | FXAA mais barato em bandwidth. |
| **Color space** | **Linear** | Iluminação correta e sem gradients sujos. |
| **MSAA / HDR** | HDR ligado apenas no skybox; LDR no resto | Evita banding em céu sem explodir bandwidth. |
| **Cel-shading** | Bandas de sombra custom via shader (não toon shader puro) — **2 bandas + rim light + AO baked** | Volume sem perder o "sólido iluminado". |

### 1.5 Referências Visuais (mood board textual)

| Referência | O que pegamos | O que NÃO pegamos |
|---|---|---|
| *Spider-Man: Into the Spider-Verse* | Rim light saturado, sombras bandeadas, poses largas | Chromatic aberration extrema, frame-rate variable |
| *Arcane* (painterly em cenas) | Volume via cor | Complexidade de partículas (orçamento mobile) |
| *Fortnite* | Sapateado animado, customização ousada | Cartoon escuro / violência cômica exagerada |
| *GTA V* (modo FPP/TPP) | Densidade urbana, movimento de câmera,ções | Fotorrealismo, densidade de NPCs inatingível em mobile |
| *Bully* | Conveniência de "escola de vida" / NPCs com humor | — |
| *Zelda: Breath of the Wild* | Vegetação estilizada, distância com atmosfera | Tonalidade watercolor |
| *Sábado à tarde em São Paulo* (mood) | Concreto sujo, letreiros desencontrados, calor | — |
| *Cidade de Deus* (cénario, não violência) | Paleta de favela, textos de parede,_LAYOUT becos | Tom sombrio |

### 1.6 Princípios de Composição de Cena

- **Regra dos três planos:** foreground (jogador e interativos), midground (cenário jogável), background (skybox/cartão-postal). Cores mais saturadas no foreground, mais frias/azuladas no fundo (atmospheric perspective).
- **Hierarquia de leitura:** jogador (mais saturado e iluminado) > veículo atual > NPCs/veículos ativos > cenário > NPCs distantes (dessaturados via LOD shader).
- **Frame dentro do frame:** ruas e becos funcionam como *leading lines* para marcos (Cristo Leigo, Viaduto do Génio, prédios do Centro).
- **Simetria quebrada proposital:** cidades brasileiras reais têm assimetria (fios, antenas, pichações, extensões irregulares). Replicamos com módulos mas sempre com **ruído humano** sobreposto (varais, antenas, tênis no fio).

---

## 2. Paleta de Cores

### 2.1 Paleta Global — "Sol, Concreto e Caos"

A paleta global é o DNA cromático. Toda cor do jogo deriva destes 12 swatches base. Saturação média-alta, valores otimizados para sRGB (gamut Android/iOS comum).

| Token | Hex | Uso | Notas |
|---|---|---|---|
| `GENESIS_SUN` | `#FFC857` | Sol, lanches, calor, ouro "falso" | Quente, amarelo-mel; evita amarelo ácido |
| `GENESIS_ASPHALT` | `#2B2E33` | Asfalto base, sombras urbanas | Quase-preto azulado |
| `GENESIS_CONCRETE` | `#A8A39A` | Concreto, cal, paredes neutras | Cinza quente (areia) |
| `GENESIS_SKY_DAY` | `#7EC8E3` | Céu de dia claro | Azul céu, saturação média |
| `GENESIS_SKY_DUSK` | `#F26B5E` | Pôr do sol Itaúna, tempestade chegando | Coral-ferrugem |
| `GENESIS_GRASS` | `#6BAB53` | Grama, Sítio do Capim, praças | Verde-mate, não neon |
| `GENESIS_RED_CLAY` | `#B05B3B` | Terra vermelha, telha, ferrugem | Cor do Brasil |
| `GENESIS_GRAFFITI_PINK` | `#FF4D8D` | Grafite, letreiro funky, destaque cômico | Magenta quente |
| `GENESIS_DANGER` | `#E63946` | Vida baixa, polícia, alerta | Vermelho sinais de trânsito |
| `GENESIS_MONEY` | `#3FA66B` | R$, sucesso, transação | Verde-moeda |
| `GENESIS_KAOS_GOLD` | `#F2C14E` | CaosCash (premium), lendário | Dourado quente |
| `GENESIS_NIGHT_BLUE` | `#1B2845` | Noite urbana, sombra de prédio | Azul-noite, não roxo |

> **Regra 60-30-10 por cena:** 60% tons neutros (asfalto, concreto, céu), 30% cor de bairro (próxima seção), 10% cor de destaque (facção/jogador/evento).

### 2.2 Paleta por Bairro

Cada bairro tem **3 cores primárias** (material, sombra, acento) e **2 cores secundárias** (detalhe, sinalização). Juntas, geram leitura instantânea: jogador sabe onde está mesmo sem minimapa.

#### 2.2.1 Vista Alegre (favela)

Conceito: **calor humano sobre concreto apertado**. Tons quentes, grafite saturado, contraste alto entre sombra de beco e sol de laje.

| Token | Hex | Uso |
|---|---|---|
| `VISTA_LAJE` | `#B8B0A4` | Laje exposta, parede crua |
| `VISTA_TIJOLO` | `#A0533A` | Tijolo à vista, telha cerâmica |
| `VISTA_GRAFFITI` | `#FF4D8D` | Grafite rosa-magenta (identidade) |
| `VISTA_LATAO` | `#D98C3F` | Zinco/lata ondulada, ferrugem clara |
| `VISTA_SOMBRA` | `#3A2D2A` | Beco, sombra apertada |
| `VISTA_CABLE` | `#1A1A1A` | Fios, postes improvisados |

#### 2.2.2 Centro Histórico

Conceito: **çoimento, comércio caótico, cinza com letreiros coloridos**. Base cinza-chumbo, acento puro dos letreiros de loja.

| Token | Hex | Uso |
|---|---|---|
| `CENTRO_PEDRA` | `#7D7468` | Pedra antiga, marquise |
| `CENTRO_GRAFITAO` | `#4A4540` | Predio sujo, fuligem |
| `CENTRO_LETREIRO` | `#F2421B` | Letreiro vermelho (lojas, bancos) |
| `CENTRO_NEON_VERDE` | `#3DBB6A` | Neon verde farmácia/sorveteria |
| `CENTRO_NEON_AZUL` | `#1E90D6` | Letreiro banco/caixa |
| `CENTRO_OCRE` | `#C68B5B` | Ocre histórico, restored façade |

#### 2.2.3 Polo Monte Verde (industrial)

Conceito: **metal, concreto armado, ferrugem**. Frios e quentes em atrito, textura pesada.

| Token | Hex | Uso |
|---|---|---|
| `MONTE_METAL` | `#5C6470` | Metal estrutural, galpão |
| `MONTE_CONCRETO` | `#8B8680` | Concreto armado cru |
| `MONTE_FERRUGEM` | `#8A4A2A` | Ferrugem, óxido |
| `MONTE_AMARELO_IND` | `#F2C200` | Faixas industriais amarelas (perigo) |
| `MONTE_BETUMEN` | `#2A2520` | Pátio asfáltico, óleo |
| `MONTE_GUINDASTE` | `#E6692F` | Laranja de equipamento |

#### 2.2.4 Sítio do Capim (rural/periferia)

Conceito: **terra, verde, casa simples**. Tons terrosos, vegetação mais presente que em qualquer outro bairro.

| Token | Hex | Uso |
|---|---|---|
| `CAPIM_TERRA` | `#B05B3B` | Estrada de terra vermelha |
| `CAPIM_GRAMA` | `#7BAE52` | Capim, mato, pasto |
| `CAPIM_CASA` | `#E3D5B8` | Parede caiação, casa simples |
| `CAPIM_MADEIRA` | `#9A6B3F` | Madeira, ripado, porta velha |
| `CAPIM_FOLHAGEM_DENSA` | `#3F6B3A` | Verde escuro, mangue |
| `CAPIM_CERAMICA` | `#C24E2E` | Telha cerâmica |

#### 2.2.5 Jardim Belvedere (nobre)

Conceito: **clean, vidro, jardim cuidado**. Frios premium, contraste baixo (sereno), vegetação controlada.

| Token | Hex | Uso |
|---|---|---|
| `BELVEDERE_VIDRO` | `#9FC4D9` | Vidro espelhado, janela |
| `BELVEDERE_GRANITO` | `#C9CDD1` | Granito, mármore limpo |
| `BELVEDERE_JARDIM` | `#4F8C5C` | Grama colonial bem cuidada |
| `BELVEDERE_METAL_POSH` | `#2C3E50` | Ferro preto, esquadria premium |
| `BELVEDERE_DOURADO` | `#D4AF37` | Detalhe dourado (portaria) |
| `BELVEDERE_BRANCO` | `#F4F1EA` | Fachada off-white |

#### 2.2.6 Praia de Itaúna (orla)

Conceito: **areia, azul, quiosques coloridos**. Saturação alta no sol, contraste forte entre céu e areia.

| Token | Hex | Uso |
|---|---|---|
| `ITAUNA_AREIA` | `#E8C98C` | Areia clara |
| `ITAUNA_MAR` | `#2E9BC9` | Mar raso |
| `ITAUNA_MAR_FUNDO` | `#1F5B7E` | Mar fundo/horizonte |
| `ITAUNA_QUIOSQUE` | `#FF6B5E` | Quiosque coral/laranja |
| `ITAUNA_VERDE_PRACA` | `#7BBF6A` | Coqueiro, grama costeira |
| `ITAUNA_BRANCO_SOL` | `#FFF7E8` | Areia clara quase branca |

### 2.3 Cores das Facções

Cada facção tem **cor primária + secundária** que aparecem em: roupas de membros, veículos, bandeiras/grafite de território, ícones de missão,HUD de reputação.

| Facção | Primária | Hex | Secundária | Hex | Uso visual típico |
|---|---|---|---|---|---|
| **União dos Caminhoneiros do Caos** | Azul-Tempestade | `#2E5A88` | Cromado/Prata | `#BFC5CC` | Caminhões azuis com cromado; bandana azul |
| **Milícia Escudo** | Preto | `#0F0F12` | Dourado envelhecido | `#C39B3D` | Terno/preto com bolsas douradas; escudo dourado |
| **Motoclube Cavaleiros do Asfalto** | Vermelho-Sangue | `#8B1A1A` | Osso | `#E8DCC2` | Couro vermelho, caveira no capacete |
| **Frente Popular de São Genésio** | Verde-Bandeira | `#1F7A2F` | Amarelo-Bandeira | `#FFD300` | Camiseta verde, estrutura amarela, sanduíche de voto |

> **Regra de conflito:** vermelho-sangue (Motoclube) e azul-tempestade (Caminhoneiros) nunca aparecem na mesma cena saturados sem um elemento neutro entre eles — evoca disputa territorial de forma subliminar.

### 2.4 Cores de UI/HUD

A UI herda a paleta global mas tem sua própria sub-paleta para estados (default, hover, success, warning, danger). **Contraste mínimo 4.5:1** para textos (WCAG AA) — crítico em mobile com sol forte.

| Token | Hex | Uso | Contraste sobre `#FFFFFF` |
|---|---|---|---|
| `UI_BG_DARK` | `#1B1D22` | Painel principal, fundo HUD | 14.2:1 |
| `UI_BG_PANEL` | `#2A2D34` | Sub-painel, modal | 10.5:1 |
| `UI_TEXT_PRI` | `#FFFFFF` | Texto primário | 21:1 |
| `UI_TEXT_SEC` | `#B6B9C2` | Texto secundário | 8.4:1 |
| `UI_ACCENT` | `#FFC857` | Botão primário, destaque | 1.8:1 (apenas sobre fundo escuro) |
| `UI_SUCCESS` | `#3FA66B` | R$ ganho, missão ok | 2.9:1 (sobre fundo escuro 6.8:1) |
| `UI_WARNING` | `#E6A23C` | Energia baixa, multa | 2.2:1 (sobre fundo escuro 5.6:1) |
| `UI_DANGER` | `#E63946` | Vida baixa, perigo | 3.7:1 (sobre fundo escuro 5.1:1) |
| `UI_PREMIUM` | `#F2C14E` | CaosCash, lendário, season | 1.9:1 (sobre fundo escuro 6.1:1) |
| `UI_FACTION_NEUTRAL` | `#8E9AAF` | Reputação 0 | 3.0:1 (sobre fundo escuro) |

> **Teste de legibilidade mobile:** todos os textos críticos (saldo, vida, munição, marcador de missão) são **testados em simulador de luz solar** (200 nits sobre tela a 350 nits). Se ilegíveis, ou sobe-se o tamanho da fonte ou adiciona-se *drop shadow* preto de 1 px.

### 2.5 Disciplina de Contraste e Hierarquia

- **Three-tier contrast:** foreground saturado (jogador, facções), midground mid-saturado (cenário jogável), background dessaturado+azulado (atmosfera).
- **Saturation budget:** numa cena, **no máximo 3 elementos altamente saturados simultaneamente**. Mais que isso = ruído visual = cansaço mobile.
- **Color blind safe:** paleta testada com simuladores de protanopia, deuteranopia e tritanopia. Facções usam também **símbolo** (cavalo, escudo, caveira, estrela), não só cor, para identificação.

---

## 3. Estilo dos Personagens

### 3.1 Linguagem Anatômica

| Aspecto | Decisão | Justificativa |
|---|---|---|
| **Escala de proporção** | **7,5 cabeças** (realista é 7,5–8; super-herói 8,5; chibi 3–4) | Brasileiro médio estilizado sem perder o "humano" |
| **Silhueta** | Ombros largos, cintura estreita, pernas ágeis; mãos e pés **+15% maiores** | Léem-se melhor em tela pequena; ajuda a leitura de ação |
| **Rosto** | Olhos grandes, nariz definido, boca expressiva; **sem features hiper-realistas** | Emociona em close sem precisar de subsurface scattering pesado |
| **Cabelo** | Volumoso, modelado em *hair cards* ou bushy alpha-tested; **estilos afro, crespo, liso, cacheado** obrigatórios | Diversidade brasileira é pilar |
| **Mãos** | 4 dedos + polegar modelados separadamente (sem luva colada) | Animação de gesto (apontar, pegar, pagar) |
| **Pele** | 8 tons base canônicos (Nude Scale BR) | Representatividade racial brasileira |

#### Escala de Pele (Nude Scale BR)

| Token | Hex | Referência aproximada |
|---|---|---|
| `SKIN_01` | `#F4D7B3` | Pele muito clara |
| `SKIN_02` | `#E8B98E` | Clara |
| `SKIN_03` | `#D39E70` | Morena clara |
| `SKIN_04` | `#B5805A` | Morena |
| `SKIN_05` | `#9A6543` | Parda |
| `SKIN_06` | `#7A4A30` | Escura |
| `SKIN_07` | `#5A3324` | Muito escura |
| `SKIN_08` | `#3F2618` | Negra |

### 3.2 Customização do Protagonista

O protagonista é **100% customizável** ([00-biblia-do-mundo.md](00-biblia-do-mundo.md) §5). A base técnica: um **mesh neutro** + sistema de **slots cosmeticos** (sem alterar geometria do corpo).

| Slot | Categorias | Variações (MVP) | Variações (final) |
|---|---|---:|---:|
| **Cabelo** | Curto / Médio / Longo / Cacheado / Afro / Trançado / Careca | 12 | 40+ |
| **Barba/Bigode** (masculino) | Não há barba/média/bigode/cavaleiro | 6 | 18 |
| **Rosto** | Formato (5), nariz (5), olhos (4), boca (4), sobrancelha (4) | 5 faces base | 20 faces base |
| **Pele** | 8 tons canônicos | 8 | 8 |
| **Camisa/Camiseta** | Básica / Estampada / Social / Esporte / Casual | 12 | 50+ |
| **Calça/Short** | Jeans / Moleton / Social / Cargo / Short / Saias | 8 | 30+ |
| **Calçado** | Chinelo / Tênis / Botina / Sandália / Salto | 8 | 25+ |
| **Acessórios de cabeça** | Boné / Chapéu / Bandana / Óculos / Bucket | 10 | 35+ |
| **Acessórios de corpo** | Relógio / Colar / Pulseira / Mochila / Bandana braço | 8 | 25+ |
| **Tatuagens** | (destraváveis) Braço / Perna / Costa / Pescoço | 4 | 20+ |

> Cada peça é um prefab com material swap; tatuagens usam **decal projection** sobre o shader de pele.

### 3.3 Tipos de NPCs (Brasil regional e de classe)

São **5 grandes arquivos** de NPC, cada um com variação de roupa/cor/idade/gênero — total **~180 NPCs únicos** visualmente. Cada tipo tem **idle anim própria** e **voiceline pool** regional.

| Arquétipo | Biótipo visual | Onde aparece | Itens de cena |
|---|---|---|---|
| **Tiozão do boteco** | Pança, camiseta regata, chinelo, bigode | Vista, Centro, Capim | Latinha, toalha no ombro |
| **Tia do pastel** | Vestido florido, touca, avental | Vista, Centro, Itaúna | Caixa térmica, plateia em volta |
| **Motoboy** | Capacete, mochila térmica, jaqueta refletiva | Centro, Belvedere, Itaúna | Mochila quadrada, celular na mão |
| **Caminhoneiro** | Boné, camisa xadrez, bigode, bota | Monte Verde | Garrafa térmica, cigarro |
| **Executivo apressado** | Terno/camisa social, maleta, fones | Centro, Belvedere | Café, celular |
| **Estudante universitário** | Mochila, boné, camiseta de banda, tênis | Centro, Belvedere | Livro, fone |
| **Vovó benzedeira** | Vestido escuro, xaile, óculos, sapato velho | Vista, Capim | Bolsa, terço |
| **Vendedor ambulante** | Bandana, sacola de produtos, kit produto | Semáforos do Centro | Bandeja, sacolas |
| **Funkguerinho / funkeira** | Boné, corrente dourada, camiseta grande, tênis branco | Vista, Itaúna | Celular com caixa de som |
| **Cuidadora de idoso** | Uniforme rosa/azul, cabelo preso | Belvedere, Centro | Carrinho de feira |
| **Policial (PM)** | Farda azul-marinho, colete, capacete balístico | Todos | Cassetete, rádio |
| **Guarda municipal** | Farda cinza-azul, colete refletivo | Todos | Multímetro, prancheta |
| **Surfstas** | Cabelo descolorido, bermuda, chinelo, prancha | Itaúna | Prancha, cerveja |
| **Pescador** | Boné sujo, camiseta de time, calça remendada | Itaúna, Capim | Caixa de isca, rede |
| **Capiau / matuto** | Chapéu de palha, camisa xadrez, bota | Capim | Cigarro palha, faca na cinta |
| **Influenciadora** | Roupa fashion, celular frontal, óculos grandes | Belvedere, Itaúna | Tripé, ring light |
| **Político / vereador** | Terno bege, lenço, sorriso amplo | Centro, Belvedere | Sanduíche de voto, bandeira |
| **Catador de reciclável** | Roupa gasta, luvas, carrinho de feira cheio | Centro, Monte Verde | Carrinho, sacos |
| **Religiosa de igreja** | Saia longa, blusa goma, Bible na mão | Todos | Bíblia, sacola |
| **Militar de Milícia** | Preto, colete tático dourado/escuro, gola alta | Centro, Vista | Rádio, brasão Escudo |

> Cada NPC tem **3 a 5 variações de cor** e **2 a 4 de material** — total de leituras visuais distintas ~800. Comportamento e diálogos detalhados em [03-personagens.md](03-personagens.md).

### 3.4 Rig e Esqueleto

- **Esqueleto base unificado:** **Humanoid** (Unity Mecanim), 68 ossos. Permite retargeting universal de anim entre personagens.
- **Bones extra:** 3 bones por cabelo longo (physics via Dynamics), 2 por barba grande, 5 faciais (sobrancelha 2, boca 2, mandíbula 1), 4 por capa/roupa solta.
- **Facial rig:** **blendshapes** (52 ARKit + 8 custom brasileiras: sorriso de canto,_raiva_olímpica, tédio de ônibus, cara de "e agora?").
- **Root motion:** desligado para locomoção (movimento por velocity) e ligado para *finishers* e emotes.
- **Layered animation:** base + upper body + facial + IK legs (pés no chão em inclinados) + additive (respiração, tique).

### 3.5 Polimento de Animação de Personagem

- **Exagero de 12%:** movimento real é "flat" no mobile; alongamos poses-chave em 12% (princípio de animação Disney #6).
- **Hand pose library:** 20 poses de mão pré-feridas (mão aberta, punho, peace, pegar, pagar, apontar, moto guidão, volante).
- **Breathing layer:** todo NPC tem respiração aditiva em idle (5–8% de escala), Nunca está 100% estático.

---

## 4. Estilo dos Cenários

### 4.1 Filosofia Modular

O mundo é **9 km²** jogáveis ([00-biblia-do-mundo.md](00-biblia-do-mundo.md) §2). Para caber em ~1,2 GB de APK com download por bairro (Addressables), o cenário é construído em **kits modulares** — não em meshes únicos.

| Conceito | Decisão |
|---|---|
| **Grid base** | Snap de 4m × 4m para calçadas/ruas; 8m para quadras; sub-grid de 1m para detalhes |
| **Módulos por bairro** | **8 kits** (6 bairros + 1 estrada/ponte + 1 vegetação genérica) |
| **Cada kit** | 60–120 meshes modulares, 40–80 materiais (com variantes de cor), 200–400 decals |
| **Modular building** | Blocos: parede 1m / 2m / 4m, janela, porta, viga, coluna, laje, telhado, beiral, marquise |
| **Vertex color** | Cada módulo tem vertex color (R=ao, G=grime, B=wetness, A=player) paravariation sem custo |
| **Texture atlas** | Materiais compartilham **4 atlas por bairro** (4096×4096 cada) — total draw calls baixo |
| **HLOD** | Geração de HLOD (Hierarchical LOD) por quadra no build; combine de meshes estáticos |

### 4.2 Vista Alegre (favela) — Cenário

| Aspecto | Decisão |
|---|---|
| **Materiais** | Laje (concreto cru com vertex color verde de mofo), tijolo aparente, lata ondulada (metal shader com rust gradient), madeira de demolição, PVC colorido (cabeamento irregular) |
| **Layout** | Becos de **1,5 a 3 m** de largura, escadarias íngremes, becos sem saída, ruelas labirínticas |
| **Sinalização** | Grafite **3 camadas**: base (pichação grossa), meio (graffiti elaborado, pichação política local), topo (cartaz de funk, placa de "não mexa") |
| **Vegetação** | Plantas em vasos improvisados (garrafa PET, pneu), bananeira em quintal, hera subindo parede |
| **Iluminação** | Sombra profunda de beco (atmosphere volume azul-noite) + sol pulverizado em lajes (warm rim) |
| **Marcos** | Quadra socior cultural (ilha de cores), laje do Zé Pequeno (ponto de missão), beco do amor (túnel de bandeiras) |
| **Skyline** | Antenas improvisadas, varais, tênis no fio, pichação no alto ("VISTA ALEGRE FIEL") |

### 4.3 Centro Histórico

| Aspecto | Decisão |
|---|---|
| **Materiais** | Pedra calcária (çoimento), granito polido, mármore desgastado, vidro antigo, metal envelhecido, azulejo histórico |
| **Layout** | Ruas retas **6–10 m**, calçadas largas com fluxo de pedestre intenso, quadras quadradas com praças |
| **Sinalização** | Letreiros comerciais grandes (plano de fundo saturado): **"LOJA DO ZÉ"**, **"FARMÁCIA POPULAR"**, **"MERCADO CENTRAL"**; letreiro de banco neon azul; lojas com toldo listrado |
| **Vegetação** | Palmeira imperial na praça, mangueira centenária, ipê amarelo |
| **Iluminação** | Sombra dura de marquise; postes de vapor de sódio ao anoitecer (laranja) |
| **Marcos** | Viaduto do Génio (inspirado no Santa Ifigênia), Praça da Sé fictícia (ponto de encontro), Prédio Banespa-like (skyline), Catedral Metropolitana estilizada |
| **Skyline** | Aglomerado de letreiros neon, antenas de TV, caixas d'água, torres de igreja |

### 4.4 Polo Monte Verde (industrial)

| Aspecto | Decisão |
|---|---|
| **Materiais** | Metal corrugado, concreto armado cru, asfalto pesado com óleo, ferrugem volumétrica, vidro sujo de escritório |
| **Layout** | Avenidas largas (15 m) para caminhões, galpões retangulares, pátios enormes com contêineres |
| **Sinalização** | Faixas amarelo-preto industriais, placa de "PERIGO" estilizada, letreiro de "FÁBRICA" sobreposto em metal |
| **Vegetação** | Mato crescendo entre rachaduras de concreto, mangue ressecado nas bordas |
| **Iluminação** | Sol cru sem marquise (área aberta), luz industrial amarela, vultos no vapor à noite |
| **Marcos** | Chaminé extinta (skyline), viaduto ferroviário abandonado, fábrica de cimento, portão da Milícia Escudo |
| **Skyline** | Guindastes, torres de resfriamento, silos, fumaça esbranquiçada |

### 4.5 Sítio do Capim (rural/periferia)

| Aspecto | Decisão |
|---|---|
| **Materiais** | Terra vermelha (shader especial com wetness volume), madeira velha, telha cerâmica, muro de tijolo cru, alambrado |
| **Layout** | Estradas de terra largas sem calçada, chácaras espalhadas, manguezal nas bordas |
| **Sinalização** | Placa de "VENDE-SE", faixa de "FESTA JUNINA", pichação esparsa, placa política antiga |
| **Vegetação** | Capim alto, bananeira, coqueiro, palmito, mato cerrado, mangue |
| **Iluminação** | Sombra longa de fim de tarde, sol quente, sem sombra artificial |
| **Marcos** | Quitanda da Dona Cleide, ponte de madeira sobre riacho, capela de santinho, campo de futebol de terra |
| **Skyline** | Serra ao longe, antena de rádio, pigeon house, párabolos |

### 4.6 Jardim Belvedere (nobre)

| Aspecto | Decisão |
|---|---|
| **Materiais** | Vidro espelhado, granito polido, mármore, jardim cuidado, ferro pintado preto |
| **Layout** | Ruas planas arborizadas, condomínios fechados, malls, rotatórias com canteiro |
| **Sinalização** | Placas limpas em metal escovado, letreiro de shopping, luminosos de café (clean typography) |
| **Vegetação** | Grama uniforme, palmeira imperial alinhada, topiaria, buxus aparado, ipê roxo |
| **Iluminação** | Luz difusa suave via árvores, lampejos de reflexo nos prédios, evening light quente |
| **Marcos** | Cristo Leigo (estátua em morro com vista panorâmica), Shopping Belvedere (mall com vidro espelhado), Praça Central com chafariz |
| **Skyline** | Torres de escritório (10–15 andares), prédios residenciais curvos, antenas stealth |

### 4.7 Praia de Itaúna

| Aspecto | Decisão |
|---|---|
| **Materiais** | Areia (shader especial com specularity wet/dry), madeira de deck, lona de quiosque colorida, vidro fosco de prédio à beira-mar |
| **Layout** | Orla longa com ciclovia, calçadão, quiosques alinhados, píer de madeira |
| **Sinalização** | Placas de "PROIBIDO MERGULHAR", letreiro de sorveteria, bandeiras de surfe, faixa de "TEMPORADA" |
| **Vegetação** | Coqueiro alinhado, restinga nativa, capim de praia |
| **Iluminação** | Sol frontal refletido no mar, golden hour intensa, sombras quentes |
| **Marcos** | Píer de madeira (ponto de missão de pesca), Cristo ao longe (visual), farol estilizado, quiosque do Tavinho (Motoclube) |
| **Skyline** | Serra ao fundo, prédios baixos à beira-mar, barcos à vela |

### 4.8 Marcos Globais (acessíveis de vários bairros)

| Marco | Bairro | Função visual/jogabilidade |
|---|---|---|
| **Cristo Leigo** | Jardim Belvedere (morro) | Marco skyline global; ponto de missão; mirante |
| **Viaduto do Génio** | Centro Histórico | Cruzamento de bairros; ponto de salto; cenário icônico |
| **Estação São Genésio** | Centro | Ponto de ligação metro/ônibus; movimento |
| **Rodoviária** | Borda Centro/Monte Verde | Saída de caminhões; missão de Caminhoneiros |
| **Ponte Estaiada** | Entre Centro e Itaúna | Estética principal; salto embaixo; pesca |
| **Morro da Vista** | Vista Alegre | Ponto alto visível de vários bairros |
| **Praia de Itaúna** | Itaúna | Cartão-postal; marco de orientação |

### 4.9 Sinalização Tipográfica do Mundo

| Tipo | Tipografia | Tratamento |
|---|---|---|
| **Letreiro comercial grande** | Display pintada à mão ou neon | Cores saturadas; homenagem a letreiros brasileiros antigos (Padaria, Bar, Mercadinho) |
| **Placa de trânsito** |tipográfica (similar a Highway Gothic) | Metal, refletiva; usada para orientação |
| **Pichação** | tipografia "pixação" São Paulo | Vertical, preto sobre concreto,Assinatura de facção |
| **Faixa de rua** | Display grossa | "FESTA JUNINA", "ELEGE TAL", "ALUGA-SE", "PARABÉNS VÓ" |
| **Outdoor** | Display | Política, produtos fictícios ("Cerveja Genésio", "Caldo de Cana Mineiro") |
| **Placa de comércio (loja)** | Variada (script, serif, sans) | Cada loja sua; mantém legibilidade |

### 4.10 Modulação de Densidade e LOD

| Distância (m) | LOD | Triangles máx | Estratégia |
|---|---|---|---|
| 0–10 | LOD0 | 30.000 | Material completo, tessellation off |
| 10–30 | LOD1 | 12.000 | Material completo, sem decals |
| 30–60 | LOD2 | 4.000 | Material atlas, sem decals |
| 60–120 | LOD3 | 1.200 | HLOD, 1 material por bairro |
| 120+ | Impostor | Billboard | Sprite 256×256 por ângulo |

---

## 5. Estilo dos Veículos

### 5.1 Linguagem Visual dos Veículos

~25 veículos brasileiros ficcionais, divididos em classes. Cada veículo tem **personalidade estética** — não são genéricos "carro vermelho".

| Princípio | Decisão |
|---|---|
| **Silhueta legível** | A 5,5" o jogador reconhece a classe pelo contorno: hatch baixo, SUV alto, moto estreita, caminhão comprido, ônibus volumoso |
| **Exagero de proporção +5%** | Capôs ligeiramente maiores, para-choques mais altos — leitura cartoon |
| **Cromados seletivos** | Apenas maçanetas, faróis, grade — não cromar tudo (gosto antigo) |
| **Cor de carro = identidade** | Cores saturadas e vintage (verde-escuro, vinho, amarelo-claro, azul-céu) — evocam Brasil anos 70-90 |
| **Adesivos brasileiros** | Family sticker (família no vidro), adesivo de time fictício ("Genésio FC"), "MEU AMOR POR VOCÊ É MAIOR QUE O TRÂNSITO" |

### 5.2 Classes e Silhuetas

| Classe | Silhueta | Volume (triângulos) | Exemplos ficcionais (inspiração real) |
|---|---|---|---|
| **Hatch popular** | Baixo, compacto, 3 volumes discretos | 18.000 | *Uno Genésio* (Fiat Uno), *Beija-Flor* (Fiat 147), *Popzinho* (VW Gol) |
| **Sedan médio** | Três volumes, capô longo | 22.000 | *Saguar* (Ford Corcel), *Vectrain* (Chevrolet Vectra) |
| **SUV / 4x4** | Alto, robusto, para-choque proeminente | 28.000 | *Hiluxar* (Toyota Hilux), *Troller Genésio* (Troller) |
| **Esportivo** | Baixo, comprido, vidro envolvente | 30.000 | *Opalar* (Chevrolet Opala), *GT-Rio* (variant) |
| **Carro antigo** | Linhas redondas, cromados | 25.000 | *Fuscao* (VW Fusca), *Karmann-Ghi* (Karmann Ghia), *Brasilia* (VW Brasília) |
| **Moto popular** | Estreita, guidão alto, rabo alto | 12.000 | *CG Genésio* (Honda CG 160), *Popzinha* (Honda Pop 100) |
| **Moto esportiva** | Avançada, carenagem, banco baixo | 16.000 | *Cavaleira* (Yamaha Fazer), *Maxinha* (Yamaha MT-03) |
| **Caminhonete** | Plataforma longa, cabine alta, carreta | 38.000 (cavalo) + 25.000 (carreta) | *Scaveo* (Scania), *Volven* (Volvo) |
| **Ônibus urbano** | Cilíndrico-alongado, vidros laterais | 32.000 | *Genésio Bus 8123* (ônibus urbano), *Marcopulus* (Marcopolo) |
| **Van** | Retangular, vidros laterais, teto alto | 20.000 | *Van do Tonho* (van Escolar/Volare) |
| **Carroça** | Madeira, cavalo, baixíssima poluição | 8.000 | *Carroça do Seu Zé* (Sítio do Capim) |
| **Bicicleta** | Estrutura leve, roda fina | 4.000 | *Monark Genésio* (Monark) |

### 5.3 Customização Visual

Cada veículo customizável em **6 slots** (na oficina mecânica do Dr. Éverton — Monte Verde):

| Slot | Opções (MVP) | Opções (final) | Custo visual |
|---|---|---|---|
| **Pintura (cor primária)** | 12 cores brasileiras vintage | 40+ + acabamentos (fosco/metálico/perolado) | Material swap |
| **Pintura secundária (2 tons)** | 5 padrões | 20+ | Decal |
| **Adesivos/Decal** | 8 (time, flame, listras) | 40+ | Decal projection |
| **Rodas** | 6 aros (estampado, liga leve, cromado, off-road) | 25+ | Mesh swap + material |
| **Escape** | 4 estilos (original, esportivo duplo, curbado, "latinha") | 15+ | Mesh swap + partícula de fumaça |
| **Acessórios** | 10 (antena bandeira, boneco de painel, 
doll da sucção, placa personalizada) | 30+ | Mesh swap |

> Sons de veículo detalhados em [05-sistemas-jogo.md](05-sistemas-jogo.md) e [10-mecanicas-jogabilidade.md](10-mecanicas-jogabilidade.md).

### 5.4 Dano Visual

Dano é estilizado, **sem deformação física realista** (caro em mobile). Usamos **estados de dano** com swap de material/mesh:

| Estado | HP % | O que muda visual |
|---|---|---|
| **Novo** | 100–80 | Material original; limpo |
| **Amassado leve** | 80–60 | Decal de amassado nas laterais; risco leve |
| **Amassado forte** | 60–40 | Para-choque desalinhado (mesh swap); vidro trincado (material) |
| **Batido** | 40–20 | Vidro quebrado (swap); fumaça do capô (partícula preta) |
| **Criticamente danificado** | 20–0 | Fogo saindo do capô (partícula laranja + luz); pneu murchando |
| **Destruído** | 0 | Carro preto carbonizado; fumaça; jogável (ainda dirige mal) |

### 5.5 Cores Brasileiras Vintage (paleta de pintura de veículos)

| Nome | Hex | Referência |
|---|---|---|
| **Azul Genésio** | `#1F5D9C` | Azul-claro anos 80 |
| **Vinho Capim** | `#6B1F2A` | Bordeaux VW |
| **Amarelo Itaúna** | `#F4C75B` | Amarelo-ovo |
| **Verde Monte** | `#2C5E3A` | Verde-escuro militar |
| **Bege Centro** | `#D6BC8C` | Areia |
| **Vermelho Vista** | `#B0282B` | Vermelho-ferrari popular |
| **Branco Belvedere** | `#F0EDE5` | Off-white premium |
| **Preto Asfalto** | `#1A1B1F` | Preto profundo |
| **Laranja Cavaleiros** | `#E6692F` | Laranja quádrupla |
| **Cinza Chumbo** | `#4F5258` | Grafite |
| **Turquesa Tropic** | `#3DB6B6` | Turquesa retraô |
| **Rosa Funk** | `#FF6B9D` | Pink brasileiro |

---

## 6. Estilo dos Ícones

### 6.1 Sistema de Ícones

| Princípio | Decisão |
|---|---|
| **Linguagem** | Pictograma + cor + forma; **leitura em 0,2s** |
| **Traço** | Linha grossa (4–6px em canvas 256), bordas arredondadas |
| **Perspectiva** | **3/4 frontal** ligeiramente inclinada (legibilidade + profundidade sem virar 3D) |
| **Paleta** | Cada ícone usa **1 cor de fundo + 1 cor de símbolo + 1 cor de acento** máximo |
| **Sombras** | Drop shadow preto 1px + iluminação de cima |
| **Cantos** | Cantos arredondados 12% do tamanho do ícone |

### 6.2 Grid e Tamanhos

| Uso | Tamanho | Densidade | Observação |
|---|---|---|---|
| **HUD (jogando)** | 48×48 dp | @2x, @3x exportados | PNG com transparência |
| **Botão grande (mapa)** | 64×64 dp | @2x, @3x | Toque seguro |
| **Lista (inventário/loja)** | 96×96 dp | @2x, @3x | Com label embaixo |
| **Mapa / minimapa** | 24×24 dp | @2x, @3x | Versão simplificada |
| **Notificação push** | 64×64 dp | @2x, @3x | Versão simplificada |
| **Loja premium** | 128×128 dp | @2x, @3x | Detalhe máximo |

### 6.3 Catálogo de Ícones por Categoria

| Categoria | Ícones | Cor base | Exemplo visual |
|---|---|---|---|
| **Missão principal** | M01, M02... caveira azul, estrela, escudo, caveira vermelha | Por facção | Fundo colorido por facção + nº |
| **Missão secundária** | Ponto de interrogação laranja | `#FFA94D` | Fundo redondo |
| **Loja (roupas)** | Camiseta | `#FF6B9D` | Camiseta 3/4 |
| **Loja (veículos)** | Chave de roda | `#3DB6B6` | Chave cruzada |
| **Loja (comida)** | pastel/Coxinha | `#F4C75B` | Comida estilizada |
| **Posto (gasolina)** | Bomba | `#E6692F` | Bomba clássica |
| **Oficina (Dr. Éverton)** | Chave inglesa | `#5C6470` | Ferramenta |
| **Ferro-velho** | Sucata metálica | `#8A4A2A` | Peça torcida |
| **Banco** | Cifrão em coluna | `#1E90D6` | Banco genérico |
| **Barbearia** | Tesoura | `#B0282B` | Tesoura aberta |
| **Academia** | Halter | `#2C5E3A` | Halter 3/4 |
| **Hospital** | Cruz vermelha | `#E63946` | Cruz clássica |
| **Delegacia** | Placa estrela | `#1B2845` | Estrela de xerife |
| **Facção (Caminhoneiros)** | Cavalo estilizado | `#2E5A88` | Cavalo azul |
| **Facção (Milícia)** | Escudo | `#0F0F12` | Escudo dourado |
| **Facção (Motoclube)** | Caveira | `#8B1A1A` | Caveira vermelha |
| **Facção (Frente)** | Estrela | `#1F7A2F` | Estrela verde/amarela |
| **Atributo (Fome)** | Hambúrguer | `#F4C75B` | Comida |
| **Atributo (Energia)** | Raio | `#FFC857` | Raio amarelo |
| **Atributo (Sanidade)** | Cérebro | `#9B6BD9` | Roxo suave |
| **Atributo (Saúde)** | Coração | `#E63946` | Vermelho clássico |
| **Reputação** | Estrela preenchida | `#FFD300` | Dourada |
| **Moeda (R$)** | Real símbolo | `#3FA66B` | Verde-moeda |
| **Moeda (CaosCash)** | Símbolo CC$ em dourado | `#F2C14E` | Premium |
| **Clima (sol)** | Sol | `#FFC857` | Amarelo |
| **Clima (chuva)** | Nuvem + gota | `#7EC8E3` | Azul céu |
| **Clima (tempestade)** | Nuvem + raio | `#5C6470` | Cinza escuro |
| **Clima (enchente)** | Ondas | `#1F5B7E` | Azul-mar fundo |
| **Clima (neblina)** | Faixas horizontais | `#B6B9C2` | Cinza claro |

### 6.4 Consistência e Manutenção

- **Biblioteca única** de ícones em `_Art/Icons/_MasterLibrary.ai` (Illustrator) exportada via script para PNG/PCT.
- **Nomenclatura:** `icon_[categoria]_[nome]_[estado]@[2x|3x].png`. Ex.: `icon_mission_caveira_red_default@3x.png`.
- **Versão de cor acessível:** cada ícone tem também versão monocromática para uso em botões secos.

---

## 7. Animações Desejadas

### 7.1 Estado Máquina do Protagonista (resumo)

```
                ┌── IDLE (varia por humor/sanidade)
                │       │
                │       ▼
                ├── WALK ────► RUN ────► SPRINT
                │       │        │         │
                │       ▼        ▼         ▼
                ├── CROUCH (entrada em roubo/agachar)
                │       │
                │       ▼
                ├── JUMP (idle jump / running jump / vault)
                │       │
                │       ▼
                ├── CLIMB (muro baixo / fence / wall)
                │       │
                │       ▼
                ├── SWIM (na enchente de Itaúna/Centro)
                │       │
                │       ▼
                ├── COMBAT (soco/empurrão/objeto)
                │       │
                │       ▼
                ├── ROB (mini-game de assalto)
                │       │
                │       ▼
                ├── DRIVE/PILOT (estado troca player→vehicle)
                │       │
                │       ▼
                ├── SOCIAL (emote, conversar, comprar)
                │       │
                │       ▼
                ├── STUMBLE (câmbio, queda, atropelo)
                │       │
                │       ▼
                └── RAGDOLL (knockout, queda crítica)
```

### 7.2 Locomoção

| Estado | Duração (frames @30fps) | Observações |
|---|---|---|
| Idle base | Loop 90 | Respiração aditiva; olhar ao redor a cada 8s |
| Walk | Loop 30 (1 m/s) | 4 direções + diagonal via blend tree |
| Run | Loop 24 (5 m/s) | Jogo de braço, leve tilt do tronco |
| Sprint | Loop 20 (8 m/s) | Câmera ligeiramente para trás; jogador ofega |
| Crouch walk | Loop 30 (1.5 m/s) | Mãos para frente (suspeito) |
| Jump (parado) | 24 start + 12 air + 18 land | Pode ser cancelado em sprint |
| Jump (correndo) | 18 start + 12 air + 14 land | Mantém momento horizontal |
| Vault (muro 1m) | 36 | IK dos pés e mãos no muro |
| Climb (muro 2m) | 60 | Sequência completa |
| Swim idle | Loop 30 | Apenas em enchente e mar de Itaúna |
| Swim forward | Loop 24 | Braçada peito |
| Stumble (tropeço) | 18 | Reativo a colisão leve |
| Knockdown | 30 + 30 get up | Transição para ragdoll ao final |

### 7.3 Direção e Pilotagem

| Estado | Animação jogador | Animação veículo |
|---|---|---|
| Entrar no carro | 40 frames abrir porta, sentar, fechar | — |
| Dirigir reto | Mãos no volante (rig IK), corpo estável | Suspensão neutra |
| Curva | IK segue volante + tilt de cabeça | Body roll leve |
| Acelerar forte | Cabeça para trás | Inclinação traseira |
| Frear forte | Cabeça para frente | Mergulho dianteiro |
| Bater (colisão leve) | Cabeça joga para frente | Amassado (swap material) |
| Bater (colisão forte) | Knockdown (sem cair) → recupera | Vidro trinca |
| Capotagem | Player ejetado (ragdoll) | Capsiza |
| Sair do carro | 36 frames abrir porta, sair, fechar | — |
| Moto (subir) | 30 frames, pé no pedal | — |
| Moto (curva) | Inclinação de corpo | Lean同步 |

### 7.4 Combate Leve

Combate é **leve, cartoon e sem gore** ([01-visao-geral.md](01-visao-geral.md) §4). Animações:

| Ação | Duração | Descrição |
|---|---|---|
| **Soco direito** | 18 | Windup 6 + impact 2 + recover 10; *hit stop* 0,04s no impacto |
| **Soco esquerdo (combo)** | 18 | Encadeia |
| **Chute** | 24 | Mais longo, knockback maior |
| **Empurrão** | 18 | Empurra NPC (sem dano, ragdoll possível) |
| **Objeto (garrafa, cadeira)** | 24 | Swing horizontal; objeto quebra no impacto |
| **Defender (bloqueio)** | Loop | Braços cruzados na frente |
| **Esquiva (passo lateral)** | 18 | i-frames 0,2s |
| **Stumble (tomou dano)** | 18 | Pega no rosto |
| **Knockout (cair)** | 30 + 60 ground | NPC fica 5s caído, depois se levanta |
| **Roubar (mini-game assalto)** | 24 init + Loop 30威胁 + 18 sucesso/falha | Olha alvo, ameaça, recebe |

### 7.5 Emotes Sociais

| Emote | Duração | Gatilho |
|---|---|---|
| **Abraço** | 40 | Social amigo |
| **Aperto de mão** | 30 | Negócio/político |
| **Tchau** | 24 | Saída |
| **OK (joinha)** | 18 | Confirmação brasileira |
| **Dance funk (basic)** | Loop 60 | Disco/praia |
| **Dance forró** | Loop 60 | Sítio do Capim/festa junina |
| **Dance sertanejo** | Loop 60 | Centro/boteco |
| **Piscadela** | 18 | Charm |
| **Dedinho do ouvido** | 18 | "Vem cá" |
| **Cara de paisagem** | Loop 30 | Tédio |
| **Sou brasileiro não desisto nunca** | 40 | Emote épico (loot raro) |
| **Reza (sinal da cruz)** | 30 | Igreja/velório |

### 7.6 Idle por Humor e Sanidade

A animação de idle muda conforme **Sanidade** ([00-biblia-do-mundo.md](00-biblia-do-mundo.md) §4) e o **humor contextual**:

| Sanidade | Idle | Detalhe |
|---|---|---|
| Alta (85–100) | Confiança: peito estufado, sorriso | Olha ao redor com energia |
| Normal (40–84) | Base neutra | Padrão |
| Baixa (15–39) | Cabeça baixa, mãos no bolso | Olha para o chão |
| Crítica (0–14) | Mão na cabeça, tremor leve, olhar vago | Visão tremida; conversa com "vozes" |

| Humor contextual | Idle | Gatilho |
|---|---|---|
| Alegre | Sorriso, passo leve | Após vitória / compra boa |
| Bravo | Punho cerrado, maxilar apertado | Após derrota / multa |
| Cansado | Respiro pesado, ombros caídos | Energia baixa |
| Faminto | Mão na barriga | Fome baixa |
| Doente | Palidez, tosse ocasional | Saúde baixa |
| Ressaca | Mão na testa,/passos cambaleantes | Bebeu no bar na véspera |

### 7.7 Animação Facial

- **Blendshapes:** 60 no rosto do protagonista (52 ARKit + 8 custom).
- **Lip-sync:** automático a partir do áudio (lib OVRLipSync ou SalatLipSync), 15 phonemas PT-BR.
- **Microneighborhood:** NPCs têm **apenas 12 blendshapes** (sorriso, sobrancelha, olhos fechados, raiva, surpresa) — suficiente para leitura mobile sem custo.

### 7.8 Animação de NPC

| Tipo de NPC | Animação única | Pool de idle |
|---|---|---|
| Tiozão do boteco | Cerveja na boca | 3 idles (sentado, apoiado, conversando) |
| Motoboy | Capacete ajustando | 3 (celular, mochila, esperando) |
| Tia do pastel | Vira pastel | 2 (cozinhando, atendendo) |
| Policial | Aborda com mão no cinto | 4 (ronda, abordagem, fumaça, café) |
| Político | Sanduíche de voto erguido | 3 (discurso, aperto de mão, sorriso amplo) |
| Catador de reciclável | Empurra carrinho | 1 loop |
| Sujeira (ambulante) | Mostra produto | 2 (grita, oferta) |
| Vovó benzedeira | Reza terço | 2 (sentada, benze) |

### 7.9 Ragdoll e Física de Corpo

- **Active ragdoll** em quedas críticas e atropelamentos não-fatais.
- **Max bones ativos:** 15 por ragdoll (otimização mobile).
- **Cessação:** após 4s parado, ragdoll é substituído por mesh estático (NPC "nocauteado").
- **Não há desmembramento / sangue** — alinhado ao 14+ ([00-biblia-do-mundo.md](00-biblia-do-mundo.md) §1).

### 7.10 State Machines Resumidas

| Contexto | Estados | Transições principais |
|---|---|---|
| **Locomotion** | Idle, Walk, Run, Sprint, Crouch, Jump, Climb, Swim, Fall | Velocidade → trigger de input; idade de anim por blend tree |
| **Vehicle** | Enter, Drive, Exit, Crash, Rollover | Input horizontal/vertical → blend tree; colisão → trigger |
| **Combat** | Idle, Attack1, Attack2, Block, Dodge, Hit, Knockout | Input de botão + direção → ataque; dano recebido → hit |
| **Social** | Idle, Talk, Emote, Trade, Rob | Input contextual (X sobre NPC) → menu radial de emote |

---

## 8. Efeitos Climáticos e Atmosféricos

Clima dinâmico é **sistema central** ([00-biblia-do-mundo.md](00-biblia-do-mundo.md) §2 regras de bairro). Cada clima tem impacto visual E de jogabilidade. **Performance é crítica** — efeitos são escalonados em qualidade.

### 8.1 Sol

| Efeito | Implementação | Custo |
|---|---|---|
| **Direcional sun + shadows** | 1 luz directional com 2 cascades | Médio (ligado em todos os tiers) |
| **Lens flare** | Bloom + Sun shafts (URP custom) | Baixo (off em low-end) |
| **God rays through trees** | Volumetric light (low res) | Médio (off em low-end) |
| **Hard contact shadows** | SSAO + contact shadows | Médio |
| **Heat haze (Monte Verde, Centro)** | Screen distortion sutil | Baixo (off em low-end) |
| **Specular no asfalto seco** | Material PBR estilizado | Insignificante |
| **Skybox** | Procedural + nuvens em parallax | Baixo |

### 8.2 Chuva

| Efeito | Implementação | Custo |
|---|---|---|
| **Particle rain (2D billboard)** | 5.000 partículas em GPU; 2.000 low-end | Médio |
| **Wet asphalt shader** | Material swap global no início da chuva; aumenta specularity + reflection | Baixo (material swap) |
| **Splash no chão** | Decal pool de 200 splashes | Baixo |
| **Ripples em poças** | Normal map animado | Médio |
| **Skybox overcast** | Swap para nimbostratus cinza | Insignificante |
| **Fog/mist leve** | Exponential height fog | Baixo |
| **Wipers no veículo** | Animação de palette + material de vidro molhado | Insignificante |
| **Gotejamento no personagem** | 30 partículas em cone na cabeça | Insignificante |
| **Som de chuva** | Layered audio (rain on roof / rain on asphalt / thunder distante) | — |

### 8.3 Tempestade

| Efeito | Implementação | Custo |
|---|---|---|
| **Tudo da chuva, mais intenso** | Density 2x; gotas maiores | Médio |
| **Lightning flash** | Plane gigante branco com flash 0,1s + bloom | Baixo |
| **Thunder (lightning triggered)** | Audio + screen shake | Insignificante |
| **Vento em árvores e fios** | Wind zone intensa; vegetation shader com strong sway | Médio |
| **Vento em NPCs** | Aditivo de animação (capa, cabelo, roupa leve) | Médio |
| **Voador de objetos leves** | 20 partículas de sacola,folha,papelão | Baixo |
| **Skybox muito escuro** | Swap + +fog density | Insignificante |
| **Apagão parcial** | Postagens e luzes piscam | Insignificante |

### 8.4 Enchente (Centro e Itaúna)

A **enchente** é um dos diferenciais do jogo ([01-visao-geral.md](01-visao-geral.md) §3). Quando o Nível de Caos sobe, baixadas alagam.

| Efeito | Implementação | Custo |
|---|---|---|
| **Plano d'água que sobe** | Mesh planar com material de água stylized; sobe pela y-axis | Médio |
| **Reflexos na água** | Planar reflection (low res) ou SSR estilizado | Médio-Alto |
| **Ripples** | Normal map dinâmico por interação | Médio |
| **Carros boiando** | Vehicles swapped para versão "boiando" (mesh swap) com física de flutuabilidade leve | Médio |
| **Carros afundados** | Material mais escuro sob a água | Insignificante |
| **NPC nadando/atravessando** | Animação swim ativada | — |
| **Lixo boiando** | 50 partículas de garrafa, sacola, isopor | Baixo |
| **Profundidade visual** | Fog volumétrico subaquático | Médio |
| **Som abafado** | Filtro de low-pass no audio bus | — |
| **Pós-processamento** | Tinta azulada, contraste reduzido | Baixo |
| **Dano ao veículo** | Carro boiando perde HP lentamente | — |

### 8.5 Neblina

Neblina especialmente comum no **Monte Verde de madrugada** ([00-biblia-do-mundo.md](00-biblia-do-mundo.md) §2 regras).

| Efeito | Implementação | Custo |
|---|---|---|
| **Exponential height fog** | Fog density subindo | Baixo |
| **Volumetric fog** | UR巷 volumetric (low res) | Médio-Alto |
| **Far plane reduzido** | Dynamic draw distance reduz | Ganho de performance |
| **Skybox escondido** | Branco-cinza | Insignificante |
| **Farol de veículos essencial** | Spot lights adicionais (jogador + IA) | Médio |
| **Som abafado** | Filtro high-pass | — |
| **Particles (micro gotas)** | 1.000 partículas finas | Baixo |

### 8.6 Outros Estados Atmosféricos

| Clima | Efeito | Onde |
|---|---|---|
| **Golden hour (pôr do sol)** | Direcional alaranjada, sombras longas, bloom alto | Diário em Itaúna |
| **Anoitecer** | Skybox escuro, luzes de lojas (emissivo), postes de sódio | Todos |
| **Madrugada fria** | Tint levemente azul, neblina, poucos NPCs | Todos |
| **Poluição alta (Monte Verde)** | Haze marrom, sol vermelho | Evento industrial |
| **Fumaça de queimada (Capim)** | Partículas de fumaça distante, céu acinzentado | Evento de Sítio do Capim |

### 8.7 Impacto em Performance e LOD por Clima

Cada clima ajusta **draw distance** e **LOD bias** para compensar custo:

| Clima | Draw Distance | LOD Bias | Particles Cap | Volumetric | Som |
|---|---|---|---|---|---|
| **Sol claro** | 100% | 0 | 100% | Off | Base |
| **Chuva** | 80% | −0.5 | 80% | Off | +2 layers |
| **Tempestade** | 70% | −1.0 | 60% | On (low) | +3 layers + thunder |
| **Enchente** | 60% | −1.5 | 50% | On (water) | Filtro low-pass |
| **Neblina** | 50% | −2.0 | 50% | On (medium) | Filtro high-pass |

> **Estratégia "fog as perf budget":** neblina e enchente são os maiores aliados de performance — quando ativos, o jogador não vê o longe, então reduzimos LOD agressivamente sem perda percebida.

### 8.8 Ciclo Dia/Noite

| Hora (jogo) | Hora real | Estado |
|---|---|---|
| 06:00 | 0,5 min | Amanhecer; luz quente |
| 12:00 | 1 min | Meio-dia; luz branca |
| 18:00 | 1,5 min | Pôr do sol; luz coral |
| 00:00 | 2 min | Meia-noite; luz azul-noite |
| 24h completas | 48 min reais | 1 dia de jogo ([00-biblia-do-mundo.md](00-biblia-do-mundo.md) §4) |

> **Skybox procedural:** gradient dia/noite controlado por shader; nuvens em parallax; estrelas à noite (1.000 pontos).

---

## 9. Especificações Técnicas de Arte (resumo)

### 9.1 Budgets por Plataforma

| Recurso | Low-end (Android 4GB) | Mid-range (6GB) | High-end (8GB+ / iPhone A14+) |
|---|---|---|---|
| **Triangles por cena (visual)** | 350.000 | 800.000 | 1.500.000 |
| **Draw calls** | < 120 | < 250 | < 400 |
| **Texture memory** | 350 MB | 700 MB | 1.2 GB |
| **Particle systems ativos** | 15 | 30 | 60 |
| **Shadow cascades** | 1 | 2 | 2 + contact |
| **SSAO** | Off | On (low) | On (high) |
| **Bloom** | Off | On (low) | On (medium) |
| **Volumetric fog** | Off | Off | On (low) |
| **FPS alvo** | 30 | 60 | 60 |

### 9.2 Convenções de Pasta (sugestão)

```
/Assets
  /_Art
    /Characters
      /Player
      /NPCs
      /Rigs
    /Vehicles
    /Environment
      /Kits
        /Vista_Alegre
        /Centro_Historico
        /Polo_Monte_Verde
        /Sitio_do_Capim
        /Jardim_Belvedere
        /Praia_de_Itauna
      /Props
      /Vegetation
    /Effects
    /UI
    /Icons
    /Textures
    /Materials
    /Shaders
```

### 9.3 Padrões de Nomenclatura

| Tipo | Padrão | Exemplo |
|---|---|---|
| **Mesh** | `M_[Bairro]_[Objeto]_LOD[0-3]` | `M_Vista_Laje_03_LOD1.fbx` |
| **Material** | `MAT_[Bairro]_[Superficie]` | `MAT_Vista_Laje.mat` |
| **Texture** | `T_[Tipo]_[Nome]_[Mapa]_[Res]` | `T_Char_Caio_Albedo_2K.png` |
| **Shader** | `S_[Linguagem]_[Nome]` | `S_URP_StylizedPBR.shadergraph` |
| **Animation** | `A_[Personagem]_[Acao]_[Direção]` | `A_Player_Walk_F.anim` |
| **Icon** | `icon_[categoria]_[nome]_[estado]@[2x|3x].png` | `icon_mission_caveira_red_default@3x.png` |

### 9.4 Texture Budgets por Classe

| Classe | Resolução máxima | Mapas |
|---|---|---|
| Personagem (jogador) | 2048×2048 | Albedo, Normal, MRAO (Metallic/Roughness/AO em packing RGB) |
| Personagem (NPC) | 1024×1024 | Albedo, Normal, MRAO |
| Veículo | 2048×2048 | Albedo, Normal, MRAO, Emissive |
| Cenário módulo (LOD0) | 1024×1024 (atlas) | Albedo, Normal, MRAO |
| Vegetação | 512×512 | Albedo, Opacity, Normal |
| Ícone | 256×256 (export 64–128 final) | Albedo, Opacity |
| Skybox | 2048×2048 (cube) | Albedo |

### 9.5 Checklist de Arte Mobile (definição de pronto)

- [ ] LOD0–LOD3 + impostor gerado.
- [ ] Material usa atlas do bairro (sem material único desnecessário).
- [ ] Texturas em ASTC 6×6 (Android) e ASTC 4×4 (iOS).
- [ ] Mesh estático marcado como Static ( batching + GI bake ).
- [ ] Sem n-gons; topology limpa para shading.
- [ ] Sombras ligadas apenas para objetos jogáveis/interativos.
- [ ] Testado em tela de 5,5" com sol simulado.
- [ ] Particulas limitadas em billboarding (não mesh particles).
- [ ] Sem motion blur (custo mobile).
- [ ] Variações de cor via vertex color ou tint, não via textura nova.

---

## Apêndice A — Atalhos de Referência Rápida

### A.1 Paleta Global (1-pager)

```
SUN #FFC857   ASPHALT #2B2E33   CONCRETE #A8A39A   SKY #7EC8E3
DUSK #F26B5E  GRASS #6BAB53    CLAY #B05B3B        PINK #FF4D8D
DANGER #E63946  MONEY #3FA66B  KAOS_GOLD #F2C14E  NIGHT #1B2845
```

### A.2 Facções (1-pager)

```
CAMINHONEIROS    Azul-Tempestade #2E5A88 + Cromado #BFC5CC
MILÍCIA ESCUDO   Preto #0F0F12      + Dourado #C39B3D
MOTOCLUBE        Verm-Sangue #8B1A1A + Osso #E8DCC2
FRENTE POPULAR   Verde #1F7A2F      + Amarelo #FFD300
```

### A.3 Bairros (1-pager)

```
VISTA ALEGRE       Laje #B8B0A4 / Tijolo #A0533A / Grafite #FF4D8D
CENTRO HISTÓRICO   Pedra #7D7468 / Grafitão #4A4540 / Letreiro #F2421B
POLO MONTE VERDE   Metal #5C6470 / Concreto #8B8680 / Ferrugem #8A4A2A
SÍTIO DO CAPIM     Terra #B05B3B / Grama #7BAE52 / Casa #E3D5B8
JARDIM BELVEDERE   Vidro #9FC4D9 / Granito #C9CDD1 / Jardim #4F8C5C
PRAIA DE ITAÚNA    Areia #E8C98C / Mar #2E9BC9 / Quiosque #FF6B5E
```

---

*Documento de Direção de Arte. Em caso de conflito com a [Bíblia do Mundo](00-biblia-do-mundo.md), esta última prevalece em matéria de nomes, números e cânones; este documento prevalece em matéria puramente visual.*

*Próximo:* [10-mecanicas-jogabilidade.md](10-mecanicas-jogabilidade.md)
