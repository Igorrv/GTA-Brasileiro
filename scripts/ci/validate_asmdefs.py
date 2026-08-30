#!/usr/bin/env python3
"""Valida definições de assembly do Unity e rejeita ciclos locais."""

from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable


IGNORED_DIRECTORIES = {
    ".git",
    ".idea",
    ".vs",
    "Build",
    "Builds",
    "Library",
    "Logs",
    "MemoryCaptures",
    "Obj",
    "Recordings",
    "Temp",
    "UserSettings",
}
GUID_PATTERN = re.compile(r"^[0-9a-fA-F]{32}$")
META_GUID_PATTERN = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.MULTILINE)


class DuplicateJsonKeyError(ValueError):
    """Indica uma chave repetida dentro do mesmo objeto JSON."""


@dataclass(frozen=True)
class Problem:
    path: Path
    location: str
    message: str

    def render(self, root: Path) -> str:
        try:
            relative_path = self.path.relative_to(root)
        except ValueError:
            relative_path = self.path
        suffix = f" ({self.location})" if self.location else ""
        return f"{relative_path}{suffix}: {self.message}"


@dataclass(frozen=True)
class Assembly:
    name: str
    path: Path
    references: tuple[str, ...]
    guid: str | None


def _object_without_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateJsonKeyError(f"chave JSON repetida: {key!r}")
        result[key] = value
    return result


def _is_ignored(path: Path, root: Path) -> bool:
    try:
        relative_parts = path.relative_to(root).parts
    except ValueError:
        relative_parts = path.parts
    return any(part in IGNORED_DIRECTORIES for part in relative_parts)


def _discover_asmdefs(root: Path) -> list[Path]:
    return sorted(
        path
        for path in root.rglob("*.asmdef")
        if path.is_file() and not _is_ignored(path, root)
    )


def _read_guid(path: Path, root: Path, problems: list[Problem]) -> str | None:
    meta_path = path.with_name(f"{path.name}.meta")
    try:
        relative_parts = path.relative_to(root).parts
    except ValueError:
        relative_parts = ()
    meta_is_required = bool(relative_parts and relative_parts[0] == "Assets")

    if not meta_path.is_file():
        if meta_is_required:
            problems.append(Problem(meta_path, "", "arquivo .meta do asmdef não encontrado"))
        return None

    try:
        contents = meta_path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        problems.append(Problem(meta_path, "", f"não foi possível ler o .meta: {error}"))
        return None

    match = META_GUID_PATTERN.search(contents)
    if match is None:
        problems.append(Problem(meta_path, "guid", "GUID válido não encontrado"))
        return None
    return match.group(1).lower()


def _load_assembly(path: Path, root: Path, problems: list[Problem]) -> Assembly | None:
    try:
        with path.open(encoding="utf-8") as stream:
            document = json.load(stream, object_pairs_hook=_object_without_duplicate_keys)
    except (OSError, UnicodeError, json.JSONDecodeError, DuplicateJsonKeyError) as error:
        problems.append(Problem(path, "", f"JSON inválido: {error}"))
        return None

    if not isinstance(document, dict):
        problems.append(Problem(path, "", "a raiz do asmdef deve ser um objeto"))
        return None

    name = document.get("name")
    if not isinstance(name, str) or not name.strip():
        problems.append(Problem(path, "name", "deve ser uma string não vazia"))
        return None

    raw_references = document.get("references", [])
    if not isinstance(raw_references, list):
        problems.append(Problem(path, "references", "deve ser uma lista"))
        return None

    references: list[str] = []
    for index, reference in enumerate(raw_references):
        if not isinstance(reference, str) or not reference.strip():
            problems.append(
                Problem(path, f"references[{index}]", "deve ser uma string não vazia")
            )
            continue
        references.append(reference)

    return Assembly(
        name=name,
        path=path,
        references=tuple(references),
        guid=_read_guid(path, root, problems),
    )


