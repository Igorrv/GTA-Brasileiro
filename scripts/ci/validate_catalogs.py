#!/usr/bin/env python3
"""Valida os catálogos de StreamingAssets sem depender do Unity."""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable


REQUIRED_CATALOGS = (
    "districts.json",
    "factions.json",
    "items.json",
    "missions.json",
    "shops.json",
    "vehicles.json",
)


class DuplicateJsonKeyError(ValueError):
    """Indica uma chave repetida dentro do mesmo objeto JSON."""


@dataclass(frozen=True)
class Problem:
    path: Path
    location: str
    message: str

    def render(self, base: Path) -> str:
        try:
            relative_path = self.path.relative_to(base)
        except ValueError:
            relative_path = self.path
        suffix = f" ({self.location})" if self.location else ""
        return f"{relative_path}{suffix}: {self.message}"


def _object_without_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateJsonKeyError(f"chave JSON repetida: {key!r}")
        result[key] = value
    return result


def _load_json(path: Path, problems: list[Problem]) -> Any | None:
    try:
        with path.open(encoding="utf-8") as stream:
            return json.load(stream, object_pairs_hook=_object_without_duplicate_keys)
    except (OSError, UnicodeError, json.JSONDecodeError, DuplicateJsonKeyError) as error:
        problems.append(Problem(path, "", f"JSON inválido: {error}"))
        return None


def _items_from(
    catalogs: dict[str, dict[str, Any]],
    filename: str,
) -> list[dict[str, Any]]:
    root = catalogs.get(filename)
    if root is None:
        return []

    items = root.get("items")
    if not isinstance(items, list):
        return []

    return [item for item in items if isinstance(item, dict)]


def _validate_catalog_shapes(
    data_dir: Path,
    catalogs: dict[str, dict[str, Any]],
    problems: list[Problem],
) -> dict[str, set[str]]:
    ids_by_catalog: dict[str, set[str]] = {}

    for filename, root in catalogs.items():
        path = data_dir / filename
        if "items" not in root:
            continue

        items = root["items"]
        if not isinstance(items, list):
            problems.append(Problem(path, "items", "deve ser uma lista"))
            continue

        identifiers: set[str] = set()
        for index, item in enumerate(items):
            location = f"items[{index}]"
            if not isinstance(item, dict):
                problems.append(Problem(path, location, "deve ser um objeto"))
                continue

            identifier = item.get("id")
            if not isinstance(identifier, str) or not identifier.strip():
                problems.append(Problem(path, f"{location}.id", "deve ser uma string não vazia"))
                continue

            if identifier in identifiers:
                problems.append(
                    Problem(path, f"{location}.id", f"ID duplicado no catálogo: {identifier!r}")
                )
            identifiers.add(identifier)

        ids_by_catalog[filename] = identifiers

    return ids_by_catalog


def _string_list(
    value: Any,
    path: Path,
    location: str,
    problems: list[Problem],
) -> list[tuple[int, str]]:
    if value is None:
        return []
    if not isinstance(value, list):
        problems.append(Problem(path, location, "deve ser uma lista"))
        return []

    strings: list[tuple[int, str]] = []
    for index, entry in enumerate(value):
        if not isinstance(entry, str) or not entry.strip():
            problems.append(
                Problem(path, f"{location}[{index}]", "deve ser uma string não vazia")
            )
            continue
        strings.append((index, entry))
    return strings


def _object_list(
    value: Any,
    path: Path,
    location: str,
    problems: list[Problem],
) -> list[tuple[int, dict[str, Any]]]:
    if value is None:
        return []
    if not isinstance(value, list):
        problems.append(Problem(path, location, "deve ser uma lista"))
        return []

    objects: list[tuple[int, dict[str, Any]]] = []
    for index, entry in enumerate(value):
        if not isinstance(entry, dict):
            problems.append(Problem(path, f"{location}[{index}]", "deve ser um objeto"))
            continue
        objects.append((index, entry))
    return objects


def _validate_reference(
    value: Any,
    targets: set[str],
    path: Path,
    location: str,
    target_catalog: str,
    problems: list[Problem],
    *,
    allow_empty: bool = False,
) -> None:
    if allow_empty and value in (None, ""):
        return
    if not isinstance(value, str) or not value.strip():
        problems.append(Problem(path, location, "deve ser uma string não vazia"))
        return
    if value not in targets:
        problems.append(
            Problem(
                path,
                location,
                f"referência inexistente {value!r} em {target_catalog}",
            )
        )


def _find_cycles(graph: dict[str, list[str]]) -> list[list[str]]:
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

        for dependency in graph.get(node, []):
            if dependency not in graph:
                continue
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


