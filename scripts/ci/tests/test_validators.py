from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path


CI_SCRIPTS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(CI_SCRIPTS))

import check_markdown_links  # noqa: E402
import validate_asmdefs  # noqa: E402
import validate_catalogs  # noqa: E402


class CatalogValidatorTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.data_dir = Path(self.temporary_directory.name) / "Data"
        self.data_dir.mkdir()
        self._write_catalog("districts.json", [{"id": "Centro"}])
        self._write_catalog("factions.json", [{"id": "Camelos"}])
        self._write_catalog("items.json", [{"id": "pastel"}])
        self._write_catalog(
            "shops.json",
            [{"id": "barraca", "bairro": "Centro", "itens": ["pastel"]}],
        )
        self._write_catalog(
            "vehicles.json",
            [{"id": "uno", "spawnBairro": "Centro"}],
        )
        self._write_catalog(
            "missions.json",
            [
                {
                    "id": "M00",
                    "faccao": "Camelos",
                    "preRequisitos": [],
                    "objetivos": [{"local": "Centro"}],
                }
            ],
        )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def _write_catalog(self, filename: str, items: list[dict[str, object]]) -> None:
        (self.data_dir / filename).write_text(
            json.dumps({"items": items}, ensure_ascii=False),
            encoding="utf-8",
        )

    def test_accepts_valid_references(self) -> None:
        problems, file_count, record_count = validate_catalogs.validate_catalogs(self.data_dir)

        self.assertEqual([], problems)
        self.assertEqual(6, file_count)
        self.assertEqual(6, record_count)

    def test_rejects_missing_item_reference(self) -> None:
        self._write_catalog(
            "shops.json",
            [{"id": "barraca", "bairro": "Centro", "itens": ["inexistente"]}],
        )

        problems, _, _ = validate_catalogs.validate_catalogs(self.data_dir)

        self.assertTrue(
            any(
                problem.location == "items[0].itens[0]"
                and "referência inexistente" in problem.message
                for problem in problems
            )
        )

    def test_rejects_mission_prerequisite_cycle(self) -> None:
        self._write_catalog(
            "missions.json",
            [
                {
                    "id": "M00",
                    "faccao": "",
                    "preRequisitos": ["M01"],
                    "objetivos": [],
                },
                {
                    "id": "M01",
                    "faccao": "",
                    "preRequisitos": ["M00"],
                    "objetivos": [],
                },
            ],
        )

        problems, _, _ = validate_catalogs.validate_catalogs(self.data_dir)

        self.assertTrue(any("ciclo entre missões" in problem.message for problem in problems))

    def test_rejects_duplicate_json_key(self) -> None:
        (self.data_dir / "items.json").write_text(
            '{"items": [{"id": "pastel", "id": "coxinha"}]}',
            encoding="utf-8",
        )

        problems, _, _ = validate_catalogs.validate_catalogs(self.data_dir)

        self.assertTrue(any("chave JSON repetida" in problem.message for problem in problems))


class AsmdefValidatorTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def _write_asmdef(
        self,
        folder: str,
        name: str,
        references: list[str],
        guid: str,
    ) -> None:
        directory = self.root / "Assets" / folder
        directory.mkdir(parents=True)
        path = directory / f"{name}.asmdef"
        path.write_text(
            json.dumps({"name": name, "references": references}),
            encoding="utf-8",
        )
        path.with_name(f"{path.name}.meta").write_text(
            f"fileFormatVersion: 2\nguid: {guid}\n",
            encoding="utf-8",
        )

    def test_accepts_acyclic_graph(self) -> None:
        self._write_asmdef("A", "A", [], "a" * 32)
        self._write_asmdef("B", "B", ["A"], "b" * 32)

        problems, assembly_count, edge_count = validate_asmdefs.validate_asmdefs(self.root)

        self.assertEqual([], problems)
        self.assertEqual(2, assembly_count)
        self.assertEqual(1, edge_count)

    def test_rejects_cycle_resolved_by_guid(self) -> None:
        self._write_asmdef("A", "A", [f"GUID:{'b' * 32}"], "a" * 32)
        self._write_asmdef("B", "B", [f"GUID:{'a' * 32}"], "b" * 32)

        problems, _, _ = validate_asmdefs.validate_asmdefs(self.root)

        self.assertTrue(any("ciclo entre assemblies" in problem.message for problem in problems))

    def test_rejects_missing_internal_caos_assembly(self) -> None:
        self._write_asmdef("A", "Caos.A", ["Caos.Inexistente"], "a" * 32)

        problems, _, _ = validate_asmdefs.validate_asmdefs(self.root)

        self.assertTrue(any("assembly interno inexistente" in problem.message for problem in problems))


class MarkdownLinkValidatorTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        (self.root / "docs" / "assets").mkdir(parents=True)
        (self.root / "docs" / "page.md").write_text("# Página\n", encoding="utf-8")
        (self.root / "docs" / "assets" / "banner.svg").write_text(
            "<svg></svg>\n",
            encoding="utf-8",
        )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def test_accepts_local_markdown_and_html_targets(self) -> None:
        (self.root / "README.md").write_text(
            "[Docs](docs/page.md)\n"
            '<img src="docs/assets/banner.svg" alt="Banner" />\n'
            "[Externo](https://example.com)\n",
            encoding="utf-8",
        )

        problems, _, link_count = check_markdown_links.check_markdown_links(self.root)

        self.assertEqual([], problems)
        self.assertEqual(2, link_count)

    def test_rejects_missing_local_target(self) -> None:
        (self.root / "README.md").write_text(
            "[Quebrado](docs/ausente.md)\n",
            encoding="utf-8",
        )

        problems, _, _ = check_markdown_links.check_markdown_links(self.root)

        self.assertEqual(1, len(problems))
        self.assertEqual("docs/ausente.md", problems[0].target)

    def test_ignores_links_inside_fenced_code(self) -> None:
        (self.root / "README.md").write_text(
            "```md\n[Exemplo](arquivo-que-nao-existe.md)\n```\n",
            encoding="utf-8",
        )

        problems, _, link_count = check_markdown_links.check_markdown_links(self.root)

        self.assertEqual([], problems)
        self.assertEqual(0, link_count)


if __name__ == "__main__":
    unittest.main()
