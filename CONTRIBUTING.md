# Como contribuir

Obrigado por ajudar a construir **São Genésio**. Este guia mantém o repositório coerente com o GDD e com a arquitetura de assemblies.

## Antes de codar

1. Leia a [Bíblia do Mundo](docs/00-biblia-do-mundo.md) — em conflito, ela vence.
2. Entenda o grafo em [docs/architecture.md](docs/architecture.md).
3. Abra uma issue descrevendo o problema/feature (exceto typos óbvios).

## Setup

1. Unity Hub → Unity **6000.5.2f1** (ou 6.x compatível).
2. Clone o repo e adicione a pasta no Hub.
3. Cena vazia `Bootstrap` → Play (ver [README](README.md)).

## Regras de código

- **Respeite os asmdefs.** Não crie referência `Simulation → Bootstrap` nem ciclos.
- **Conteúdo novo** (item, loja, missão, evento, veículo): prefira JSON em `Assets/StreamingAssets/Data/`.
- **Performance mobile:** pooling, materiais compartilhados, sem alloc em `Update` quente.
- **Nomes e tom:** português BR, humor leve, paródias — nunca marcas reais.
- **Sem assets binários pesados** sem discussão prévia (o projeto é runtime-generated).

### Onde colocar o quê

| Mudança | Assembly / pasta |
|---|---|
| Evento de domínio, utilitário | `Caos.Core` |
| DTO / loader | `Caos.Data` |
| Relógio, clima, Caos | `Caos.World` |
| Economia, atributos, missões | `Caos.Gameplay` |
| Persistência | `Caos.Save` |
| MonoBehaviour, cidade, UI | `Caos.Simulation` |
| Composition root | `Caos.Bootstrap` |
| Build / smoke | `Assets/Editor` |
| Design / lore | `docs/` |

## Commits

Estilo sugerido ([Conventional Commits](https://www.conventionalcommits.org/)):

```
feat(simulation): polícia antecipa posição do alvo
fix(city): static batching quebrava em quarteirão vazio
docs(gdd): alinhar frota S8 com vehicles.json
chore: atualizar .gitignore de logs locais
```

## Pull requests

- Uma intenção por PR (não misture refactor gigante + feature).
- Descreva **como testar** no Editor (e no Device Simulator se for touch).
- Atualize GDD/README se mudar comportamento visível ao jogador.
- Use o template em `.github/PULL_REQUEST_TEMPLATE.md`.

## Validação rápida

- Compila no Editor sem erros.
- Play Mode: cidade sobe, player anda, entra no carro, HUD aparece.
- Se tocou em boot/cidade: rode o smoke headless (`CaosPlaySmoke.Run`) quando possível.

## Código de conduta

Participação regida pelo [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
