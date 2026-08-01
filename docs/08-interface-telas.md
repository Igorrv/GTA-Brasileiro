# 08 — Interface e Telas

> UX/UI **mobile-first** de **Cidade do Caos: Mundo Aberto**. Leituras complementares: [Bíblia do Mundo](00-biblia-do-mundo.md), [09-arte-estilo-visual.md](09-arte-estilo-visual.md) (paleta/ícones).

## 8.1 Princípios de UI Mobile

- **Zona do polegar:** ações primárias nos 35% inferiores da tela; botões ≥ **48×48 dp** (44 pt mínimo).
- **Ergonomia:** joystick esquerdo, ação/contexto direita; nada crítico no topo alcançável só com duas mãos.
- **Haptics:** vibração curta ao interagir, forte ao dano/crítico.
- **Feedback:** todo toque tem resposta (som + vibração + animação do botão).
- **Legibilidade:** contraste AA (WCAG ≥ 4,5:1); tipografia sans-serif robusta; escala de fonte ajustável.
- **Persistência:** salvar automático a cada ação de mundo; nenhum progresso perdido ao fechar o app.

## 8.2 Telas (índice)

| ID | Tela |
|---|---|
| T01 | Inicial / Menu principal |
| T02 | Personagem (customização/habilidades/roupas) |
| T03 | Mapa |
| T04 | Inventário |
| T05 | Veículos (garagem) |
| T06 | Status |
| T07 | Loja |
| T08 | Progressão (XP/conquistas/missões) |
| T09 | Facções |
| T10 | Eventos |
| T11 | HUD de jogo |
| T12 | Configurações / Acessibilidade |
| T13 | Oficina (Dr. Éverton) |
| T14 | Trabalho/Corrida (app VaiJá) |

---

## T01 — Tela Inicial / Menu Principal

```
┌───────────────────────────────┐
│        CIDADE DO CAOS         │
│        Mundo Aberto           │
│                               │
│      [ JOGAR  ▶ (Continuar)]  │
│      [ NOVO JOGO            ] │
│      [ CONFIGURAÇÕES   ⚙ ]    │
│      [ LOJA (CaosCash)  🛒 ]  │
│                               │
│  v1.0  •  Conectado  •  ☁     │
└───────────────────────────────┘
```
- **Jogar:** continua no último save (spawn onde parou).
- **Novo Jogo:** seleção de slot + onboarding (FTUE).
- **Loja:** abre T07 (mostra oferta diária).
- Mostra clima/atualização do dia (live-ops).

---

## T02 — Tela de Personagem

```
┌────────────────────────────────┐
│ ◄ Voltar   PERSONAGEM   [Salvar]│
├──────────────┬─────────────────┤
│              │ [Aparência][Habil.][Roupas]│
│   PRÉVIA     │ ────────────────│
│  (avatar 3D) │ Nome: Caio ___  │
│  girar/pinça │ Gênero: ◯ ◯ ◯  │
│              │ Corpo: slider   │
│              │ Cabelo: ◀ ▶     │
│              │ Pele: 1..8 ▶    │
│              │ Tatuagem: ◀ ▶   │
├──────────────┴─────────────────┤
│ Habilidades (pontos: 3)        │
│ Direção    ▰▰▰░░  [+]          │
│ Combate    ▰▰░░░  [+]          │
│ Social     ▰░░░░  [+]          │
│ Sobreviv.  ▰▰░░░  [+]          │
│ Crime      ▰░░░░  [+]          │
├────────────────────────────────┤
│            [ CONFIRMAR ]        │
└────────────────────────────────┘
```
- **Aparência:** customização (ver [03-personagens.md](03-personagens.md)).
- **Habilidades:** árvores com pontos por nível.
- **Roupas:** guarda-roupa (categorias: cabeça, tronco, pernas, pés, acessórios).

---

## T03 — Tela de Mapa

