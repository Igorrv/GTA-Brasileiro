using System.Collections.Generic;
using Caos.Data;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Malha viária de São Genésio do Caos — <b>dados puros</b> (sem GameObject), compartilhados por
    /// <see cref="CityGenerator"/> (geometria), <see cref="TrafficSystem"/>/<see cref="PedestrianSystem"/>/
    /// <see cref="PoliceSystem"/> (navegação) e HUD/minimapa (nome de rua e bairro).
    ///
    /// Modelo: grade de <c>N</c> linhas por eixo, espaçadas de <see cref="Cell"/>. Uma a cada 4 é
    /// <b>avenida</b> (mais larga, com faixa dupla amarela); as outras são ruas. Entre duas linhas fica
    /// um <b>quarteirão</b> (calçada + lotes). A área do morro não é dirigível (viela/escadaria) — igual
    /// à cidade de verdade.
    /// </summary>
    public sealed class CityLayout
    {
        // ---- métricas (m) ----
        public const float Cell            = 74f;   // distância entre eixos de via
        public const float StreetHalfWidth = 7f;    // rua  = 14 m
        public const float AvenueHalfWidth = 11f;   // av.  = 22 m
        public const float SidewalkWidth   = 3.2f;
        public const int   AvenueEvery     = 4;

        public readonly int   N;        // linhas por eixo
        public readonly float Origin;   // coordenada da linha 0
        public readonly float Extent;   // meio-lado da cidade

        // morro da Vista Alegre: dentro desse raio o carro não sobe (só viela e escadaria)
        public Vector3 MorroCenter { get; private set; } = new Vector3(270f, 0f, 250f);
        public float   MorroRadius { get; private set; } = 108f;
        public float   MorroPeak   { get; private set; } = 26f;

        // praia: tudo ao sul dessa linha é areia/mar
        public float BeachZ { get; private set; } = -430f;
        public float SeaZ   { get; private set; } = -520f;

        // rio da Marginal: faixa vertical a oeste
        public float RiverX      { get; private set; } = -455f;
        public float RiverHalfW  { get; private set; } = 26f;

        private readonly List<DistrictDto> _districts = new List<DistrictDto>();
        private readonly string[] _nameX;   // nome da via por linha (eixo X = vias norte-sul)
        private readonly string[] _nameZ;

        public CityLayout(int n, GameCatalogs catalogs)
        {
            N      = Mathf.Max(5, n | 1);            // ímpar: existe linha central passando na origem
            Origin = -(N - 1) * 0.5f * Cell;
            Extent = Mathf.Abs(Origin) + Cell * 0.5f;

            if (catalogs != null && catalogs.Districts != null)
                _districts.AddRange(catalogs.Districts);

            _nameX = new string[N];
            _nameZ = new string[N];
            NameStreets(catalogs != null ? catalogs.Streets : null);
        }

        // ------------------------------------------------------------------ vias
        public bool  IsAvenue(int line)   => line % AvenueEvery == 0;
        public float HalfWidth(int line)  => IsAvenue(line) ? AvenueHalfWidth : StreetHalfWidth;
        public float LinePos(int line)    => Origin + line * Cell;

        /// <summary>Índice de linha mais próximo de uma coordenada (eixo único).</summary>
        public int NearestLine(float coord) => Mathf.Clamp(Mathf.RoundToInt((coord - Origin) / Cell), 0, N - 1);

        /// <summary>Centro do quarteirão entre as linhas (i, i+1) x (j, j+1).</summary>
        public Vector3 BlockCenter(int i, int j)
        {
            float x = (LinePos(i) + HalfWidth(i) + LinePos(i + 1) - HalfWidth(i + 1)) * 0.5f;
            float z = (LinePos(j) + HalfWidth(j) + LinePos(j + 1) - HalfWidth(j + 1)) * 0.5f;
            return new Vector3(x, 0f, z);
        }

        public Vector2 BlockSize(int i, int j)
        {
            float w = (LinePos(i + 1) - HalfWidth(i + 1)) - (LinePos(i) + HalfWidth(i));
            float d = (LinePos(j + 1) - HalfWidth(j + 1)) - (LinePos(j) + HalfWidth(j));
            return new Vector2(w, d);
        }

        public Vector3 Intersection(int i, int j) => new Vector3(LinePos(i), 0f, LinePos(j));

        /// <summary>Ponto na pista da direita da via mais próxima, com a direção do fluxo.</summary>
        public bool TryNearestLanePoint(Vector3 p, out Vector3 point, out Vector3 forward)
        {
            int li = NearestLine(p.x);
            int lj = NearestLine(p.z);
            float dx = Mathf.Abs(p.x - LinePos(li));
            float dz = Mathf.Abs(p.z - LinePos(lj));

            if (dx <= dz)   // via norte-sul (varia em Z)
            {
                float dir  = p.z >= 0f ? 1f : -1f;                    // mantém o sentido de quem chega
                float lane = LaneOffset(li) * (dir > 0f ? 1f : -1f);
                point   = new Vector3(LinePos(li) + lane, 0f, Mathf.Clamp(p.z, -Extent, Extent));
                forward = new Vector3(0f, 0f, dir);
            }
            else            // via leste-oeste (varia em X)
            {
                float dir  = p.x >= 0f ? 1f : -1f;
                float lane = LaneOffset(lj) * (dir > 0f ? -1f : 1f);
                point   = new Vector3(Mathf.Clamp(p.x, -Extent, Extent), 0f, LinePos(lj) + lane);
                forward = new Vector3(dir, 0f, 0f);
            }
            return true;
        }

        /// <summary>Meia-pista: onde fica o centro da faixa de rolamento (mão da direita).</summary>
        public float LaneOffset(int line) => HalfWidth(line) * 0.5f;

        /// <summary>Distância até o eixo de via mais próximo — usado para saber se está "na rua".</summary>
        public float DistanceToRoad(Vector3 p)
        {
            float dx = Mathf.Abs(p.x - LinePos(NearestLine(p.x)));
            float dz = Mathf.Abs(p.z - LinePos(NearestLine(p.z)));
            return Mathf.Min(dx, dz);
        }

        public bool IsOnRoad(Vector3 p) => DistanceToRoad(p) <= AvenueHalfWidth;

        /// <summary>Falso no morro (carro não sobe), na água e fora da cidade.</summary>
        public bool IsDrivable(Vector3 p)
        {
            if (Mathf.Abs(p.x) > Extent || Mathf.Abs(p.z) > Extent) return false;
            if (p.z < BeachZ) return false;
            if (Mathf.Abs(p.x - RiverX) < RiverHalfW) return false;
            Vector3 d = p - MorroCenter; d.y = 0f;
            return d.sqrMagnitude > MorroRadius * MorroRadius;
        }

        /// <summary>Altura do terreno: 0 na cidade, sobe no morro, desce na areia/mar.</summary>
        public float TerrainHeight(Vector3 p)
        {
            Vector3 d = p - MorroCenter; d.y = 0f;
            float dist = d.magnitude;
            if (dist < MorroRadius)
            {
                float t = 1f - dist / MorroRadius;
                return MorroPeak * t * t * (3f - 2f * t) * 0.5f;   // smoothstep suave
            }
            if (p.z < BeachZ) return -0.4f;
            return 0f;
        }

        /// <summary>Ponto de spawn dirigível numa via aleatória, a até <paramref name="radius"/> do centro dado.</summary>
        public Vector3 RandomDrivablePoint(Vector3 around, float radius, int tries = 24)
        {
            for (int k = 0; k < tries; k++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 candidate = new Vector3(around.x + r.x, 0f, around.z + r.y);
                if (!TryNearestLanePoint(candidate, out var pt, out _)) continue;
                if (IsDrivable(pt)) return pt;
            }
            return new Vector3(LinePos(N / 2) + LaneOffset(N / 2), 0f, 0f);
        }

        // ------------------------------------------------------------------ rota (GPS)
        /// <summary>
        /// Traça o caminho de <paramref name="origem"/> até <paramref name="destino"/> <b>pelas vias</b>,
        /// no formato que um GPS desenharia: sai para a via mais próxima, segue por ela até o cruzamento
        /// alinhado com o destino, dobra a esquina e chega.
        ///
        /// Numa cidade em grade não é preciso A*: o caminho ótimo em quarteirões é o "L", e a escolha
        /// que importa é <b>qual perna do L vem primeiro</b> — aqui vem a mais longa, que é como o
        /// motorista pensa (pega a avenida antes de entrar na rua). Se o cotovelo cair em cima do morro
        /// ou do rio, tenta o L invertido.
        /// </summary>
        public List<Vector3> CalcularRota(Vector3 origem, Vector3 destino)
        {
            var rota = new List<Vector3>(4);

            TryNearestLanePoint(origem, out var partida, out _);
            TryNearestLanePoint(destino, out var chegada, out _);

            int linhaOrigemX = NearestLine(partida.x), linhaOrigemZ = NearestLine(partida.z);
            int linhaDestX   = NearestLine(chegada.x), linhaDestZ   = NearestLine(chegada.z);

            // dois cotovelos possíveis: virar em X primeiro, ou em Z primeiro
            Vector3 cotoveloA = new Vector3(LinePos(linhaDestX),   0f, LinePos(linhaOrigemZ));
            Vector3 cotoveloB = new Vector3(LinePos(linhaOrigemX), 0f, LinePos(linhaDestZ));

            bool aOk = IsDrivable(cotoveloA);
            bool bOk = IsDrivable(cotoveloB);

            Vector3 cotovelo;
            if (aOk && bOk)
            {
                // pega o L cuja primeira perna é a mais longa (avenida antes da rua)
                float pernaA = Mathf.Abs(cotoveloA.x - partida.x);
                float pernaB = Mathf.Abs(cotoveloB.z - partida.z);
                cotovelo = pernaA >= pernaB ? cotoveloA : cotoveloB;
            }
            else if (aOk) cotovelo = cotoveloA;
            else if (bOk) cotovelo = cotoveloB;
            else          cotovelo = (cotoveloA + cotoveloB) * 0.5f;   // último recurso

            rota.Add(origem);
            rota.Add(partida);
            if ((cotovelo - partida).sqrMagnitude > 25f && (cotovelo - chegada).sqrMagnitude > 25f)
                rota.Add(cotovelo);
            rota.Add(chegada);
            rota.Add(destino);
            return rota;
        }

        /// <summary>Comprimento total de uma rota, em metros — o HUD mostra como distância do trajeto.</summary>
        public static float ComprimentoDaRota(List<Vector3> rota)
        {
            float total = 0f;
            for (int i = 1; i < rota.Count; i++) total += Vector3.Distance(rota[i - 1], rota[i]);
            return total;
        }

        // ------------------------------------------------------------------ bairros
        /// <summary>Bairro cujo centro está mais perto do ponto (id do enum <see cref="DistrictId"/>).</summary>
        public string DistrictIdAt(Vector3 p)
        {
            string best = "Centro";
            float bestD = float.MaxValue;
            for (int i = 0; i < _districts.Count; i++)
            {
                var d = _districts[i];
                float dx = p.x - d.centroX, dz = p.z - d.centroZ;
                float sq = dx * dx + dz * dz;
                if (sq < bestD) { bestD = sq; best = d.id; }
            }
            return best;
        }

        public DistrictDto DistrictAt(Vector3 p)
        {
            string id = DistrictIdAt(p);
            for (int i = 0; i < _districts.Count; i++)
                if (_districts[i].id == id) return _districts[i];
            return null;
        }

        public DistrictDto DistrictById(string id)
        {
            for (int i = 0; i < _districts.Count; i++)
                if (_districts[i].id == id) return _districts[i];
            return null;
        }

        /// <summary>Centro do bairro no mundo (usado por missões, lojas e beacons).</summary>
        public Vector3 DistrictCenter(string id)
        {
            var d = DistrictById(id);
            return d != null ? new Vector3(d.centroX, 0f, d.centroZ) : Vector3.zero;
        }

        public IReadOnlyList<DistrictDto> Districts => _districts;

        // ------------------------------------------------------------------ nomes de rua
        private void NameStreets(StreetNamesDto names)
        {
            var avenidas = (names != null && names.avenidas != null && names.avenidas.Count > 0)
                ? names.avenidas : new List<string> { "Av. Brasil", "Av. Getúlio Vargas", "Av. das Nações", "Av. do Contorno" };
            var ruas = (names != null && names.ruas != null && names.ruas.Count > 0)
                ? names.ruas : new List<string> { "R. XV de Novembro", "R. do Comércio", "R. das Flores", "R. Projetada A" };

            int ai = 0, ri = 0;
            for (int i = 0; i < N; i++)
            {
                if (IsAvenue(i)) { _nameX[i] = avenidas[ai++ % avenidas.Count]; }
                else             { _nameX[i] = ruas[ri++ % ruas.Count]; }
            }
            for (int j = 0; j < N; j++)
            {
                if (IsAvenue(j)) { _nameZ[j] = avenidas[ai++ % avenidas.Count]; }
                else             { _nameZ[j] = ruas[ri++ % ruas.Count]; }
            }
        }

        public string StreetNameX(int line) => _nameX[Mathf.Clamp(line, 0, N - 1)];
        public string StreetNameZ(int line) => _nameZ[Mathf.Clamp(line, 0, N - 1)];

        /// <summary>Logradouro em que o ponto está (a via mais próxima), para o HUD.</summary>
        public string StreetNameAt(Vector3 p)
        {
            int li = NearestLine(p.x);
            int lj = NearestLine(p.z);
            float dx = Mathf.Abs(p.x - LinePos(li));
            float dz = Mathf.Abs(p.z - LinePos(lj));

            Vector3 d = p - MorroCenter; d.y = 0f;
            if (d.sqrMagnitude < MorroRadius * MorroRadius) return "Ladeira do Cruzeiro";
            if (p.z < BeachZ) return "Praia de Itaúna";

            return dx <= dz ? _nameX[li] : _nameZ[lj];
        }
    }
}
