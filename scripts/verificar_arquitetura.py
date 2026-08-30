#!/usr/bin/env python3
"""Guarda da arquitetura do Cidade do Caos.

A regra de ouro do projeto (docs/architecture.md) e que os assemblies Caos.* tem dependencias
explicitas e UNIDIRECIONAIS. Isso e facil de escrever num documento e facil de furar sem querer:
basta um `using` a mais e uma referencia no asmdef para o grafo virar um ciclo, e a partir dai
tudo recompila junto e nada mais e testavel isolado.

Este script transforma a regra em teste. Roda em segundos, sem Unity, e e o que o CI executa.

Verificacoes:
  1. Todo asmdef tem nome unico e referencia so assemblies que existem.
  2. O grafo de dependencias e aciclico.
  3. Camada de baixo nao referencia camada de cima (Simulation e Bootstrap sao folhas).
  4. Todo `using Caos.X` esta declarado como referencia no asmdef do arquivo.
  5. Assembly marcado noEngineReferences nao usa UnityEngine/UnityEditor, nem depende de quem usa.
  6. Todo arquivo .cs esta coberto por algum asmdef.
  7. Todo asset tem .meta (senao o Unity gera um GUID novo por maquina e o git briga).

Uso:  python3 scripts/verificar_arquitetura.py
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
ASSETS = RAIZ / "Assets"

# Folhas do grafo: podem depender de tudo, mas ninguem pode depender delas.
FOLHAS = {"Caos.Simulation", "Caos.Bootstrap", "Caos.Tests"}

RE_USING_CAOS = re.compile(r"^\s*using\s+(Caos\.[A-Za-z0-9_.]+)\s*;", re.MULTILINE)
RE_ENGINE = re.compile(r"\bUnity(Engine|Editor)\b")


def sem_comentarios(texto: str) -> str:
    """Apaga comentarios, strings e chars, preservando as quebras de linha.

    A checagem de engine olha codigo, nao prosa: os arquivos do nucleo explicam nos comentarios
    justamente por que nao usam UnityEngine, e isso nao pode virar erro.
    """
    saida: list[str] = []
    i, n = 0, len(texto)
    while i < n:
        c = texto[i]
        prox = texto[i + 1] if i + 1 < n else ""

        if c == "/" and prox == "/":
            while i < n and texto[i] != "\n":
                i += 1
            continue
        if c == "/" and prox == "*":
            i += 2
            while i < n and not (texto[i] == "*" and i + 1 < n and texto[i + 1] == "/"):
                if texto[i] == "\n":
                    saida.append("\n")
                i += 1
            i += 2
            continue
        if c == "@" and prox == '"':
            i += 2
            while i < n:
                if texto[i] == '"':
                    if i + 1 < n and texto[i + 1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                if texto[i] == "\n":
                    saida.append("\n")
                i += 1
            continue
        if c in ('"', "'"):
            aspas = c
            i += 1
            while i < n and texto[i] != aspas:
                i += 2 if texto[i] == "\\" else 1
            i += 1
            continue

        saida.append(c)
        i += 1
    return "".join(saida)

erros: list[str] = []
avisos: list[str] = []


def erro(msg: str) -> None:
    erros.append(msg)


def aviso(msg: str) -> None:
    avisos.append(msg)


def carregar_asmdefs() -> dict[str, dict]:
    assemblies: dict[str, dict] = {}
    for caminho in sorted(ASSETS.rglob("*.asmdef")):
        try:
            dados = json.loads(caminho.read_text(encoding="utf-8"))
        except json.JSONDecodeError as e:
            erro(f"{caminho.relative_to(RAIZ)}: JSON invalido ({e}).")
            continue
        nome = dados.get("name")
        if not nome:
            erro(f"{caminho.relative_to(RAIZ)}: asmdef sem 'name'.")
            continue
        if nome in assemblies:
            erro(f"Dois asmdef com o nome '{nome}'.")
            continue
        assemblies[nome] = {
            "caminho": caminho,
            "pasta": caminho.parent,
            "referencias": list(dados.get("references") or []),
            "sem_engine": bool(dados.get("noEngineReferences")),
            "root": dados.get("rootNamespace") or "",
        }
    return assemblies


def checar_referencias(assemblies: dict[str, dict]) -> None:
    for nome, info in assemblies.items():
        for ref in info["referencias"]:
            if ref.startswith("Caos.") and ref not in assemblies:
                erro(f"{nome}: referencia '{ref}', que nao existe no projeto.")
            if ref == nome:
                erro(f"{nome}: referencia a si mesmo.")


def checar_ciclos(assemblies: dict[str, dict]) -> None:
    estado: dict[str, int] = {}   # 0 = nao visitado, 1 = na pilha, 2 = pronto

    def visitar(nome: str, caminho: list[str]) -> None:
        estado[nome] = 1
        for ref in assemblies[nome]["referencias"]:
            if ref not in assemblies:
                continue
            if estado.get(ref, 0) == 1:
                ciclo = " -> ".join(caminho + [nome, ref])
                erro(f"Ciclo de dependencia entre assemblies: {ciclo}")
            elif estado.get(ref, 0) == 0:
                visitar(ref, caminho + [nome])
        estado[nome] = 2

    for nome in sorted(assemblies):
        if estado.get(nome, 0) == 0:
            visitar(nome, [])


def checar_folhas(assemblies: dict[str, dict]) -> None:
    for nome, info in assemblies.items():
        for ref in info["referencias"]:
            if ref in FOLHAS and ref != nome:
                erro(
                    f"{nome}: depende de '{ref}', que e folha do grafo.\n"
                    f"       Camada de baixo nao pode enxergar cena, boot nem teste "
                    f"(use ServiceLocator ou EventBus)."
                )


def dono_do_arquivo(arquivo: Path, assemblies: dict[str, dict]) -> str | None:
    """Assembly cuja pasta e o ancestral mais proximo do arquivo (regra do Unity)."""
    melhor, profundidade = None, -1
    for nome, info in assemblies.items():
        pasta = info["pasta"]
        try:
            arquivo.relative_to(pasta)
        except ValueError:
            continue
        if len(pasta.parts) > profundidade:
            melhor, profundidade = nome, len(pasta.parts)
    return melhor


def checar_usings_e_engine(assemblies: dict[str, dict]) -> None:
    for arquivo in sorted(ASSETS.rglob("*.cs")):
        dono = dono_do_arquivo(arquivo, assemblies)
        rel = arquivo.relative_to(RAIZ)
        if dono is None:
            # Assets/Editor nao tem asmdef: cai no Assembly-CSharp-Editor, que referencia tudo.
            aviso(f"{rel}: fora de qualquer asmdef (vai para o assembly padrao do Unity).")
            continue

        info = assemblies[dono]
        texto = arquivo.read_text(encoding="utf-8")
        codigo = sem_comentarios(texto)
        permitidos = set(info["referencias"]) | {dono}

        for ns in RE_USING_CAOS.findall(codigo):
            alvo = next((a for a in assemblies if ns == a or ns.startswith(a + ".")), None)
            if alvo is None:
                continue
            if alvo not in permitidos:
                erro(
                    f"{rel}: usa '{ns}' mas '{dono}' nao referencia '{alvo}' no asmdef."
                )

        if info["sem_engine"] and RE_ENGINE.search(codigo):
            linha = next(
                (i + 1 for i, l in enumerate(codigo.splitlines()) if RE_ENGINE.search(l)), 0
            )
            erro(
                f"{rel}:{linha}: '{dono}' esta marcado noEngineReferences mas usa Unity*.\n"
                f"       Use CaosLog / CaosMath / IRandomSource, ou mova o arquivo para um "
                f"assembly com engine."
            )

    for nome, info in assemblies.items():
        if not info["sem_engine"]:
            continue
        for ref in info["referencias"]:
            if ref in assemblies and not assemblies[ref]["sem_engine"]:
                erro(
                    f"{nome}: e sem engine, mas depende de '{ref}', que usa a engine. "
                    f"Isso arrasta UnityEngine de volta."
                )


def checar_metas() -> None:
    for p in sorted(ASSETS.rglob("*")):
        if p.name.endswith(".meta"):
            continue
        if not p.with_name(p.name + ".meta").exists():
            erro(f"{p.relative_to(RAIZ)}: falta o arquivo .meta (o Unity geraria um GUID por maquina).")


def desenhar_grafo(assemblies: dict[str, dict]) -> None:
    print("Grafo de assemblies (-> = depende de):")
    for nome in sorted(assemblies):
        info = assemblies[nome]
        refs = [r for r in info["referencias"] if r.startswith("Caos.")]
        marca = "sem engine" if info["sem_engine"] else "com engine"
        alvo = ", ".join(sorted(refs)) if refs else "(nada)"
        print(f"  {nome:<18} [{marca:^10}] -> {alvo}")


def main() -> int:
    if not ASSETS.is_dir():
        print(f"ERRO: nao achei {ASSETS}", file=sys.stderr)
        return 2

    assemblies = carregar_asmdefs()
    if not assemblies:
        print("ERRO: nenhum asmdef encontrado.", file=sys.stderr)
        return 2

    checar_referencias(assemblies)
    checar_ciclos(assemblies)
    checar_folhas(assemblies)
    checar_usings_e_engine(assemblies)
    checar_metas()

    desenhar_grafo(assemblies)
    print()

    for a in avisos:
        print(f"aviso: {a}")
    if avisos:
        print()

    if erros:
        for e in erros:
            print(f"ERRO: {e}")
        print(f"\n{len(erros)} problema(s) de arquitetura.")
        return 1

    print(f"OK: {len(assemblies)} assemblies, grafo aciclico e unidirecional.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
