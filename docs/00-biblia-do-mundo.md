# 00 — Bíblia do Mundo (Fonte de Verdade)

> **Status:** CANÔNICO. Todo o restante do GDD DEVE usar os nomes, números, escalas e convenções definidos aqui. Em caso de conflito entre qualquer seção e este arquivo, **este arquivo prevalece**.

---

## 1. Identidade do Jogo

| Campo | Valor |
|---|---|
| **Título** | Cidade do Caos: Mundo Aberto |
| **Subtítulo / tagline** | "Sobreviva ao caos. Domine a cidade." |
| **Gênero** | Sandbox urbano de mundo aberto (ação-aventura / vida sim) |
| **Inspiração declarada** | GTA (mecânica), Bully/Sims (vida sim leve), Brazilian memes & cotidiano |
| **Plataformas-alvo** | Mobile (Android / iOS) primário; port PC secundário |
| **Engine** | Unity 6 LTS (ver [12-tecnologia-implementacao.md](12-tecnologia-implementacao.md)) |
| **Classificação indicativa pretendida** | 14+ (violência leve/cartoon, humor, criminosidade leve, **sem** violência gráfica/gore) |
| **Idioma** | Português (PT-BR) com dublagem e legendas |

## 2. A Cidade

- **Nome oficial:** *São Genésio*
- **Apelidos:** **"Cidade do Caos"**, "o Caos", "Genésio".
- **Região metropolitana:** **Grande Genésio**.
- **Inspirada em:** São Paulo (verticalização, trânsito, centro comercial), Rio de Janeiro (morros, orla, favela), Recife (orla, pontes, ruas apertadas), Belo Horizonte (bairro nobre, curvas, ladeiras).
- **Escala de mundo (alvo de design):** mapa contínuo de **~9 km²** jogáveis (3 km × 3 km), com bairros distintos conectados por vias expressas, pontes e uma rodovia perimetral.
- **População fictícia:** ~3,2 milhões (sentida via densidade de NPCs/spawn).

### Os 6 Bairros (canônicos)

| # | Tipo | Nome | Vibe / Referência | Risco | Lojas-chave |
|---|---|---|---|---|---|
| 1 | **Favela** | Comunidade Vista Alegre ("a Vista") | Morro apertado, becos, funk, comércio informal. Rio + Recife | Médio | Barraca de pastel, birosca, barbearia |
| 2 | **Centro** | Centro Histórico | Comércio caótico, pedestres, bancos, prédios velhos. SP | Médio | Loja de roupas, banco, lanchonete |
| 3 | **Zona Industrial** | Polo Monte Verde | Galpões, pátios, caminhões, garagens. ABC paulista | Alto | Oficina, ferro-velho, posto |
| 4 | **Zona Rural Periférica** | Sítio do Capim | Chácaras, estrada de terra, carroças, mangue. Periferia Norte | Baixo | Quitanda, venda |
| 5 | **Bairro Nobre** | Jardim Belvedere | Condomínios, shoppings, esportivos, cafés caros. BH/Alphaville | Baixo | Concessionária, shopping, academia |
| 6 | **Orla / Praia** | Praia de Itaúna | Areia, quiosques, ciclovia, pôr do sol. Rio/Recife | Médio | Quiosque, surf shop, bar |

> Regras de bairro: cada bairro tem **reputação independente**, **spawn de veículos** característico (esportivos no Belvedere; carroças no Sítio do Capim; motos na Vista), **clima** próprio (enchente frequente em Itaúna e Centro; neblina no Monte Verde de madrugada) e **presença policial** diferente.

## 3. As 4 Facções (canônicas)

| # | Facção | Símbolo / Cor | Líder | Domínio | O que querem |
|---|---|---|---|---|---|
| 1 | **União dos Caminhoneiros do Caos** | Cavalo de aço / **azul-tempestade** | Seu Otacílio | Polo Monte Verde + estradas | Liberdade nas estradas, combustível barato, respeito |
| 2 | **Milícia Escudo** | Escudo / **preto-e-dourado** | "Coronel" Bento | Centro + Vista Alegre (extorsão) | Controle territorial, "segurança" cobrada |
| 3 | **Motoclube Cavaleiros do Asfalto** | Caveira de moto / **vermelho-sangue** | Tavinho | Corredores do trânsito, Praia de Itaúna | Domínio das ruas, corridas, terror no tráfego |
| 4 | **Frente Popular de São Genésio** | Estrela / **verde-e-amarelo** | Vereadora Helena Velasco | Jardim Belvedere + Centro (político) | Poder, votos, propina, imagem pública |