def _find_cycles(graph: dict[str, set[str]]) -> list[list[str]]:
    state = {node: 0 for node in graph}
    stack: list[str] = []
    stack_positions: dict[str, int] = {}
    cycles: list[list[str]] = []
    seen_cycles: set[tuple[str, ...]] = set()

    def canonical(cycle: list[str]) -> tuple[str, ...]:
        body = cycle[:-1]
        rotations = [tuple(body[index:] + body[:index]) for index in range(len(body))]
        return min(rotations)

    def visit(node: str) -> None:
        state[node] = 1
        stack_positions[node] = len(stack)
        stack.append(node)

        for dependency in sorted(graph[node]):
            if state[dependency] == 0:
                visit(dependency)
            elif state[dependency] == 1:
                cycle = stack[stack_positions[dependency] :] + [dependency]
                key = canonical(cycle)
                if key not in seen_cycles:
                    seen_cycles.add(key)
                    cycles.append(cycle)

        stack.pop()
        stack_positions.pop(node)
        state[node] = 2

    for node in sorted(graph):
        if state[node] == 0:
            visit(node)

    return cycles


def validate_asmdefs(root: Path) -> tuple[list[Problem], int, int]:
    """Retorna problemas, quantidade de assemblies e arestas internas."""

    root = root.resolve()
    problems: list[Problem] = []
    paths = _discover_asmdefs(root)
    if not paths:
        return [Problem(root, "", "nenhum arquivo .asmdef encontrado")], 0, 0

    assemblies: dict[str, Assembly] = {}
    guids: dict[str, Assembly] = {}
    for path in paths:
        assembly = _load_assembly(path, root, problems)
        if assembly is None:
            continue

        previous = assemblies.get(assembly.name)
        if previous is not None:
            problems.append(
                Problem(
                    path,
                    "name",
                    f"nome de assembly duplicado; já definido em {previous.path.relative_to(root)}",
                )
            )
            continue
        assemblies[assembly.name] = assembly

        if assembly.guid is not None:
            previous_guid = guids.get(assembly.guid)
            if previous_guid is not None:
                problems.append(
                    Problem(
                        path.with_name(f"{path.name}.meta"),
                        "guid",
                        (
                            "GUID duplicado; já usado por "
                            f"{previous_guid.path.relative_to(root)}"
                        ),
                    )
                )
            else:
                guids[assembly.guid] = assembly

    graph: dict[str, set[str]] = {name: set() for name in assemblies}
    for assembly in assemblies.values():
        for index, reference in enumerate(assembly.references):
            dependency: str | None = None
            if reference.startswith("GUID:"):
                guid = reference.removeprefix("GUID:")
                if not GUID_PATTERN.fullmatch(guid):
                    problems.append(
                        Problem(
                            assembly.path,
                            f"references[{index}]",
                            f"referência GUID malformada: {reference!r}",
                        )
                    )
                    continue
                target = guids.get(guid.lower())
                if target is not None:
                    dependency = target.name
                # GUIDs desconhecidos podem pertencer a pacotes UPM não versionados.
            elif reference in assemblies:
                dependency = reference
            elif reference.startswith("Caos."):
                problems.append(
                    Problem(
                        assembly.path,
                        f"references[{index}]",
                        f"assembly interno inexistente: {reference!r}",
                    )
                )

            if dependency is not None:
                graph[assembly.name].add(dependency)

    for cycle in _find_cycles(graph):
        first_assembly = assemblies[cycle[0]]
        problems.append(
            Problem(
                first_assembly.path,
                "references",
                f"ciclo entre assemblies: {' -> '.join(cycle)}",
            )
        )

    edge_count = sum(len(dependencies) for dependencies in graph.values())
    return problems, len(assemblies), edge_count


def _default_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _parse_args(arguments: Iterable[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Valida JSON, referências locais e ciclos nos asmdefs do Unity."
    )
    parser.add_argument(
        "--root",
        type=Path,
        default=_default_root(),
        help="Raiz do repositório.",
    )
    return parser.parse_args(arguments)


def main(arguments: Iterable[str] | None = None) -> int:
    args = _parse_args(arguments)
    root = args.root.resolve()
    problems, assembly_count, edge_count = validate_asmdefs(root)
    if problems:
        print(f"Falha na validação dos asmdefs ({len(problems)} erro(s)):", file=sys.stderr)
        for problem in problems:
            print(f"- {problem.render(root)}", file=sys.stderr)
        return 1

    print(
        f"Asmdefs OK: {assembly_count} assembly(ies), {edge_count} referência(s) "
        "interna(s) e nenhum ciclo."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
