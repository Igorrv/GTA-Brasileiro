using System.Collections.Generic;
using Caos.Data;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Constrói São Genésio do Caos em runtime a partir do <see cref="CityLayout"/> e dos catálogos:
    /// asfalto, calçada com meio-fio, quarteirões com fachada por bairro, mobiliário urbano, comércio
    /// (<see cref="Interactable"/>) e os marcos da cidade (matriz, morro com cruzeiro, orla, viaduto,
    /// terminal, campo de várzea).
    ///
    /// Cada quarteirão vira um sub-objeto que passa por <see cref="StaticBatchingUtility.Combine"/> —
    /// centenas de peças viram poucas draw calls (docs/12 §12.7). Rótulos (TextMesh) ficam fora do
    /// batching, num root separado.
    /// </summary>
    public sealed class CityGenerator
    {
        private readonly CityLayout   _layout;
        private readonly GameCatalogs _catalogs;
        private readonly Transform    _root;
        private readonly Transform    _labels;

        /// <summary>Estabelecimentos criados (o WorldBuilder passa a lista ao InteractionScanner).</summary>
        public List<Interactable> Shops { get; } = new List<Interactable>();
        /// <summary>Vagas de estacionamento em via — onde nascem os carros parados e o veículo do jogador.</summary>
        public List<Vector3> ParkingSpots { get; } = new List<Vector3>();
        /// <summary>Semáforos por eixo (o TrafficSystem alterna as fases).</summary>
        public List<MeshRenderer> LuzesNorteSul { get; } = new List<MeshRenderer>();
        public List<MeshRenderer> LuzesLesteOeste { get; } = new List<MeshRenderer>();
        /// <summary>Luminárias dos postes (o <see cref="DayNightLighting"/> acende ao anoitecer).</summary>
        public List<MeshRenderer> Luminarias { get; } = new List<MeshRenderer>();

        public Vector3 PlayerSpawn { get; private set; }

        /// <summary>Usado quando os catálogos falharam: a cidade abre mesmo sem districts.json.</summary>
        private static readonly DistrictDto kBairroPadrao = new DistrictDto
        {
            id = "Centro", nome = "Centro", tipo = "Centro", corHex = "#B9A88F",
            alturaMin = 3f, alturaMax = 8f, raio = 200f, policiamento = 3
        };

        public CityGenerator(CityLayout layout, GameCatalogs catalogs, Transform root)
        {
            PlayerActions.Assentos.Clear();   // estáticos sobrevivem ao Play Mode no Editor
            _layout   = layout;
            _catalogs = catalogs;
            _root     = root;

            var lg = new GameObject("[Rotulos]");
            lg.transform.SetParent(root, false);
            _labels = lg.transform;
        }

        // ==================================================================== build
        public void Build()
        {
            BuildTerreno();
            BuildFaixas();
            BuildQuarteiroes();
            BuildMorro();
            BuildOrla();
            BuildRioEViaduto();
            BuildMarcos();
            BuildMobiliarioViario();
            BuildBuracos();
            SpawnComercio();
            DrenarAssentosDosProps();
            DefinirSpawnDoJogador();
        }

        // ==================================================================== terreno base
        private void BuildTerreno()
        {
            float e = _layout.Extent;

            // asfalto: a cidade inteira é rua; os quarteirões são desenhados por cima
            var asfalto = new GameObject("Asfalto");
            asfalto.transform.SetParent(_root, false);
            CityPalette.Box(asfalto.transform, "Pista", new Vector3(0f, -0.05f, 0f), new Vector3(e * 2f, 0.1f, e * 2f),
                            CityPalette.MatTex(Superficie.Asfalto, Color.white, e * 2f, e * 2f, 0.18f, 0f));

            // periferia: campo/terra ao redor da malha (dá horizonte em vez de vazio)
            var fora = new GameObject("Periferia");
            fora.transform.SetParent(_root, false);
            CityPalette.Box(fora.transform, "Campo", new Vector3(0f, -0.4f, 0f), new Vector3(e * 5f, 0.6f, e * 5f),
                            CityPalette.MatTex(Superficie.Grama, Color.white, e * 5f, e * 5f));

            // mar ao sul
            var mar = new GameObject("Mar");
            mar.transform.SetParent(_root, false);
            CityPalette.Box(mar.transform, "Agua", new Vector3(0f, -0.9f, _layout.SeaZ - 260f),
                            new Vector3(e * 5f, 1.0f, 600f), CityPalette.Mar, 0f, collide: false);
            CityPalette.Box(mar.transform, "Areia", new Vector3(0f, -0.42f, _layout.BeachZ - 45f),
                            new Vector3(e * 3f, 0.5f, 110f),
                            CityPalette.MatTex(Superficie.Areia, Color.white, e * 3f, 110f));
        }

        // ==================================================================== faixas e travessias
        private void BuildFaixas()
        {
            var root = new GameObject("Sinalizacao");
            root.transform.SetParent(_root, false);
            float e = _layout.Extent;

            for (int i = 0; i < _layout.N; i++)
            {
                if (!_layout.IsAvenue(i)) continue;
                float p = _layout.LinePos(i);

                // faixa dupla contínua amarela nas avenidas (norte-sul e leste-oeste)
                CityPalette.Box(root.transform, "FaixaNS_A", new Vector3(p - 0.22f, 0.02f, 0f), new Vector3(0.16f, 0.04f, e * 2f), CityPalette.FaixaAmarela, 0f, false);
                CityPalette.Box(root.transform, "FaixaNS_B", new Vector3(p + 0.22f, 0.02f, 0f), new Vector3(0.16f, 0.04f, e * 2f), CityPalette.FaixaAmarela, 0f, false);
                CityPalette.Box(root.transform, "FaixaLO_A", new Vector3(0f, 0.02f, p - 0.22f), new Vector3(e * 2f, 0.04f, 0.16f), CityPalette.FaixaAmarela, 0f, false);
                CityPalette.Box(root.transform, "FaixaLO_B", new Vector3(0f, 0.02f, p + 0.22f), new Vector3(e * 2f, 0.04f, 0.16f), CityPalette.FaixaAmarela, 0f, false);
            }

            // travessia de pedestre (zebra simplificada) nos cruzamentos de avenida
            for (int i = 0; i < _layout.N; i++)
            {
                if (!_layout.IsAvenue(i)) continue;
                for (int j = 0; j < _layout.N; j++)
                {
                    if (!_layout.IsAvenue(j)) continue;
                    Vector3 c = _layout.Intersection(i, j);
                    if (!_layout.IsDrivable(c)) continue;
                    float w = CityLayout.AvenueHalfWidth;
                    CityPalette.Box(root.transform, "Faixa", c + new Vector3(0f, 0.025f,  w + 1.6f), new Vector3(w * 2f, 0.04f, 2.6f), CityPalette.FaixaBranca, 0f, false);
                    CityPalette.Box(root.transform, "Faixa", c + new Vector3(0f, 0.025f, -w - 1.6f), new Vector3(w * 2f, 0.04f, 2.6f), CityPalette.FaixaBranca, 0f, false);
                    CityPalette.Box(root.transform, "Faixa", c + new Vector3( w + 1.6f, 0.025f, 0f), new Vector3(2.6f, 0.04f, w * 2f), CityPalette.FaixaBranca, 0f, false);
                    CityPalette.Box(root.transform, "Faixa", c + new Vector3(-w - 1.6f, 0.025f, 0f), new Vector3(2.6f, 0.04f, w * 2f), CityPalette.FaixaBranca, 0f, false);
                }
            }
            StaticBatchingUtility.Combine(root);
        }

        // ==================================================================== quarteirões
        private void BuildQuarteiroes()
        {
            for (int i = 0; i < _layout.N - 1; i++)
            for (int j = 0; j < _layout.N - 1; j++)
            {
                Vector3 c = _layout.BlockCenter(i, j);
                if (c.z < _layout.BeachZ + 30f) continue;                       // areia
                if (Mathf.Abs(c.x - _layout.RiverX) < _layout.RiverHalfW + 12f) continue; // rio
                Vector3 dm = c - _layout.MorroCenter; dm.y = 0f;
                if (dm.sqrMagnitude < _layout.MorroRadius * _layout.MorroRadius) continue; // morro tem geração própria

                Vector2 size = _layout.BlockSize(i, j);
                var dist = _layout.DistrictAt(c) ?? kBairroPadrao;
                BuildQuarteirao(c, size, dist, i, j);
            }
        }

        private void BuildQuarteirao(Vector3 center, Vector2 size, DistrictDto dist, int i, int j)
        {
            var block = new GameObject($"Quarteirao_{i}_{j}");
            block.transform.SetParent(_root, false);
            block.transform.position = center;

            // calçada em pedra portuguesa + meio-fio
            CityPalette.Box(block.transform, "Calcada", new Vector3(0f, 0.07f, 0f), new Vector3(size.x, 0.14f, size.y),
                            CityPalette.MatTex(Superficie.Calcada, Color.white, size.x, size.y));
            CityPalette.Box(block.transform, "MeioFioN", new Vector3(0f, 0.09f,  size.y * 0.5f), new Vector3(size.x, 0.18f, 0.3f), CityPalette.MeioFio, 0f, false);
            CityPalette.Box(block.transform, "MeioFioS", new Vector3(0f, 0.09f, -size.y * 0.5f), new Vector3(size.x, 0.18f, 0.3f), CityPalette.MeioFio, 0f, false);
            CityPalette.Box(block.transform, "MeioFioL", new Vector3( size.x * 0.5f, 0.09f, 0f), new Vector3(0.3f, 0.18f, size.y), CityPalette.MeioFio, 0f, false);
            CityPalette.Box(block.transform, "MeioFioO", new Vector3(-size.x * 0.5f, 0.09f, 0f), new Vector3(0.3f, 0.18f, size.y), CityPalette.MeioFio, 0f, false);

            // miolo do lote (recuado da calçada)
            float rec = CityLayout.SidewalkWidth;
            Vector2 lote = new Vector2(size.x - rec * 2f, size.y - rec * 2f);
            string tipo = dist != null ? dist.id : "Centro";

            switch (tipo)
            {
                case "Centro":      LoteCentro(block.transform, lote, dist);      break;
                case "Cohab":       LoteCohab(block.transform, lote, dist, i, j); break;
                case "Belvedere":   LoteBelvedere(block.transform, lote, dist);   break;
                case "MonteVerde":  LoteIndustrial(block.transform, lote, dist);  break;
                case "SitioCapim":  LoteRural(block.transform, lote, dist);       break;
                case "Itauna":      LoteOrla(block.transform, lote, dist);        break;
                case "Rodoviaria":  LoteRodoviaria(block.transform, lote, dist);  break;
                case "Marginal":    LoteMarginal(block.transform, lote, dist);    break;
                default:            LoteCentro(block.transform, lote, dist);      break;
            }

            // vaga de estacionamento na via, rente ao meio-fio
            ParkingSpots.Add(center + new Vector3(size.x * 0.5f + 3.2f, 0f, Random.Range(-size.y * 0.3f, size.y * 0.3f)));

            StaticBatchingUtility.Combine(block);
        }

        // ---------------------------------------------------------- fachadas por bairro
        private void LoteCentro(Transform block, Vector2 lote, DistrictDto d)
        {
            int cols = Random.Range(2, 4);
            float w = lote.x / cols;
            for (int k = 0; k < cols; k++)
            {
                float x = -lote.x * 0.5f + w * (k + 0.5f);
                int andares = Random.Range((int)Mathf.Max(2f, d.alturaMin), (int)Mathf.Max(3f, d.alturaMax) + 1);
                float h = andares * 3.2f;
                Color baseCor = CityPalette.Vary(CityPalette.Parse(d.corHex, new Color(0.72f, 0.68f, 0.60f)), 0.10f);

                // a fachada texturizada já traz as fileiras de janela: o cubo lê como prédio
                CityPalette.Box(block, "Predio", new Vector3(x, h * 0.5f, 0f), new Vector3(w * 0.86f, h, lote.y * 0.8f),
                                CityPalette.MatTex(Superficie.Fachada, baseCor, w * 0.86f, h));

                // térreo comercial: vitrine + toldo + letreiro
                CityPalette.Box(block, "Vitrine", new Vector3(x, 1.9f, -lote.y * 0.4f - 0.08f),
                                new Vector3(w * 0.82f, 3.4f, 0.16f),
                                CityPalette.MatTex(Superficie.Vitrine, Color.white, w * 0.82f, 3.4f, 0.55f, 0.2f), 0f, false);
                CityPalette.Box(block, "Toldo", new Vector3(x, 3.9f, -lote.y * 0.4f - 0.9f), new Vector3(w * 0.8f, 0.12f, 1.8f), CityPalette.MatViva(), 0f, false);
                CityProps.CoberturaDeLaje(block, new Vector3(x, h, 0f), w * 0.5f, lote.y * 0.5f);
            }
        }

        private void LoteCohab(Transform block, Vector2 lote, DistrictDto d, int i, int j)
        {
            // conjunto habitacional: blocos idênticos, alinhados, 4 andares — e o campinho no meio
            if ((i + j) % 5 == 0)
            {
                CityProps.CampoDeVarzea(block, Vector3.zero, lote.x * 0.9f, lote.y * 0.9f);
                return;
            }
            Color cor = CityPalette.Parse(d.corHex, new Color(0.80f, 0.74f, 0.62f));
            for (int k = 0; k < 2; k++)
            {
                float z = -lote.y * 0.25f + k * lote.y * 0.5f;
                float h = 4 * 2.9f;
                CityPalette.Box(block, "BlocoCohab", new Vector3(0f, h * 0.5f, z), new Vector3(lote.x * 0.85f, h, lote.y * 0.28f),
                                CityPalette.MatTex(Superficie.Fachada, cor, lote.x * 0.85f, h));
                for (int a = 0; a < 4; a++)
                    CityPalette.Box(block, "Varanda", new Vector3(0f, a * 2.9f + 1.6f, z - lote.y * 0.15f),
                                    new Vector3(lote.x * 0.85f, 1.0f, 0.25f), CityPalette.ConcretoEscuro, 0f, false);
                CityPalette.Label(_labels, "BLOCO " + (char)('A' + k), block.position + new Vector3(0f, h + 1.2f, z), Color.white, 0.3f);
            }
        }

        private void LoteBelvedere(Transform block, Vector2 lote, DistrictDto d)
        {
            // casa de alto padrão: muro alto, portão, garagem, jardim
            CityProps.Muro(block, new Vector3(0f, 0f, -lote.y * 0.5f), lote.x, 0f, 2.8f, pichado: false);
            CityPalette.Box(block, "Portao", new Vector3(0f, 1.4f, -lote.y * 0.5f), new Vector3(4.5f, 2.8f, 0.3f), CityPalette.MetalEscuro, 0f, false);
            CityPalette.Box(block, "Jardim", new Vector3(0f, 0.16f, 0f), new Vector3(lote.x * 0.9f, 0.1f, lote.y * 0.9f), CityPalette.Grama, 0f, false);

            if (Random.value < 0.3f)
            {
                int andares = Random.Range(5, (int)Mathf.Max(6f, d.alturaMax) + 1);
                float h = andares * 3.1f;
                CityPalette.Box(block, "EdificioCondominio", new Vector3(0f, h * 0.5f, 0f), new Vector3(lote.x * 0.5f, h, lote.y * 0.5f), CityPalette.Mat(CityPalette.Parse(d.corHex, Color.white)));
                for (int a = 1; a < andares; a++)
                    CityPalette.Box(block, "Sacadas", new Vector3(0f, a * 3.1f + 1.0f, -lote.y * 0.25f - 0.2f), new Vector3(lote.x * 0.5f, 1.1f, 0.3f), CityPalette.Vidro, 0f, false);
            }
            else
            {
                float h = Random.Range(3.2f, 7f);
                CityPalette.Box(block, "Casa", new Vector3(0f, h * 0.5f, 0f), new Vector3(lote.x * 0.6f, h, lote.y * 0.5f), CityPalette.Mat(CityPalette.Vary(Color.white, 0.06f)));
                CityPalette.Box(block, "Telhado", new Vector3(0f, h + 0.3f, 0f), new Vector3(lote.x * 0.66f, 0.5f, lote.y * 0.56f), CityPalette.Telha, 0f, false);
                CityPalette.Box(block, "Piscina", new Vector3(lote.x * 0.28f, 0.2f, lote.y * 0.25f), new Vector3(5f, 0.3f, 3f), CityPalette.Mat(new Color(0.25f, 0.65f, 0.80f)), 0f, false);
                CityProps.Arvore(block, new Vector3(-lote.x * 0.3f, 0.15f, lote.y * 0.25f));
            }
        }

        private void LoteIndustrial(Transform block, Vector2 lote, DistrictDto d)
        {
            float h = Random.Range(6f, 11f);
            CityPalette.Box(block, "Galpao", new Vector3(0f, h * 0.5f, 0f), new Vector3(lote.x * 0.9f, h, lote.y * 0.75f),
                            CityPalette.MatTex(Superficie.Chapisco, CityPalette.Vary(new Color(0.72f, 0.72f, 0.70f), 0.06f), lote.x * 0.9f, h));
            CityPalette.Box(block, "TelhadoZinco", new Vector3(0f, h + 0.25f, 0f), new Vector3(lote.x * 0.94f, 0.4f, lote.y * 0.8f),
                            CityPalette.MatTex(Superficie.Metal, Color.white, lote.x * 0.94f, lote.y * 0.8f, 0.5f, 0.7f), 0f, false);
            CityPalette.Box(block, "Portao", new Vector3(0f, 2.5f, -lote.y * 0.375f - 0.1f), new Vector3(8f, 5f, 0.3f), CityPalette.MetalEscuro, 0f, false);
            CityPalette.Box(block, "Patio", new Vector3(0f, 0.16f, lote.y * 0.35f), new Vector3(lote.x * 0.9f, 0.1f, lote.y * 0.25f), CityPalette.ConcretoEscuro, 0f, false);

            if (Random.value < 0.3f)
            {
                CityPalette.Cyl(block, "Chamine", new Vector3(lote.x * 0.35f, 11f, lote.y * 0.3f), 2.4f, 22f, CityPalette.Mat(new Color(0.72f, 0.40f, 0.30f)), collide: true);
                CityPalette.Box(block, "FaixaChamine", new Vector3(lote.x * 0.35f, 20f, lote.y * 0.3f), new Vector3(2.6f, 1.4f, 2.6f), CityPalette.Mat(Color.white), 0f, false);
            }
            CityProps.CercaArame(block, new Vector3(0f, 0f, lote.y * 0.5f), lote.x, 0f);
        }

        private void LoteRural(Transform block, Vector2 lote, DistrictDto d)
        {
            CityPalette.Box(block, "Terreno", new Vector3(0f, 0.16f, 0f), new Vector3(lote.x, 0.1f, lote.y), CityPalette.GramaSeca, 0f, false);

            if (Random.value < 0.55f)
            {
                float h = Random.Range(2.8f, 3.6f);
                Vector3 p = new Vector3(Random.Range(-lote.x * 0.2f, lote.x * 0.2f), 0f, Random.Range(-lote.y * 0.2f, lote.y * 0.2f));
                CityPalette.Box(block, "CasaSimples", p + new Vector3(0f, h * 0.5f, 0f), new Vector3(9f, h, 7f),
                                CityPalette.MatTex(Superficie.Reboco, CityPalette.Vary(new Color(0.86f, 0.82f, 0.70f), 0.08f), 9f, h));
                CityPalette.Box(block, "Telhado", p + new Vector3(0f, h + 0.35f, 0f), new Vector3(10.5f, 0.6f, 8.5f),
                                CityPalette.MatTex(Superficie.Telha, Color.white, 10.5f, 8.5f), 0f, false);
                CityPalette.Box(block, "Varanda", p + new Vector3(0f, 1.2f, -4.2f), new Vector3(9f, 0.15f, 2f), CityPalette.Madeira, 0f, false);
            }
            int arvores = Random.Range(2, 6);
            for (int k = 0; k < arvores; k++)
                CityProps.Arvore(block, new Vector3(Random.Range(-lote.x * 0.45f, lote.x * 0.45f), 0.15f, Random.Range(-lote.y * 0.45f, lote.y * 0.45f)), 1.2f);
            CityProps.CercaArame(block, new Vector3(0f, 0f, -lote.y * 0.5f), lote.x, 0f);
        }

        private void LoteOrla(Transform block, Vector2 lote, DistrictDto d)
        {
            int andares = Random.Range((int)Mathf.Max(3f, d.alturaMin), (int)Mathf.Max(5f, d.alturaMax) + 1);
            float h = andares * 3.0f;
            Color cor = CityPalette.Vary(CityPalette.Parse(d.corHex, new Color(0.92f, 0.90f, 0.84f)), 0.07f);
            CityPalette.Box(block, "PredioOrla", new Vector3(0f, h * 0.5f, 0f), new Vector3(lote.x * 0.7f, h, lote.y * 0.6f),
                            CityPalette.MatTex(Superficie.Fachada, cor, lote.x * 0.7f, h));
            for (int a = 1; a < andares; a++)
                CityPalette.Box(block, "Sacada", new Vector3(0f, a * 3.0f + 1.0f, -lote.y * 0.3f - 0.25f),
                                new Vector3(lote.x * 0.7f, 1.1f, 0.5f), CityPalette.Mat(new Color(0.85f, 0.85f, 0.82f)), 0f, false);
            CityProps.CoberturaDeLaje(block, new Vector3(0f, h, 0f), lote.x * 0.4f, lote.y * 0.35f);
            CityProps.Coqueiro(block, new Vector3(-lote.x * 0.4f, 0.15f, -lote.y * 0.4f));
        }

        private void LoteRodoviaria(Transform block, Vector2 lote, DistrictDto d)
        {
            // prédio baixo comercial + fileira de barracas de camelô na frente
            float h = Random.Range(3.5f, 9f);
            CityPalette.Box(block, "Comercio", new Vector3(0f, h * 0.5f, lote.y * 0.15f), new Vector3(lote.x * 0.85f, h, lote.y * 0.5f),
                            CityPalette.Mat(CityPalette.Vary(CityPalette.Parse(d.corHex, new Color(0.78f, 0.72f, 0.62f)), 0.08f)));
            CityPalette.Box(block, "Letreiro", new Vector3(0f, h + 0.7f, lote.y * 0.15f - lote.y * 0.25f), new Vector3(lote.x * 0.6f, 1.2f, 0.3f), CityPalette.MatViva(), 0f, false);

            int barracas = Random.Range(2, 5);
            for (int k = 0; k < barracas; k++)
                CityProps.BarracaCamelo(block, new Vector3(-lote.x * 0.35f + k * (lote.x * 0.7f / Mathf.Max(1, barracas - 1)), 0.15f, -lote.y * 0.3f),
                                        0f, CityPalette.CorViva());
        }

        private void LoteMarginal(Transform block, Vector2 lote, DistrictDto d)
        {
            float h = Random.Range(3f, 8f);
            CityPalette.Box(block, "Barracao", new Vector3(0f, h * 0.5f, 0f), new Vector3(lote.x * 0.8f, h, lote.y * 0.6f),
                            CityPalette.Mat(CityPalette.Vary(new Color(0.58f, 0.56f, 0.52f), 0.08f)));
            CityProps.Muro(block, new Vector3(0f, 0f, -lote.y * 0.5f), lote.x * 0.95f, 0f, 3.0f, pichado: true);
            if (Random.value < 0.4f)
                CityProps.Arvore(block, new Vector3(lote.x * 0.35f, 0.15f, lote.y * 0.3f));
        }

        // ==================================================================== morro / favela
        private void BuildMorro()
        {
            var root = new GameObject("MorroVistaAlegre");
            root.transform.SetParent(_root, false);
            root.transform.position = _layout.MorroCenter;

            // terraços concêntricos (o carro não sobe: é viela e escadaria)
            int aneis = 5;
            for (int a = aneis; a >= 1; a--)
            {
                float r = _layout.MorroRadius * (a / (float)aneis);
                float y = _layout.TerrainHeight(_layout.MorroCenter + new Vector3(r * 0.85f, 0f, 0f));
                CityPalette.Cyl(root.transform, "Terraco", new Vector3(0f, y * 0.5f, 0f), r * 2f, Mathf.Max(0.6f, y), CityPalette.Terra, collide: true);
            }

            // casas de tijolo empilhadas, subindo o morro
            int casas = 90;
            for (int k = 0; k < casas; k++)
            {
                float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float rad = Random.Range(12f, _layout.MorroRadius - 8f);
                Vector3 local = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                float y = _layout.TerrainHeight(_layout.MorroCenter + local);

                float h = Random.Range(2.7f, 7.5f);
                float w = Random.Range(4.5f, 7.5f);
                float dp = Random.Range(4.5f, 7f);
                float yaw = Random.Range(0f, 360f);

                // metade sem reboco (tijolo aparente), metade pintada — é o que se vê no morro
                Material parede = Random.value < 0.45f
                    ? CityPalette.MatTex(Superficie.Tijolo, Color.white, w, h)
                    : CityPalette.MatTex(Superficie.Chapisco, CityPalette.Vary(CityPalette.CorViva(), 0.05f), w, h);

                CityPalette.Box(root.transform, "CasaLaje", local + new Vector3(0f, y + h * 0.5f, 0f), new Vector3(w, h, dp), parede, yaw);
                // laje descoberta com vergalhão esperando o segundo andar (institucional)
                if (Random.value < 0.55f)
                {
                    for (int v = 0; v < 4; v++)
                        CityPalette.Box(root.transform, "Vergalhao",
                            local + new Vector3(Random.Range(-w * 0.35f, w * 0.35f), y + h + 0.5f, Random.Range(-dp * 0.35f, dp * 0.35f)),
                            new Vector3(0.06f, 1.0f, 0.06f), CityPalette.Metal, 0f, false);
                }
                CityProps.CoberturaDeLaje(root.transform, local + new Vector3(0f, y + h, 0f), w, dp);
            }

            // escadaria da comunidade + vielas
            for (int s = 0; s < 3; s++)
            {
                float ang = (s * 120f + 30f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                for (int step = 0; step < 22; step++)
                {
                    float rad = _layout.MorroRadius - step * 4.2f;
                    if (rad < 6f) break;
                    Vector3 local = dir * rad;
                    float y = _layout.TerrainHeight(_layout.MorroCenter + local);
                    CityPalette.Box(root.transform, "Degrau", local + new Vector3(0f, y + 0.15f, 0f),
                                    new Vector3(3.2f, 0.3f, 4.2f), CityPalette.Concreto, ang * Mathf.Rad2Deg);
                }
            }

            CityProps.CruzeiroDoMirante(root.transform, new Vector3(0f, _layout.TerrainHeight(_layout.MorroCenter), 0f));
            CityProps.TorreDeRadio(root.transform, new Vector3(28f, _layout.TerrainHeight(_layout.MorroCenter + new Vector3(28f, 0f, 12f)), 12f));
            CityPalette.Label(_labels, "COMUNIDADE VISTA ALEGRE",
                              _layout.MorroCenter + new Vector3(0f, _layout.MorroPeak * 0.5f + 20f, -_layout.MorroRadius * 0.6f),
                              new Color(1f, 0.85f, 0.4f), 0.9f);

            StaticBatchingUtility.Combine(root);
        }

        // ==================================================================== orla
        private void BuildOrla()
        {
            var root = new GameObject("OrlaItauna");
            root.transform.SetParent(_root, false);
            float z = _layout.BeachZ + 6f;

            // calçadão de pedra portuguesa (faixa clara com desenho em xadrez simplificado)
            CityPalette.Box(root.transform, "Calcadao", new Vector3(0f, 0.12f, z), new Vector3(_layout.Extent * 1.6f, 0.24f, 9f), CityPalette.Mat(new Color(0.88f, 0.86f, 0.80f)));
            for (int k = -18; k <= 18; k++)
                CityPalette.Box(root.transform, "Onda", new Vector3(k * 22f, 0.25f, z), new Vector3(11f, 0.04f, 3.4f), CityPalette.Mat(new Color(0.25f, 0.25f, 0.27f)), 0f, false);

            // quiosques + coqueiros + areia
            string[] nomes = { "QUIOSQUE 7", "BARRACA DO NEGO VÉIO", "QUIOSQUE 12", "TENDA DO COCO" };
            for (int k = 0; k < nomes.Length; k++)
            {
                float x = -180f + k * 120f;
                CityProps.Quiosque(root.transform, new Vector3(x, 0f, z - 26f), nomes[k]);
            }
            for (int k = 0; k < 26; k++)
                CityProps.Coqueiro(root.transform, new Vector3(Random.Range(-_layout.Extent * 0.8f, _layout.Extent * 0.8f), 0f, z - Random.Range(8f, 20f)));

            // píer
            var pier = new GameObject("Pier");
            pier.transform.SetParent(root.transform, false);
            CityPalette.Box(pier.transform, "Tabuas", new Vector3(120f, 1.2f, z - 90f), new Vector3(7f, 0.3f, 150f), CityPalette.Madeira);
            for (int k = 0; k < 12; k++)
                CityPalette.Cyl(pier.transform, "Estaca", new Vector3(120f + (k % 2 == 0 ? -3f : 3f), 0.4f, z - 25f - k * 12f), 0.5f, 2.2f, CityPalette.Madeira);
            CityPalette.Box(pier.transform, "Farol", new Vector3(120f, 6f, z - 160f), new Vector3(4f, 10f, 4f), CityPalette.Mat(Color.white));
            CityPalette.Sphere(pier.transform, "LuzFarol", new Vector3(120f, 11.5f, z - 160f), 1.6f, CityPalette.Mat(new Color(1f, 0.85f, 0.4f)));

            CityPalette.Label(_labels, "PRAIA DE ITAÚNA", new Vector3(0f, 8f, z - 12f), new Color(0.15f, 0.35f, 0.55f), 0.8f);
            StaticBatchingUtility.Combine(root);
        }

        // ==================================================================== rio + viaduto
        private void BuildRioEViaduto()
        {
            var root = new GameObject("MarginalRioSujo");
            root.transform.SetParent(_root, false);

            CityPalette.Box(root.transform, "Rio", new Vector3(_layout.RiverX, -0.6f, 0f),
                            new Vector3(_layout.RiverHalfW * 2f, 1.2f, _layout.Extent * 2f), CityPalette.RioSujo, 0f, false);
            CityPalette.Box(root.transform, "MurethaO", new Vector3(_layout.RiverX - _layout.RiverHalfW, 0.6f, 0f),
                            new Vector3(1.2f, 1.6f, _layout.Extent * 2f), CityPalette.Concreto);
            CityPalette.Box(root.transform, "MurethaL", new Vector3(_layout.RiverX + _layout.RiverHalfW, 0.6f, 0f),
                            new Vector3(1.2f, 1.6f, _layout.Extent * 2f), CityPalette.Concreto);

            // viaduto sobre o rio nas avenidas leste-oeste
            for (int j = 0; j < _layout.N; j++)
            {
                if (!_layout.IsAvenue(j)) continue;
                float z = _layout.LinePos(j);
                CityPalette.Box(root.transform, "Ponte", new Vector3(_layout.RiverX, 0.6f, z),
                                new Vector3(_layout.RiverHalfW * 2f + 14f, 1.2f, CityLayout.AvenueHalfWidth * 2f), CityPalette.ConcretoEscuro);
                CityPalette.Box(root.transform, "GuardaCorpoN", new Vector3(_layout.RiverX, 1.8f, z + CityLayout.AvenueHalfWidth),
                                new Vector3(_layout.RiverHalfW * 2f + 14f, 1.2f, 0.4f), CityPalette.Metal, 0f, false);
                CityPalette.Box(root.transform, "GuardaCorpoS", new Vector3(_layout.RiverX, 1.8f, z - CityLayout.AvenueHalfWidth),
                                new Vector3(_layout.RiverHalfW * 2f + 14f, 1.2f, 0.4f), CityPalette.Metal, 0f, false);
                // pilar pichado — o cartão-postal da marginal
                CityPalette.Box(root.transform, "Pilar", new Vector3(_layout.RiverX, -0.4f, z), new Vector3(3f, 2f, 3f), CityPalette.Concreto);
                CityPalette.Box(root.transform, "Pichacao", new Vector3(_layout.RiverX + 1.6f, 0.4f, z), new Vector3(0.05f, 1.0f, 2.2f), CityPalette.Pichacao, 0f, false);
            }
            StaticBatchingUtility.Combine(root);
        }

        // ==================================================================== marcos
        private void BuildMarcos()
        {
            var root = new GameObject("Marcos");
            root.transform.SetParent(_root, false);

            // Praça da Matriz no centro geométrico da cidade
            Vector3 praca = _layout.BlockCenter(_layout.N / 2, _layout.N / 2);
            CityPalette.Box(root.transform, "PisoPraca", praca + new Vector3(0f, 0.16f, 0f), new Vector3(52f, 0.12f, 52f), CityPalette.Mat(new Color(0.80f, 0.78f, 0.72f)), 0f, false);
            CityProps.IgrejaMatriz(root.transform, praca + new Vector3(0f, 0.2f, 14f), 0f);

            // coreto
            var coreto = new GameObject("Coreto");
            coreto.transform.SetParent(root.transform, false);
            coreto.transform.position = praca + new Vector3(-14f, 0f, -12f);
            CityPalette.Cyl(coreto.transform, "Base", new Vector3(0f, 0.4f, 0f), 9f, 0.8f, CityPalette.Concreto, collide: true);
            for (int k = 0; k < 6; k++)
            {
                float a = k * 60f * Mathf.Deg2Rad;
                CityPalette.Cyl(coreto.transform, "Coluna", new Vector3(Mathf.Cos(a) * 3.8f, 2.2f, Mathf.Sin(a) * 3.8f), 0.3f, 3.6f, CityPalette.Mat(Color.white));
            }
            CityPalette.Cyl(coreto.transform, "Cupula", new Vector3(0f, 4.4f, 0f), 9.5f, 0.6f, CityPalette.Mat(new Color(0.60f, 0.20f, 0.20f)));

            for (int k = 0; k < 10; k++)
                CityProps.Arvore(root.transform, praca + new Vector3(Random.Range(-24f, 24f), 0.2f, Random.Range(-24f, 24f)));
            for (int k = 0; k < 6; k++)
            {
                Vector3 p = praca + new Vector3(Random.Range(-20f, 20f), 0.55f, Random.Range(-20f, 20f));
                float giro = Random.Range(0f, 180f);
                var banco = CityPalette.Box(root.transform, "BancoPraca", p, new Vector3(2.4f, 0.12f, 0.6f), CityPalette.Madeira, giro, false);
                CityPalette.Box(root.transform, "EncostoBanco", p + new Vector3(0f, 0.35f, 0f), new Vector3(2.4f, 0.6f, 0.1f), CityPalette.Madeira, giro, false);
                RegistrarAssento(banco.transform);
            }

            CityPalette.Label(_labels, "PRAÇA DA MATRIZ", praca + new Vector3(0f, 6f, -22f), new Color(1f, 0.9f, 0.6f), 0.6f);

            // Terminal Rodoviário
            Vector3 term = _layout.DistrictCenter("Rodoviaria");
            var terminal = new GameObject("TerminalRodoviario");
            terminal.transform.SetParent(root.transform, false);
            terminal.transform.position = new Vector3(term.x, 0f, term.z);
            CityPalette.Box(terminal.transform, "Saguao", new Vector3(0f, 5f, 0f), new Vector3(60f, 10f, 26f), CityPalette.Mat(new Color(0.78f, 0.76f, 0.70f)));
            CityPalette.Box(terminal.transform, "MarquiseNorte", new Vector3(0f, 6.2f, -20f), new Vector3(64f, 0.5f, 16f), CityPalette.Metal, 0f, false);
            for (int k = 0; k < 5; k++)
            {
                CityPalette.Cyl(terminal.transform, "Pilar", new Vector3(-26f + k * 13f, 3.1f, -26f), 0.7f, 6.2f, CityPalette.Concreto, collide: true);
                CityPalette.Box(terminal.transform, "Plataforma", new Vector3(-26f + k * 13f, 0.3f, -24f), new Vector3(11f, 0.5f, 4f), CityPalette.Calcada, 0f, false);
            }
            CityPalette.Label(_labels, "TERMINAL RODOVIÁRIO DE SÃO GENÉSIO", new Vector3(term.x, 11.6f, term.z - 13.5f), new Color(0.2f, 0.4f, 0.75f), 0.55f);

            // Caixa d'água municipal (marco visível de longe)
            Vector3 cx = _layout.DistrictCenter("MonteVerde");
            CityProps.CaixaDaguaGigante(root.transform, new Vector3(cx.x + 60f, 0f, cx.z + 40f));

            // Estádio de várzea da COHAB
            Vector3 cohab = _layout.DistrictCenter("Cohab");
            CityProps.CampoDeVarzea(root.transform, new Vector3(cohab.x, 0.18f, cohab.z), 70f, 45f);
            CityPalette.Box(root.transform, "Arquibancada", new Vector3(cohab.x, 1.2f, cohab.z + 27f), new Vector3(60f, 2.4f, 6f), CityPalette.Concreto);
            CityPalette.Label(_labels, "ESTÁDIO MUNICIPAL SEU OTACÍLIO", new Vector3(cohab.x, 5.5f, cohab.z + 30f), new Color(1f, 0.9f, 0.6f), 0.45f);

            StaticBatchingUtility.Combine(root);
        }

        // ==================================================================== mobiliário viário
        private void BuildMobiliarioViario()
        {
            var root = new GameObject("MobiliarioUrbano");
            root.transform.SetParent(_root, false);

            for (int i = 0; i < _layout.N; i++)
            for (int j = 0; j < _layout.N; j++)
            {
                Vector3 cross = _layout.Intersection(i, j);
                if (!_layout.IsDrivable(cross)) continue;

                float wi = _layout.HalfWidth(i) + 2.2f;
                float wj = _layout.HalfWidth(j) + 2.2f;

                // placa de rua na esquina
                CityProps.PlacaDeRua(root.transform, cross + new Vector3(wi, 0.15f, wj), _layout.StreetNameZ(j), _layout.StreetNameX(i));

                // semáforo nos cruzamentos de avenida
                if (_layout.IsAvenue(i) && _layout.IsAvenue(j))
                {
                    var sNS = CityProps.Semaforo(root.transform, cross + new Vector3(wi, 0.15f, -wj), 0f);
                    LuzesNorteSul.Add(CityProps.LuzSemaforo(sNS.transform));
                    var sLO = CityProps.Semaforo(root.transform, cross + new Vector3(-wi, 0.15f, wj), 90f);
                    LuzesLesteOeste.Add(CityProps.LuzSemaforo(sLO.transform));
                }

                // poste de luz alternado (não em toda esquina — senão vira árvore de natal)
                if ((i + j) % 2 == 0)
                {
                    var luz = CityProps.PosteDeLuz(root.transform, cross + new Vector3(-wi, 0.15f, -wj), Random.Range(0f, 360f), aceso: false);
                    if (luz != null) Luminarias.Add(luz);
                }

                // quebra-molas na entrada de rua comum vinda de avenida
                if (!_layout.IsAvenue(i) && _layout.IsAvenue(j) && Random.value < 0.35f)
                    CityProps.QuebraMolas(root.transform, cross + new Vector3(0f, 0f, wj + 6f), _layout.HalfWidth(i) * 2f, 0f);
            }

            // mobiliário espalhado pelas calçadas
            for (int i = 0; i < _layout.N - 1; i++)
            for (int j = 0; j < _layout.N - 1; j++)
            {
                Vector3 c = _layout.BlockCenter(i, j);
                if (!_layout.IsDrivable(c)) continue;
                Vector2 size = _layout.BlockSize(i, j);
                Vector3 borda = c + new Vector3(Random.Range(-size.x * 0.4f, size.x * 0.4f), 0.15f, -size.y * 0.5f - 1.4f);

                float r = Random.value;
                if      (r < 0.18f) CityProps.PontoDeOnibus(root.transform, borda, 0f);
                else if (r < 0.30f) CityProps.Orelhao(root.transform, borda, Random.Range(0f, 360f));
                else if (r < 0.42f) CityProps.BancaDeJornal(root.transform, borda, Random.Range(-15f, 15f));
                else if (r < 0.70f) CityProps.Lixeira(root.transform, borda);
                else if (r < 0.92f) CityProps.Arvore(root.transform, borda);
            }

            StaticBatchingUtility.Combine(root);
        }

        /// <summary>Recolhe os assentos que os props deixaram anotados (bancos de ponto de ônibus).</summary>
        private void DrenarAssentosDosProps()
        {
            foreach (var p in CityProps.AssentosPendentes)
            {
                var marca = new GameObject("Assento");
                marca.transform.SetParent(_labels, false);
                marca.transform.position = p;
                PlayerActions.Assentos.Add(marca.transform);
            }
            CityProps.AssentosPendentes.Clear();
        }

        /// <summary>
        /// Marca um objeto como assento. O ponto fica fora do batching (o transform precisa continuar
        /// consultável) e entra na lista estática que o <see cref="PlayerActions"/> varre.
        /// </summary>
        private void RegistrarAssento(Transform banco)
        {
            var marca = new GameObject("Assento");
            marca.transform.SetParent(_labels, false);      // root sem StaticBatching
            marca.transform.position = banco.position + Vector3.up * 0.1f;
            marca.transform.rotation = banco.rotation;
            PlayerActions.Assentos.Add(marca.transform);
        }

        // ==================================================================== buracos
        /// <summary>
        /// Espalha buracos pelas vias. A periferia leva mais (a COHAB, a Marginal e o Sítio ganham o
        /// dobro) — recapeamento é assunto de bairro nobre.
        /// </summary>
        private void BuildBuracos()
        {
            var root = new GameObject("Buracos");
            root.transform.SetParent(_root, false);

            int alvo = 70, criados = 0, tentativas = 0;
            while (criados < alvo && tentativas++ < alvo * 12)
            {
                Vector3 p = new Vector3(Random.Range(-_layout.Extent, _layout.Extent), 0f,
                                        Random.Range(-_layout.Extent, _layout.Extent));
                if (!_layout.TryNearestLanePoint(p, out var faixa, out _)) continue;
                if (!_layout.IsDrivable(faixa)) continue;

                string bairro = _layout.DistrictIdAt(faixa);
                bool periferia = bairro == "Cohab" || bairro == "Marginal" || bairro == "SitioCapim"
                              || bairro == "VistaAlegre" || bairro == "Rodoviaria";
                if (!periferia && Random.value < 0.6f) continue;   // no asfalto bom, é raro

                faixa.y = 0.02f;
                Buraco.Criar(root.transform, faixa, Random.Range(0.7f, 1.5f), Random.Range(0.5f, 1.2f));
                criados++;
            }
            Buracos = criados;
        }

        /// <summary>Quantos buracos a cidade ganhou nesta geração.</summary>
        public int Buracos { get; private set; }

        // ==================================================================== comércio
        private void SpawnComercio()
        {
            if (_catalogs == null || _catalogs.Shops == null || _catalogs.Shops.Count == 0) return;

            var root = new GameObject("Comercio");
            root.transform.SetParent(_root, false);

            var usados = new HashSet<string>();
            foreach (var dto in _catalogs.Shops)
            {
                Vector3 centro = string.IsNullOrEmpty(dto.bairro) ? Vector3.zero : _layout.DistrictCenter(dto.bairro);
                Vector3 pos = PosicaoLivreEmVia(centro, 120f, usados);
                var it = BuildEstabelecimento(root.transform, dto, pos);
                if (it != null) Shops.Add(it);
            }
            StaticBatchingUtility.Combine(root);
        }

        private Vector3 PosicaoLivreEmVia(Vector3 around, float raio, HashSet<string> usados)
        {
            for (int k = 0; k < 40; k++)
            {
                Vector2 r = Random.insideUnitCircle * raio;
                Vector3 p = new Vector3(around.x + r.x, 0f, around.z + r.y);
                if (!_layout.IsDrivable(p)) continue;

                // encosta na calçada da via mais próxima
                _layout.TryNearestLanePoint(p, out var lane, out var fwd);
                Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 spot = lane + side * -(CityLayout.AvenueHalfWidth + 6f);
                if (!_layout.IsDrivable(lane)) continue;

                string key = Mathf.RoundToInt(spot.x / 18f) + ":" + Mathf.RoundToInt(spot.z / 18f);
                if (usados.Contains(key)) continue;
                usados.Add(key);
                spot.y = 0f;
                return spot;
            }
            return around + new Vector3(Random.Range(-40f, 40f), 0f, Random.Range(-40f, 40f));
        }

        private Interactable BuildEstabelecimento(Transform parent, ShopDto dto, Vector3 pos)
        {
            if (!System.Enum.TryParse<TipoInteracao>(dto.tipo, true, out var tipo)) tipo = TipoInteracao.Barraca;
            Color cor = CityPalette.Parse(dto.corHex, Color.white);

            var go = new GameObject("Loja_" + dto.id);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            switch (tipo)
            {
                case TipoInteracao.Posto:    MontarPosto(go.transform, cor);            break;
                case TipoInteracao.Oficina:  MontarOficina(go.transform, cor);          break;
                case TipoInteracao.Trabalho: MontarPontoDeTrabalho(go.transform, cor);  break;
                case TipoInteracao.Barraca:  CityProps.BarracaCamelo(go.transform, Vector3.zero, 0f, cor); break;
                default:                     MontarLoja(go.transform, cor);             break;
            }

            CityPalette.Label(_labels, dto.nome.ToUpper(), pos + new Vector3(0f, 6.2f, -2.6f), cor, 0.30f);

            var it = go.AddComponent<Interactable>();
            it.tipo      = tipo;
            it.rotulo    = dto.nome;
            it.bordao    = dto.bordao;
            it.cor       = cor;
            it.radius    = tipo == TipoInteracao.Posto ? 8f : 6f;
            it.precoBase = dto.precoBase;
            it.pagamento = dto.pagamento;
            if (dto.itens != null) it.itens = new List<string>(dto.itens);
            if (tipo == TipoInteracao.Trabalho)
            {
                it.energiaCost   = 14f;
                it.fomeCost      = 10f;
                it.horasTrabalho = 2f;
            }
            return it;
        }

        private void MontarPosto(Transform p, Color cor)
        {
            CityPalette.Box(p, "Pista", new Vector3(0f, 0.1f, 0f), new Vector3(18f, 0.2f, 14f), CityPalette.Concreto, 0f, false);
            CityPalette.Box(p, "Cobertura", new Vector3(0f, 6.2f, 0f), new Vector3(18f, 0.6f, 14f), CityPalette.Mat(cor), 0f, false);
            for (int k = 0; k < 4; k++)
            {
                float sx = (k % 2 == 0) ? -7f : 7f;
                float sz = (k < 2) ? -5f : 5f;
                CityPalette.Cyl(p, "Coluna", new Vector3(sx, 3f, sz), 0.6f, 6f, CityPalette.Mat(Color.white), collide: true);
            }
            for (int k = 0; k < 2; k++)
            {
                CityPalette.Box(p, "Bomba", new Vector3(-3f + k * 6f, 1.1f, 0f), new Vector3(1.2f, 2.0f, 0.9f), CityPalette.Mat(Color.white));
                CityPalette.Box(p, "Visor", new Vector3(-3f + k * 6f, 1.7f, -0.5f), new Vector3(0.8f, 0.5f, 0.1f), CityPalette.MetalEscuro, 0f, false);
            }
            CityPalette.Box(p, "Loja", new Vector3(0f, 1.8f, 9f), new Vector3(10f, 3.6f, 5f), CityPalette.Mat(Color.white));
            CityPalette.Box(p, "Faixa", new Vector3(0f, 6.9f, 0f), new Vector3(18f, 0.8f, 14.2f), CityPalette.Mat(cor), 0f, false);
        }

        private void MontarOficina(Transform p, Color cor)
        {
            CityPalette.Box(p, "Galpao", new Vector3(0f, 2.2f, 2f), new Vector3(14f, 4.4f, 10f), CityPalette.Mat(CityPalette.Vary(cor, 0.05f)));
            CityPalette.Box(p, "PortaoRolo", new Vector3(0f, 1.9f, -3.1f), new Vector3(7f, 3.8f, 0.25f), CityPalette.Metal, 0f, false);
            CityPalette.Box(p, "Toldo", new Vector3(0f, 4.6f, -4.5f), new Vector3(14f, 0.15f, 3f), CityPalette.Metal, 0f, false);
            // pneus empilhados e um carro no macaco: cenário obrigatório de oficina
            for (int k = 0; k < 5; k++)
                CityPalette.Cyl(p, "Pneu", new Vector3(-5.5f + (k % 2) * 1.4f, 0.25f + k * 0.42f, -5f), 1.3f, 0.4f, CityPalette.Mat(new Color(0.12f, 0.12f, 0.13f)));
            CityPalette.Box(p, "CarroDesmontado", new Vector3(4.5f, 0.7f, -5.5f), new Vector3(1.8f, 0.9f, 4f), CityPalette.Mat(new Color(0.45f, 0.45f, 0.48f)));
        }

        private void MontarPontoDeTrabalho(Transform p, Color cor)
        {
            CityPalette.Box(p, "Container", new Vector3(0f, 1.4f, 0f), new Vector3(6f, 2.8f, 2.6f), CityPalette.Mat(cor));
            CityPalette.Box(p, "Placa", new Vector3(0f, 3.4f, -1.4f), new Vector3(5f, 1.0f, 0.15f), CityPalette.Mat(Color.white), 0f, false);
            for (int k = 0; k < 6; k++)
                CityPalette.Box(p, "Caixa", new Vector3(Random.Range(-4f, 4f), 0.4f, Random.Range(2f, 5f)),
                                new Vector3(1.2f, 0.8f, 1.2f), CityPalette.Madeira);
            CityPalette.Box(p, "Cone", new Vector3(-4f, 0.4f, -2.5f), new Vector3(0.6f, 0.8f, 0.6f), CityPalette.Mat(new Color(0.95f, 0.45f, 0.10f)), 0f, false);
        }

        private void MontarLoja(Transform p, Color cor)
        {
            float h = Random.Range(4.0f, 5.5f);
            CityPalette.Box(p, "Fachada", new Vector3(0f, h * 0.5f, 1.5f), new Vector3(11f, h, 7f), CityPalette.Mat(CityPalette.Vary(cor, 0.06f)));
            CityPalette.Box(p, "Vitrine", new Vector3(0f, 1.6f, -2.1f), new Vector3(8f, 2.6f, 0.2f), CityPalette.Vidro, 0f, false);
            CityPalette.Box(p, "Toldo",   new Vector3(0f, 3.4f, -3.2f), new Vector3(10f, 0.15f, 2.2f), CityPalette.Mat(cor), 0f, false);
            CityPalette.Box(p, "Letreiro",new Vector3(0f, h + 0.6f, -1.9f), new Vector3(9f, 1.2f, 0.25f), CityPalette.Mat(cor), 0f, false);
            CityProps.CoberturaDeLaje(p, new Vector3(0f, h, 1.5f), 5f, 3f);
        }

        // ==================================================================== spawn
        private void DefinirSpawnDoJogador()
        {
            // na calçada da Praça da Matriz, olhando pra igreja
            Vector3 praca = _layout.BlockCenter(_layout.N / 2, _layout.N / 2);
            PlayerSpawn = praca + new Vector3(0f, 1.2f, -30f);
        }
    }
}