- **Relações iniciais entre facções (−100 a +100):**
  - Caminhoneiros ↔ Motoclube: **−40** (conflito de trânsito).
  - Milícia Escudo ↔ Frente Popular: **−30** (disputa de poder/propina).
  - Motoclube ↔ Milícia: **−20** (escaramuças).
  - Caminhoneiros ↔ Frente Popular: **+20** (votos/logística).
  - Todas as facções começam **neutras (0)** em relação ao jogador.

## 4. Moeda e Escala Numérica

| Moeda | Símbolo | Origem | Uso |
|---|---|---|---|
| **Real (soft)** | R$ | Ganho em jogo (trabalhos, missões, saques) | Tudo no mundo: veículos, comida, gasolina, multas, casas, upgrades |
| **CaosCash (hard/premium)** | CC$ | Compra com dinheiro real + recompensas raras | Cosméticos, skins premium, season pass, aceleradores |

- **Salário/dinheiro inicial do jogador:** **R$ 150,00** no bolso.
- **Ordem de grandeza de preços:** lanche R$ 12; combustível cheio R$ 180; carro popular usado R$ 18.000; esportivo R$ 220.000; quitinete (aluguel/mês) R$ 950; casa nobre R$ 1.200.000.
- **Inflação:** índice `IPC-Caos` semanal; detalhado em [05-sistemas-jogo.md](05-sistemas-jogo.md).

### Escalas de atributos (todas 0–100 salvo reputação)

| Atributo | Escala | Efeito de valor baixo (≤15) | Efeito de valor alto (≥85) |
|---|---|---|---|
| **Fome** (saciedade, 100=cheio) | 0–100 | Perde vida, fraqueza, visão tremida | Bônus de regeneração de vida |
| **Energia** | 0–100 | Movimento lento, desmaio em 0 | Bônus de corrida/ação |
| **Sanidade** | 0–100 | Alucinações leves, NPCs reagem mal, decisões ruins | Intuições/bônus em diálogos |
| **Saúde (HP)** | 0–100 | Morte → respawn no hospital (custo R$) | — |
| **Reputação** | **−100 a +100** (por facção e por bairro) | Inimigo: atacam, recusam missões | Aliado: descontos, reforços, missões exclusivas |

- **Decaimento base (por minuto de jogo):** Fome −0,5; Energia −0,4 (−1,2 correndo/dirigindo); Sanidade estável (muda por eventos).
- **1 dia de jogo = 48 min reais** (24 min dia / 24 min noite) — ver [10-mecanicas-jogabilidade.md](10-mecanicas-jogabilidade.md).

## 5. Protagonista

- **Nome padrão:** Caio "Caique" Martins — **100% customizável** (nome, gênero, aparência, roupas).
- **Background canônico (default):** 24 anos, recém-chegado(a) à cidade,Voltou para São Genésio após perder o emprego, morando num quitinete no Centro, sem carro e com R$ 150 no bolso. Quer "subir na vida" — legal ou ilegalmente, o jogador decide.
- **Atributos iniciais:** Fome 70, Energia 70, Sanidade 60, Saúde 100. Reputação 0 em tudo.

## 6. NPCs Principais (canônicos — detalhe em [03-personagens.md](03-personagens.md))

Resumo nominal (nomes travados):

