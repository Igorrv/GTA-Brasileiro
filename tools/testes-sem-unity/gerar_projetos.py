#!/usr/bin/env python3
"""Gera projetos .NET que espelham o grafo de asmdefs sem engine, para testar fora do Unity.

Por que isto existe
-------------------
As camadas de regra (Core, Data, World, Gameplay) estao marcadas com "noEngineReferences": true,
ou seja, elas compilam sem UnityEngine. Isso permite rodar os testes num runner comum de .NET,
sem licenca de Unity e sem Editor -- que e o unico jeito de ter teste automatico no CI deste
projeto hoje.

Os .csproj sao GERADOS a partir dos .asmdef de proposito: o grafo de dependencias tem uma unica
fonte de verdade. Se alguem adicionar uma referencia num asmdef, ela aparece aqui sozinha; se
alguem fizer uma camada pura depender de uma camada com engine, a geracao falha na hora.

Uso:
    python3 tools/testes-sem-unity/gerar_projetos.py
    dotnet test tools/testes-sem-unity/gerado/Caos.Testes/Caos.Testes.csproj
"""

from __future__ import annotations

import json
import re
import shutil
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
SCRIPTS = RAIZ / "Assets" / "Scripts"
TESTES = RAIZ / "Assets" / "Tests" / "EditMode"
SAIDA = Path(__file__).resolve().parent / "gerado"

# Unity 6 compila os assemblies do jogo em C# 9. Fixar aqui faz o build sem Unity reprovar
# qualquer sintaxe que o Editor nao aceitaria.
LANG_VERSION = "9.0"
TFM = "net8.0"


def ler_asmdefs() -> dict[str, dict]:
    encontrados: dict[str, dict] = {}
    for caminho in sorted(SCRIPTS.rglob("*.asmdef")):
        dados = json.loads(caminho.read_text(encoding="utf-8"))
        nome = dados["name"]
        if nome in encontrados:
            raise SystemExit(f"ERRO: dois asmdef com o nome '{nome}'.")
        encontrados[nome] = {
            "asmdef": caminho,
            "pasta": caminho.parent,
            "referencias": list(dados.get("references") or []),
            "sem_engine": bool(dados.get("noEngineReferences")),
        }
    return encontrados


def csproj_biblioteca(nome: str, info: dict, todos: dict[str, dict]) -> str:
    pasta_rel = info["pasta"].relative_to(RAIZ).as_posix()
    referencias = []
    for ref in info["referencias"]:
        alvo = todos.get(ref)
        if alvo is None:
            raise SystemExit(
                f"ERRO: '{nome}' referencia '{ref}', que nao e um assembly do projeto.\n"
                f"       Um assembly sem engine so pode depender de outro assembly sem engine."
            )
        if not alvo["sem_engine"]:
            raise SystemExit(
                f"ERRO: '{nome}' esta marcado como noEngineReferences mas depende de '{ref}',\n"
                f"       que usa a engine. Corrija o asmdef ou tire a dependencia."
            )
        referencias.append(f'    <ProjectReference Include="../{ref}/{ref}.csproj" />')

    bloco_ref = "\n".join(referencias)
    if bloco_ref:
        bloco_ref = f"  <ItemGroup>\n{bloco_ref}\n  </ItemGroup>\n"

    return f"""<!-- GERADO por tools/testes-sem-unity/gerar_projetos.py a partir de {info["asmdef"].name}. Nao edite. -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>{TFM}</TargetFramework>
    <LangVersion>{LANG_VERSION}</LangVersion>
    <AssemblyName>{nome}</AssemblyName>
    <RootNamespace>{nome}</RootNamespace>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)../../../../{pasta_rel}/**/*.cs" />
  </ItemGroup>

{bloco_ref}</Project>
"""


def csproj_testes(puros: list[str]) -> str:
    referencias = "\n".join(
        f'    <ProjectReference Include="../{nome}/{nome}.csproj" />' for nome in puros
    )
    return f"""<!-- GERADO por tools/testes-sem-unity/gerar_projetos.py. Nao edite. -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>{TFM}</TargetFramework>
    <LangVersion>{LANG_VERSION}</LangVersion>
    <AssemblyName>Caos.Testes</AssemblyName>
    <RootNamespace>Caos.Tests</RootNamespace>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)../../../../Assets/Tests/EditMode/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  </ItemGroup>

  <ItemGroup>
{referencias}
  </ItemGroup>

</Project>
"""


def main() -> int:
    if not SCRIPTS.is_dir():
        print(f"ERRO: nao achei {SCRIPTS}", file=sys.stderr)
        return 1
    if not TESTES.is_dir():
        print(f"ERRO: nao achei {TESTES}", file=sys.stderr)
        return 1

    todos = ler_asmdefs()
    puros = sorted(nome for nome, info in todos.items() if info["sem_engine"])
    if not puros:
        print("ERRO: nenhum assembly com noEngineReferences=true.", file=sys.stderr)
        return 1

    if SAIDA.exists():
        shutil.rmtree(SAIDA)
    SAIDA.mkdir(parents=True)

    for nome in puros:
        destino = SAIDA / nome
        destino.mkdir()
        (destino / f"{nome}.csproj").write_text(
            csproj_biblioteca(nome, todos[nome], todos), encoding="utf-8"
        )

    destino_testes = SAIDA / "Caos.Testes"
    destino_testes.mkdir()
    (destino_testes / "Caos.Testes.csproj").write_text(csproj_testes(puros), encoding="utf-8")

    com_engine = sorted(nome for nome in todos if nome not in puros)
    print(f"Assemblies sem engine (compilados e testados aqui): {', '.join(puros)}")
    print(f"Assemblies com engine (so o Unity compila):        {', '.join(com_engine)}")
    print(f"Projetos gerados em: {SAIDA.relative_to(RAIZ)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
