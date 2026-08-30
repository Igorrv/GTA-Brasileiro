#!/usr/bin/env python3
"""Verifica destinos locais em arquivos Markdown, sem acessar a rede."""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable
from urllib.parse import unquote, urlsplit


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
MARKDOWN_LINK_PATTERN = re.compile(
    r"!?\[[^\]]*\]\(\s*(?P<target><[^>\n]+>|[^\s)\n]+)"
)
REFERENCE_LINK_PATTERN = re.compile(
    r"^\s{0,3}\[[^\]]+\]:\s*(?P<target><[^>\n]+>|[^\s\n]+)",
    re.MULTILINE,
)
HTML_LINK_PATTERN = re.compile(
    r"<(?:a|img)\b[^>]*?\b(?:href|src)\s*=\s*[\"'](?P<target>[^\"']+)[\"']",
    re.IGNORECASE,
)
FENCE_PATTERN = re.compile(r"^\s{0,3}(`{3,}|~{3,})")
INLINE_CODE_PATTERN = re.compile(r"(?<!`)`[^`\n]*`(?!`)")
SKIPPED_SCHEMES = {"data", "http", "https", "javascript", "mailto", "tel"}


@dataclass(frozen=True)
class Problem:
    source: Path
    line: int
    target: str
    message: str

    def render(self, root: Path) -> str:
        try:
            relative_source = self.source.relative_to(root)
        except ValueError:
            relative_source = self.source
        return f"{relative_source}:{self.line}: {self.message}: {self.target!r}"


def _is_ignored(path: Path, root: Path) -> bool:
    try:
        relative_parts = path.relative_to(root).parts
    except ValueError:
        relative_parts = path.parts
    return any(part in IGNORED_DIRECTORIES for part in relative_parts)


def _without_code_blocks(contents: str) -> str:
    output: list[str] = []
    active_fence: str | None = None

    for line in contents.splitlines(keepends=True):
        fence = FENCE_PATTERN.match(line)
        if fence is not None:
            marker = fence.group(1)[0]
            if active_fence is None:
                active_fence = marker
            elif marker == active_fence:
                active_fence = None
            output.append("\n" if line.endswith("\n") else "")
            continue

        if active_fence is not None:
            output.append("\n" if line.endswith("\n") else "")
            continue

        output.append(INLINE_CODE_PATTERN.sub("", line))

    return "".join(output)


def _extract_targets(contents: str) -> list[tuple[int, str]]:
    clean_contents = _without_code_blocks(contents)
    matches: list[tuple[int, str]] = []
    for pattern in (MARKDOWN_LINK_PATTERN, REFERENCE_LINK_PATTERN, HTML_LINK_PATTERN):
        for match in pattern.finditer(clean_contents):
            target = match.group("target").strip()
            if target.startswith("<") and target.endswith(">"):
                target = target[1:-1]
            line = clean_contents.count("\n", 0, match.start()) + 1
            matches.append((line, target))
    return sorted(matches)


def _local_path(target: str) -> str | None:
    if not target or target.startswith(("#", "//")):
        return None

    parsed = urlsplit(target)
    if parsed.scheme.lower() in SKIPPED_SCHEMES or parsed.netloc:
        return None
    if parsed.scheme:
        return None

    return unquote(parsed.path)


def _resolve_target(root: Path, source: Path, raw_path: str) -> Path:
    if raw_path.startswith("/"):
        return root / raw_path.lstrip("/")
    return source.parent / raw_path


def _target_exists(path: Path) -> bool:
    if path.exists():
        return True
    if path.suffix:
        return False
    return path.with_suffix(".md").exists()


def check_markdown_links(root: Path) -> tuple[list[Problem], int, int]:
    """Retorna problemas, arquivos Markdown lidos e links locais verificados."""

    root = root.resolve()
    problems: list[Problem] = []
    markdown_paths = sorted(
        path
        for path in root.rglob("*.md")
        if path.is_file() and not _is_ignored(path, root)
    )

    checked_links = 0
    for source in markdown_paths:
        try:
            contents = source.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as error:
            problems.append(Problem(source, 1, "", f"não foi possível ler o arquivo ({error})"))
            continue

        for line, target in _extract_targets(contents):
            raw_path = _local_path(target)
            if raw_path is None:
                continue

            checked_links += 1
            resolved = _resolve_target(root, source, raw_path).resolve()
            try:
                resolved.relative_to(root)
            except ValueError:
                problems.append(
                    Problem(source, line, target, "link local aponta para fora do repositório")
                )
                continue

            if not _target_exists(resolved):
                problems.append(Problem(source, line, target, "destino local não encontrado"))

    return problems, len(markdown_paths), checked_links


def _default_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _parse_args(arguments: Iterable[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Verifica links e imagens locais nos arquivos Markdown."
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
    problems, file_count, link_count = check_markdown_links(root)
    if problems:
        print(
            f"Falha na validação dos links Markdown ({len(problems)} erro(s)):",
            file=sys.stderr,
        )
        for problem in problems:
            print(f"- {problem.render(root)}", file=sys.stderr)
        return 1

    print(
        f"Links Markdown OK: {file_count} arquivo(s) e "
        f"{link_count} destino(s) local(is)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