1. **Tia Marlene** — dona da barraca de pastel (informante, Vista Alegre).
2. **Delegado "Bira" (Aldemir Bira)** — policial civil ambíguo (Centro).
3. **Seu Otacílio** — caminhoneiro veterano, mentor de direção (líder facção 1).
4. **Vereadora Helena Velasco** — política corrupta (líder facção 4).
5. **Tavinho** — líder do Motoclube Cavaleiros (líder facção 3).
6. **Tonho da Van** — motorista de van do alternativo, contrabando leve (Centro/Vista).
7. **Betina "Bia" Reis** — influenciadora digital, missões de fama (Belvedere/Itaúna).
8. **"Zé Pequeno do Beco" (Josival)** — líder comunitário/chefe local da Vista Alegre.
9. **Dr. Éverton** — dono da oficina mecânica, upgrades de veículo (Monte Verde).
10. **Dona Cleide** — dona do quitinete, vizinha fofoqueira e cômica (Centro).

## 7. Vizinhos (5 tipos — canônicos)

1. **O Fofoqueiro** (Seu Arlindo) — sabe de tudo, paga por fofoca.
2. **O Churrasqueiro** (Vanderlei) — faz churrasco às 7h, convida/conflictua.
3. **A Religiosa** (Dona Cida) — reza alto, julga, mas cura/benze.
4. **O Mal-humorado** (Seu Tobias) — reclama de barulho, chama fiscal/polícia.
5. **A Idosa Gente-Boa** (Vó Ivone) — oferece comida, dicas, biscoitos.

## 8. Animais Urbanos (canônicos)

| Animal | Comportamento | Interação |
|---|---|---|
| **Pombos** | Bando em praças; podem "atacar" se provocados | Alimentar (gasta comida) / espantar |
| **Cachorros de rua** | Seguem jogador, latem | Adotar (pet), alimentar, reação de NPCs |
| **Gatos** | Subiram em telhados, indiferentes | Cafuné (sobe Sanidade) |
| **Gambás** | Noturnos, assustam | Eventos cômicos noturnos |
| **Cavalos de carroça** | Puxam carroças no Sítio do Capim | Alimentar, evitar atropelamento |

## 9. Autoridades (canônicas)

| Autoridade | Ameaça | Comportamento | Gatilho |
|---|---|---|---|
| **Polícia Militar (PM)** | Alta (perseguição) | Persegue, prende em estrelas | Crime violento, trânsito caótico |
| **Polícia Civil (Delegado Bira)** | Média (investigativa) | Missões de cabo-eleitoral/propina | História, facção |
| **Guarda Municipal** | Baixa | Multas de trânsito, abordagem leve | Excesso de velocidade, estacionar errado |
| **Fiscal da Prefeitura** | Nenhum (econômico) | Cobra taxas, embarga comércio informal | Vender na rua sem alvará |
| **Político (Helena/Frente)** | Variável | Manipula mídia, contrata/ameaça | Facção política |

## 10. Glossário (termos a usar em todo o GDD)

- **Caos / Nível de Caos:** medidor 0–100 do estado da cidade ( enchentes, greves, tumultos); sobe por eventos e por ações do jogador; afeta spawn, tráfego, IA. Detalhe em [10-mecanicas-jogabilidade.md](10-mecanicas-jogabilidade.md).
- **Estrelas de procurado:** 0–5 (escala de resposta policial), estilo GTA.
- **Corredor:** espaço entre fileiras de carros (motos passam).
- **Gambiarra:** crafting urbano — improvisar ferramentas/itens com sucata.
- **Quitinete:** habitação barata inicial.
- **Alternativo:** van/ônibus informal (o do Tonho).
- **Birosca:** mercadinho de bairro.
- **IPC-Caos:** índice de inflação semanal da economia.
- **CaosCash (CC$):** moeda premium.

## 11. Convenções de Escrita do GDD

- Sempre que citar a cidade, use **São Genésio / Cidade do Caos**.
- Números de atributos sempre na escala definida acima.
- Veículos seguem tabela em [04-sistemas-direcao-veiculos.md](04-sistemas-direcao-veiculos.md).
- Eventos numerados E01–E50 em [06-eventos-aleatorios.md](06-eventos-aleatorios.md).
- Telas numeradas T01–T0x em [08-interface-telas.md](08-interface-telas.md).
- Missões numeradas M01–Mxx em [07-missoes.md](07-missoes.md).

---
*Próximo:* [01-visao-geral.md](01-visao-geral.md)