def _validate_references(
    data_dir: Path,
    catalogs: dict[str, dict[str, Any]],
    ids: dict[str, set[str]],
    problems: list[Problem],
) -> None:
    district_ids = ids.get("districts.json", set())
    faction_ids = ids.get("factions.json", set())
    item_ids = ids.get("items.json", set())
    mission_ids = ids.get("missions.json", set())

    shops_path = data_dir / "shops.json"
    for shop_index, shop in enumerate(_items_from(catalogs, "shops.json")):
        prefix = f"items[{shop_index}]"
        _validate_reference(
            shop.get("bairro"),
            district_ids,
            shops_path,
            f"{prefix}.bairro",
            "districts.json",
            problems,
            allow_empty=True,
        )
        for item_index, item_id in _string_list(
            shop.get("itens", []),
            shops_path,
            f"{prefix}.itens",
            problems,
        ):
            _validate_reference(
                item_id,
                item_ids,
                shops_path,
                f"{prefix}.itens[{item_index}]",
                "items.json",
                problems,
            )

    vehicles_path = data_dir / "vehicles.json"
    for vehicle_index, vehicle in enumerate(_items_from(catalogs, "vehicles.json")):
        _validate_reference(
            vehicle.get("spawnBairro"),
            district_ids,
            vehicles_path,
            f"items[{vehicle_index}].spawnBairro",
            "districts.json",
            problems,
        )

    missions_path = data_dir / "missions.json"
    prerequisite_graph: dict[str, list[str]] = {identifier: [] for identifier in mission_ids}
    for mission_index, mission in enumerate(_items_from(catalogs, "missions.json")):
        prefix = f"items[{mission_index}]"
        mission_id = mission.get("id")

        _validate_reference(
            mission.get("faccao"),
            faction_ids,
            missions_path,
            f"{prefix}.faccao",
            "factions.json",
            problems,
            allow_empty=True,
        )

        prerequisites = _string_list(
            mission.get("preRequisitos", []),
            missions_path,
            f"{prefix}.preRequisitos",
            problems,
        )
        for prerequisite_index, prerequisite_id in prerequisites:
            _validate_reference(
                prerequisite_id,
                mission_ids,
                missions_path,
                f"{prefix}.preRequisitos[{prerequisite_index}]",
                "missions.json",
                problems,
            )
            if isinstance(mission_id, str) and prerequisite_id in mission_ids:
                prerequisite_graph.setdefault(mission_id, []).append(prerequisite_id)

        objectives = _object_list(
            mission.get("objetivos", []),
            missions_path,
            f"{prefix}.objetivos",
            problems,
        )
        for objective_index, objective in objectives:
            local = objective.get("local")
            if local in (None, ""):
                continue
            _validate_reference(
                local,
                district_ids,
                missions_path,
                f"{prefix}.objetivos[{objective_index}].local",
                "districts.json",
                problems,
            )

    for cycle in _find_cycles(prerequisite_graph):
        problems.append(
            Problem(
                missions_path,
                "preRequisitos",
                f"ciclo entre missões: {' -> '.join(cycle)}",
            )
        )

    events_path = data_dir / "events.json"
    reputation_targets = district_ids | faction_ids
    for event_index, event in enumerate(_items_from(catalogs, "events.json")):
        prefix = f"items[{event_index}]"
        for district_index, district_id in _string_list(
            event.get("bairros", []),
            events_path,
            f"{prefix}.bairros",
            problems,
        ):
            _validate_reference(
                district_id,
                district_ids,
                events_path,
                f"{prefix}.bairros[{district_index}]",
                "districts.json",
                problems,
            )

        options = _object_list(
            event.get("opcoes", []),
            events_path,
            f"{prefix}.opcoes",
            problems,
        )
        for option_index, option in options:
            impact = option.get("impacto")
            if impact is None:
                continue
            if not isinstance(impact, dict):
                problems.append(
                    Problem(
                        events_path,
                        f"{prefix}.opcoes[{option_index}].impacto",
                        "deve ser um objeto",
                    )
                )
                continue

            reputation = _object_list(
                impact.get("rep", []),
                events_path,
                f"{prefix}.opcoes[{option_index}].impacto.rep",
                problems,
            )
            for reputation_index, entry in reputation:
                _validate_reference(
                    entry.get("alvo"),
                    reputation_targets,
                    events_path,
                    (
                        f"{prefix}.opcoes[{option_index}].impacto."
                        f"rep[{reputation_index}].alvo"
                    ),
                    "districts.json ou factions.json",
                    problems,
                )


def validate_catalogs(data_dir: Path) -> tuple[list[Problem], int, int]:
    """Retorna problemas, total de arquivos lidos e total de registros."""

    data_dir = data_dir.resolve()
    problems: list[Problem] = []
    if not data_dir.is_dir():
        return [Problem(data_dir, "", "diretório de catálogos não encontrado")], 0, 0

    paths = sorted(data_dir.glob("*.json"))
    catalogs: dict[str, dict[str, Any]] = {}
    for path in paths:
        root = _load_json(path, problems)
        if root is None:
            continue
        if not isinstance(root, dict):
            problems.append(Problem(path, "", "a raiz do catálogo deve ser um objeto"))
            continue
        catalogs[path.name] = root

    for filename in REQUIRED_CATALOGS:
        path = data_dir / filename
        if filename not in catalogs and not any(problem.path == path for problem in problems):
            problems.append(Problem(path, "", "catálogo obrigatório não encontrado"))
            continue
        if filename in catalogs and "items" not in catalogs[filename]:
            problems.append(Problem(path, "items", "lista obrigatória ausente"))

    ids = _validate_catalog_shapes(data_dir, catalogs, problems)
    _validate_references(data_dir, catalogs, ids, problems)

    record_count = sum(
        len(root["items"])
        for root in catalogs.values()
        if isinstance(root.get("items"), list)
    )
    return problems, len(paths), record_count


def _default_data_dir() -> Path:
    return Path(__file__).resolve().parents[2] / "Assets" / "StreamingAssets" / "Data"


def _parse_args(arguments: Iterable[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Valida sintaxe, IDs e referências dos catálogos do jogo."
    )
    parser.add_argument(
        "--data-dir",
        type=Path,
        default=_default_data_dir(),
        help="Diretório que contém os catálogos JSON.",
    )
    return parser.parse_args(arguments)


def main(arguments: Iterable[str] | None = None) -> int:
    args = _parse_args(arguments)
    problems, file_count, record_count = validate_catalogs(args.data_dir)
    if problems:
        print(f"Falha na validação dos catálogos ({len(problems)} erro(s)):", file=sys.stderr)
        for problem in problems:
            print(f"- {problem.render(args.data_dir.parent.parent.parent)}", file=sys.stderr)
        return 1

    print(
        f"Catálogos OK: {file_count} arquivo(s), {record_count} registro(s) "
        "e referências válidas."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
