# Portal — Cidade do Caos

<span class="caos-kicker">Documentação viva</span>

Bem-vindo ao hub do projeto **Cidade do Caos**. Aqui o Game Design Document, a arquitetura Unity e o status do slice jogável convivem num portal pesquisável.

## Como navegar

1. **Design** → comece pela [Bíblia do Mundo](00-biblia-do-mundo.md) (fonte de verdade).
2. **Código** → leia [Arquitetura](architecture.md) e [Tecnologia](12-tecnologia-implementacao.md).
3. **Prioridade** → acompanhe o [Roadmap](13-mvp-roadmap.md).

Use a **busca** no canto da sidebar para achar eventos, missões, veículos ou sistemas.

<div class="caos-grid">

<div class="caos-card">

### O jogo

Sandbox urbano mobile em São Genésio. Humor BR, caos crescente, direção com peso e sessões de 5–15 min.

[Visão geral →](01-visao-geral.md)

</div>

<div class="caos-card">

### Slice S1–S8

Cidade 960×960 m, 36 veículos, polícia, comércio, rádio, missões, touch e save — já jogável no Editor.

[Roadmap →](13-mvp-roadmap.md)

</div>

<div class="caos-card">

### Arquitetura

Assemblies `Caos.*` com grafo acíclico, EventBus, ServiceLocator e cidade gerada em runtime.

[Ver módulos →](architecture.md)

</div>

<div class="caos-card">

### Conteúdo data-driven

Catálogos JSON em `StreamingAssets/Data` — veículos, itens, lojas, rádio, eventos e missões.

[Tecnologia →](12-tecnologia-implementacao.md)

</div>

</div>

## Loop em uma frase

Explorar → trabalhar / missão → sobreviver (fome, energia, sanidade) → investir → evoluir — com o **Caos** subindo a cada sessão.

## Atalhos rápidos

| Quero… | Vá para |
|---|---|
| Entender nomes, moedas e escalas | [00 · Bíblia](00-biblia-do-mundo.md) |
| Ver física de carro e frota | [04 · Direção](04-sistemas-direcao-veiculos.md) |
| IPC-Caos, reputação, sanidade | [05 · Sistemas](05-sistemas-jogo.md) |
| Wireframes e telas mobile | [08 · Interface](08-interface-telas.md) |
| Abrir o projeto no Unity | [README do repositório](../README.md) |

---

*Em caso de conflito entre qualquer seção e a Bíblia do Mundo, **a Bíblia prevalece**.*