```
┌────────────────────────────────┐
│ [⌂]              🗺 MAPA     🔍 │
├────────────────────────────────┤
│   Vista Alegre  Centro         │
│      ▲(M04)      ▲(M07)  ▲🏪  │
│  ░░░░░░░░░░░░ ▓▓▓▓▓▓▓▓▓▓▓▓  │
│  ░ favela ░░ ░ Centro ░░░░░   │
│   ░░░░░░░░░░░░ ▓▓▓▓▓▓▓▓▓▓▓▓  │
│            🌊 Itaúna           │
│            (enchente)          │
├────────────────────────────────┤
│ Filtros: [⚖ Missões][⛽ Posto] │
│ [🔧 Oficina][👥 Facção][ 🏠 ]  │
├────────────────────────────────┤
│ Rota: Centro → Monte Verde 8min│
│            [ DEFINIR ROTA ]     │
└────────────────────────────────┘
```
- **Pinça:** zoom (cidade → quarteirão → prédio).
- **Ícones:** missão (M/S/F/D/G), loja, posto, oficina, casa, facção, evento ativo.
- **Rota:** traça caminho no mundo e ativa GPS/linha no minimapa.
- **Filtros:** por tipo de marcador; mostrar/ocultar.
- **Bairros coloridos** conforme facção dominante (ver [10](10-mecanicas-jogabilidade.md)).

---

## T04 — Tela de Inventário

```
┌────────────────────────────────┐
│ ◄  INVENTÁRIO   Carga: 8/16    │
├────────────┬───────────────────┤
│ [Itens][Chaves][Comida][Docs] │
├────────────┴───────────────────┤
│ ┌──┐┌──┐┌──┐┌──┐               │
│ │🍕││🔧││🔑││📄│   (grid 4×4)   │
│ └──┘└──┘└──┘└──┘               │
│ ┌──┐┌──┐┌──┐┌──┐               │
│ │💊││🛢││🧰││🌭│               │
│ └──┘└──┘└──┘└──┘               │
├────────────────────────────────┤
│ Item selecionado: 🍕 Pastel    │
│ Fome +25. [ USAR ]  [ DESCARTAR]│
└────────────────────────────────┘
```
- **Comida:** restaura Fome (algumas dão San/Energia).
- **Chaves:** de veículos, casas, galpões.
- **Documentos:** missões, provas (M12), multas.
- **Materiais:** para gambiarras (ver [10.8](10-mecanicas-jogabilidade.md)).

---

## T05 — Tela de Veículos (Garagem)

```
┌────────────────────────────────┐
│ ◄  GARAGEM   Vagas: 3/5        │
├──────────────┬─────────────────┤
│              │ ◀ Uno c/ Escada ▶│
│  PRÉVIA 3D   │ Classe: Popular │
│  (carro,     │ Estado: 72%     │
│   girar)     │ Combust.: 60%   │
│              │                 │
│              │ [ DIRIGIR ]     │
│              │ [ OFICINA ] 🔧  │
│              │ [ VENDER ]      │
├──────────────┴─────────────────┤
│ Vagas: [Uno][CG160][Bicicleta] │
│        [+ comprar vaga]        │
└────────────────────────────────┘
```
- Mostra dano, combustível, upgrades instalados.
- Atalho para Oficina (T13) e mercado de veículos.

---

## T06 — Tela de Status

```
┌────────────────────────────────┐
│ ◄  STATUS                      │
├────────────────────────────────┤
│  ❤ Saúde   ▰▰▰▰▰▰▰▰░░ 78      │
│  🍗 Fome    ▰▰▰▰▰░░░░░ 54      │
│  ⚡ Energia ▰▰▰▰▰▰▰░░░ 66      │
│  🧠 Sanid.  ▰▰▰▰▰▰▰▰░░ 81      │
├────────────────────────────────┤
│  Dinheiro:  R$ 1.240           │
│  CaosCash:  CC$  40            │
│  Nível: 12   XP: ▰▰▰░░░ p/13   │
│  Nível de Caos: 47  🔥         │
│  Procurado: ⭐ 0                │
│  Hora: Ter 14:32 (Sol forte)   │
│  Casa: Quitinete (Centro)      │
└────────────────────────────────┘
```

