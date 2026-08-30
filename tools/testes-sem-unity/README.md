# Testes das regras sem abrir o Unity

As camadas de regra do jogo — `Caos.Core`, `Caos.Data`, `Caos.World` e `Caos.Gameplay` — estão
marcadas com `"noEngineReferences": true` nos seus `.asmdef`. Elas não compilam contra `UnityEngine`:
economia, IPC-Caos, XP, missões, atributos, reputação, relógio e barramento de eventos são C# puro.

Isso tem uma consequência prática: **dá para compilar e testar essa parte do jogo com o SDK do .NET,
sem licença de Unity e sem Editor.** É o que roda no CI a cada pull request.

## Rodar

```bash
python3 tools/testes-sem-unity/gerar_projetos.py
dotnet test tools/testes-sem-unity/gerado/Caos.Testes/Caos.Testes.csproj
```

## Como funciona

`gerar_projetos.py` lê os `.asmdef` do projeto e escreve um `.csproj` para cada assembly sem engine,
espelhando exatamente as mesmas referências. Os arquivos gerados vão para `gerado/` (fora do git).

O grafo de dependências tem, portanto, **uma única fonte de verdade: o `.asmdef`**. Mexeu na
referência lá, ela aparece aqui sozinha. E se alguém fizer uma camada pura depender de uma camada com
engine, a geração falha na hora em vez de descobrirmos isso no build de Android.

Os testes ficam em `Assets/Tests/EditMode/` e são os **mesmos arquivos** que o Test Runner do Unity
roda pelo assembly `Caos.Tests` — não existe cópia para sair de sincronia. Eles usam só NUnit e as
APIs do jogo, nunca `UnityEngine`.

## Limites

O que **não** é coberto aqui: `Caos.Simulation`, `Caos.Bootstrap`, `Caos.Content` e `Caos.Save`.
São as camadas que dependem de MonoBehaviour, física, UI e I/O da engine — para elas continua valendo
o smoke em Play Mode (`Caos › Play (smoke)`) e o teste na mão no aparelho.