---

## T07 — Tela de Loja

```
┌────────────────────────────────┐
│ ◄  LOJA   [ R$ ][ CC$ ]        │
├──────────────┬─────────────────┤
│ [Destaque][Roupas][Skins Veic.]│
│ [Pacotes][Passe][Acelerador]   │
├──────────────┴─────────────────┤
│ 🔥 OFERTA DO DIA               │
│  Skin "Fusca Nitro"  CC$ 120   │
│  ┌─────┐                       │
│  │ img │   [ COMPRAR ]         │
│  └─────┘                       │
│                                │
│  Passe de Temporada 1  CC$ 0*  │
│  (*premium CC$ 90 p/ tier PRO) │
└────────────────────────────────┘
```
- Duas moedas: R$ (cosmético/jogabilidade básica) e CC$ (premium).
- Detalhes de monetização ética em [11-monetizacao.md](11-monetizacao.md).

---

## T08 — Tela de Progressão

```
┌────────────────────────────────┐
│ ◄  PROGRESSÃO                  │
├────────────────────────────────┤
│ Nível 12   XP 1.240/2.000      │
│ Conquistas (12/60):            │
│  🏆 Primeira Corrida ✓         │
│  🏆 Rei do Corredor ✓          │
│  🔒 Dono de Mansão             │
│                                │
│ MISSÕES ATIVAS:                │
│  ▶ M05 O Caminhoneiro          │
│  ▶ S03 Foto pra Bia            │
│  ▶ D01 Rota da Manhã (4/5)     │
│  🆕 G02 (gerada)               │
└────────────────────────────────┘
```

---

## T09 — Tela de Facções

```
┌────────────────────────────────┐
│ ◄  FACÇÕES                     │
├────────────────────────────────┤
│ 🚚 Caminhoneiros do Caos       │
│    Rep: ▰▰▰▰░░░░░░ +38  (Amigo)│
│    Próx. benefício (+50): ─    │
│                                │
│ 🛡 Milícia Escudo              │
│    Rep: ▰░░░░░░░░░ -22 (Friom) │
│                                │
│ 🏍 Motoclube Cavaleiros        │
│    Rep: ▰▰░░░░░░░░ +12         │
│                                │
│ ⭐ Frente Popular              │
│    Rep: ▰▰▰░░░░░░░ +30         │
└────────────────────────────────┘
```
- Barra de reputação −100…+100 por facção e por bairro (alternar aba).
- Mostra benefício atual e próximo.

---

## T10 — Tela de Eventos

```
┌────────────────────────────────┐
│ ◄  EVENTOS                     │
├────────────────────────────────┤
│ AGORA:                         │
│  ⚡ E01 Enchente — Centro       │
│     [ ver no mapa ]             │
│  🎉 E14 Show na Praça          │
│                                │
│ HISTÓRICO (últimas 24h):       │
│  • E06 Blitz (resolvido +Rep)  │
│  • E13 Motoqueiro (briguei)    │
└────────────────────────────────┘
```

---

## T11 — HUD de Jogo

```
┌────────────────────────────────┐
│ ❤78 🍗54 ⚡66 🧠81   R$1.240   │  <- status topo (compacto)
│  ⭐ (se procurado)             │
│                                │
│  ╭─────╮                       │
│  │MINIMA│      (mundo 3D)      │
│  │  PA  │                      │
│  ╰─────╯                       │
│                                │
│  ┌──┐                  ┌────┐  │
│  │🗺 │  (joystick)     │AÇÃO│  │  <- interagir/entrar
│  └──┘                  └────┘  │
│                  [Correr][Atac]│  <- botões ação (contexto)
└────────────────────────────────┘
```
- **Minimapa:** bairros, rota, eventos, polícia.
- **Botão contextual AÇÃO:** muda conforme contexto (entrar veículo, falar, abrir porta, furtar).
- **Ao dirigir:** HUD troca para acelerar/freio/buzina/farol (ver [10.12](10-mecanicas-jogabilidade.md)).
- **Avisos:** popups discretos de evento/missão/multa (não bloqueiam jogo).

---

## T12 — Configurações / Acessibilidade

```
┌────────────────────────────────┐
│ ◄  CONFIGURAÇÕES               │
├────────────────────────────────┤
│ Áudio:  Geral ▰▰▰░░  Música ▰▰ │
│ Controle: ( )Botões  ( )Gyro    │
│ Sensibilidade dir.: ▰▰▰░░      │
│ Idioma: Português (BR) ▾       │
│ ─ Acessibilidade ─             │
│ Tamanho de fonte: [A-][A+][A++]│
│ Daltonismo: ( )Não ( )Protan.. │
│ Legendas: [✓] Cores de contraste│
│ Cones de visão (furtivo): [✓]  │
│ Reduzir movimento: [ ]         │
│ Modo de toque grande: [ ]      │
│ ─ Conta/Nuvem ─                │
│ [Salvar na nuvem] [Sair]       │
└────────────────────────────────┘
```

---

## T13 — Oficina (Dr. Éverton)

```
┌────────────────────────────────┐
│ ◄  OFICINA  Dr. Éverton        │
├──────────────┬─────────────────┤
│              │ Veículo: Uno    │
│  PRÉVIA 3D   │ Dano: 72%       │
│  (com        │                 │
│   upgrades)  │ [REPARAR R$180] │
│              │ UPGRADES:       │
│              │  Motor   ▰▰ ▶   │
│              │  Freio   ▰  ▶   │
│              │  Pneu    ▰▰ ▶   │
│              │  Nitro   🔒     │
│              │ VISUAL:         │
│              │  Pintura/Adesivo│
└──────────────┴─────────────────┘
```

---

## T14 — App VaiJá (Trabalho/Corrida)

```
┌────────────────────────────────┐
│ 📱 VaiJá          Saldo: R$1.240│
├────────────────────────────────┤
│ Modo: ( )Corrida ( )Entrega    │
│                                │
│ PRÓXIMA CORRIDA:               │
│  📍 Centro → Belvedere         │
│  Dist: 2,4km   Pago: R$ 24     │
│  [ ACEITAR ▶ ]                 │
│                                │
│ Hoje: 3 corridas • R$ 72       │
│ Meta diária: 5/5 (bônus +R$50) │
└────────────────────────────────┘
```

---

## 8.3 Fluxos Principais

### Onboarding (FTUE) — objetivo: 1º veículo em ≤12 min
1. Cutscene M01 (chegada) → tutorial de andar/câmera (2 min)
2. M02 quitanda → tutorial de dinheiro/compra (2 min)
3. M03 VaiJá → tutorial de direção (3 min)
4. Mini-evento E15/E07 (humor) → tutorial de escolhas (2 min)
5. Compra/aluguel de bicicleta ou 1ª motoca → recompensa M03 (3 min)
**Métrica:** ≥60% chegam ao 1º veículo sem fricção.

### Fluxo de missão
Mapa marcador → aceitar → rota/GPS → objetivos sequenciais → confirmação → recompensa (pop-up) → atualização de status/facção.

### Fluxo de compra
Loja (T07) → seleciona item → confirma (R$/CC$) → animação de entrega → atualização de inventário/garagem.

## 8.4 Microinterações e Feedback
- Botões: escala 1,0→0,94 ao toque + som "clack" + haptic leve.
- Recompensa: moedas voando + som de caixa registradora + banner curto.
- Dano: borda vermelha + haptic forte + som de impacto.
- Evento: banner lateral não bloqueante + ícone pulsante no minimapa.

## 8.5 Acessibilidade (resumo)
Fonte escalonável, daltonismo (3 modos), legendas always-on em cutscenes, redução de movimento, cones de visão opcionais, esquemas de controle alternativos (gyro, um-botão para dirigir em modo fácil), pausa anytime.

---
*Próximo:* [09-arte-estilo-visual.md](09-arte-estilo-visual.md) • *Índice:* [README.md](README.md)
