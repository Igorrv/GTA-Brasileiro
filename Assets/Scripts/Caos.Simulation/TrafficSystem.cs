using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.World;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Trânsito de São Genésio: os carros andam <b>na mão da direita das vias</b> do
    /// <see cref="CityLayout"/>, param no sinal vermelho, respeitam o carro da frente e convertem nas
    /// esquinas. Nada de pathfinding — é aritmética de grade, custo O(n) com n ≈ 16 (docs/12 §12.5).
    ///
    /// A frota sai do catálogo com peso por raridade: enche de Uno/Gol/Pálio e raramente passa uma
    /// jamanta. A densidade acompanha a hora do dia (<see cref="TimeOfDayService.Trafego"/>): rush de
    /// manhã e no fim da tarde, rua vazia de madrugada.
    ///
    /// Rigidbody kinematic: não entra no solver, mas ainda colide com o carro do jogador — é assim que
    /// a batida vira dano (<see cref="VehicleHealth"/>) e crime (<see cref="CrimeSystem"/>).
    /// </summary>
    public class TrafficSystem : MonoBehaviour
    {
        [SerializeField] private int   maxActive     = 26;   // rua cheia: densidade de cidade, não de maquete
        [SerializeField] private float recycleRadius = 210f;
        [SerializeField] private float spawnRadius   = 170f;
        [SerializeField] private float spawnInterval = 0.4f;

        private const float kVelRua      = 15f;   // ~54 km/h
        private const float kVelAvenida  = 21f;   // ~76 km/h
        private const float kDistFrente  = 13f;   // freia pelo carro da frente
        private const float kDistSinal   = 11f;   // começa a parar no sinal
        private const float kFaseVerde   = 13f;   // segundos por fase do semáforo

        /// <summary>Fase atual do semáforo (lida pelos carros e pintada nas lâmpadas).</summary>
        public static bool VerdeNorteSul { get; private set; } = true;

        private Transform             _player;
        private GameCatalogs          _catalogs;
        private CityLayout            _layout;
        private TimeOfDayService      _time;
        private ObjectPool<TrafficCar> _pool;
        private readonly List<TrafficCar> _active = new List<TrafficCar>();
        private float _spawnAccum, _faseAccum;

        public void Init(Transform player, GameCatalogs catalogs)
        {
            _player   = player;
            _catalogs = catalogs;
            _layout   = CityRuntime.Layout;
            ServiceLocator.TryGet(out _time);

            _pool = new ObjectPool<TrafficCar>(Factory, prewarm: maxActive);
            Fill();
            PintarSemaforos();
        }

        // ------------------------------------------------------------------ fábrica
        private TrafficCar Factory()
        {
            var dto = VehicleFactory.SortearParaTrafego(_catalogs, "Viatura", "Emergencia", "Bicicleta");

            var go = new GameObject("Trafego_" + (dto != null ? dto.id : "generico"));
            var col = VehicleFactory.BuildBody(go.transform, dto,
                        Random.value < 0.55f ? VehicleFactory.CorDe(dto) : CityPalette.CorViva(),
                        rodasVisuais: true);
            if (col != null) col.isTrigger = false;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // gente ao volante: sem isso o trânsito é carcaça vazia e o roubo não faz sentido
            VehicleFactory.Condutor(go.transform, dto);

            var car = go.AddComponent<TrafficCar>();
            car.dto = dto;
            car.ehMoto = dto != null && (dto.classe == "Moto" || dto.carroceria == "Moto");
            car.cruzeiro = dto != null && dto.velMaxKmh > 20f ? Mathf.Min(dto.velMaxKmh / 3.6f, kVelAvenida) : kVelRua;
            go.SetActive(false);
            return car;
        }

        // ------------------------------------------------------------------ loop
        private void Update()
        {
            if (_player == null || _pool == null || _layout == null) return;
            float dt = Time.deltaTime;

            // ciclo do semáforo
            _faseAccum += dt;
            if (_faseAccum >= kFaseVerde)
            {
                _faseAccum = 0f;
                VerdeNorteSul = !VerdeNorteSul;
                PintarSemaforos();
            }

            Vector3 ppos = _player.position;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var c = _active[i];
                if (c == null) { _active.RemoveAt(i); continue; }

                Dirigir(c, dt);

                if ((c.transform.position - ppos).sqrMagnitude > recycleRadius * recycleRadius ||
                    !_layout.IsDrivable(c.transform.position))
                {
                    _pool.Release(c);
                    _active.RemoveAt(i);
                }
            }

            _spawnAccum += dt;
            if (_spawnAccum >= spawnInterval) { _spawnAccum = 0f; Fill(); }
        }

        private void Dirigir(TrafficCar c, float dt)
        {
            Vector3 fwd = c.Forward;
            float alvo = c.cruzeiro;

            bool sinal = SinalFechadoAFrente(c);
            float distFrente = DistanciaCarroAFrente(c);

            if (sinal) alvo = 0f;
            else if (distFrente < kDistFrente)
            {
                // não é liga-desliga: acompanha o carro da frente proporcionalmente à distância.
                // É o que gera o sanfona do trânsito real em vez de carros parando na hora.
                alvo = Mathf.Lerp(0f, c.cruzeiro, Mathf.InverseLerp(3f, kDistFrente, distFrente));
                c.paciencia += dt;
                if (c.paciencia > 2.5f && !c.buzinou) { c.buzinou = true; c.paciencia = 0f; }
            }
            else { c.paciencia = 0f; c.buzinou = false; }

            c.velocidade = Mathf.MoveTowards(c.velocidade, alvo, (alvo > c.velocidade ? 6f : 16f) * dt);
            c.transform.position += fwd * c.velocidade * dt;
            c.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

            // conversão na esquina
            float along = c.eixo == 0 ? c.transform.position.z : c.transform.position.x;
            int cruz = _layout.NearestLine(along);
            if (Mathf.Abs(along - _layout.LinePos(cruz)) < 1.5f)
            {
                if (cruz != c.ultimaEsquina)
                {
                    c.ultimaEsquina = cruz;
                    if (Random.value < 0.28f) Converter(c, cruz);
                }
            }
        }

        private void Converter(TrafficCar c, int novaLinha)
        {
            int novoEixo = 1 - c.eixo;
            float novaDir = Random.value < 0.5f ? 1f : -1f;

            // a via em que ele entra precisa continuar dirigível (nada de subir o morro nem cair no rio)
            Vector3 teste = PosicaoNaFaixa(novoEixo, novaLinha, novaDir,
                                           novoEixo == 0 ? c.transform.position.z : c.transform.position.x);
            teste += (novoEixo == 0 ? Vector3.forward : Vector3.right) * novaDir * 20f;
            if (!_layout.IsDrivable(teste)) return;

            float cross = c.eixo == 0 ? c.transform.position.x : c.transform.position.z;
            int linhaAtual = _layout.NearestLine(cross);

            c.eixo  = novoEixo;
            c.linha = novaLinha;
            c.dir   = novaDir;
            c.transform.position = PosicaoNaFaixa(novoEixo, novaLinha, novaDir, _layout.LinePos(linhaAtual));
            c.velocidade *= 0.55f;   // reduz pra fazer a curva
        }

        /// <summary>Posição na faixa: <paramref name="along"/> é a coordenada ao longo da via.</summary>
        private Vector3 PosicaoNaFaixa(int eixo, int linha, float dir, float along)
        {
            float off = _layout.LaneOffset(linha);
            if (eixo == 0) // via norte-sul (anda em Z)
                return new Vector3(_layout.LinePos(linha) + off * dir, 0.05f, along);
            return new Vector3(along, 0.05f, _layout.LinePos(linha) - off * dir);
        }

        private bool SinalFechadoAFrente(TrafficCar c)
        {
            if (!_layout.IsAvenue(c.linha)) return false;           // só avenida tem semáforo
            bool meuVerde = c.eixo == 0 ? VerdeNorteSul : !VerdeNorteSul;
            if (meuVerde) return false;

            float along = c.eixo == 0 ? c.transform.position.z : c.transform.position.x;
            int prox = c.dir > 0f ? Mathf.CeilToInt((along - _layout.Origin) / CityLayout.Cell)
                                  : Mathf.FloorToInt((along - _layout.Origin) / CityLayout.Cell);
            prox = Mathf.Clamp(prox, 0, _layout.N - 1);
            if (!_layout.IsAvenue(prox)) return false;              // cruzamento sem semáforo

            float dist = (_layout.LinePos(prox) - along) * c.dir;
            return dist > CityLayout.AvenueHalfWidth && dist < kDistSinal + CityLayout.AvenueHalfWidth;
        }

        /// <summary>Distância até o carro da frente na mesma faixa (infinito se a via está livre).</summary>
        private float DistanciaCarroAFrente(TrafficCar c)
        {
            Vector3 p = c.transform.position;
            float menor = float.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                var o = _active[i];
                if (o == null || o == c) continue;
                if (o.eixo != c.eixo || o.linha != c.linha || !Mathf.Approximately(o.dir, c.dir)) continue;

                Vector3 d = o.transform.position - p;
                float ahead = c.eixo == 0 ? d.z * c.dir : d.x * c.dir;
                if (ahead > 0.5f && ahead < menor) menor = ahead;
            }

            // o carro do jogador também é obstáculo — parar no farol atrás dele é meio caminho do realismo
            if (_player != null)
            {
                Vector3 d = _player.position - p;
                float lateral = c.eixo == 0 ? Mathf.Abs(d.x) : Mathf.Abs(d.z);
                float ahead   = c.eixo == 0 ? d.z * c.dir : d.x * c.dir;
                if (lateral < 3f && ahead > 0.5f && ahead < menor) menor = ahead;
            }
            return menor;
        }

        // ------------------------------------------------------------------ spawn
        private void Fill()
        {
            int desejado = maxActive;
            if (_time != null) desejado = Mathf.Max(3, Mathf.RoundToInt(maxActive * _time.Trafego));

            while (_active.Count < desejado && _pool.InPool > 0)
            {
                var c = _pool.Get();
                if (!Posicionar(c)) { _pool.Release(c); break; }
                _active.Add(c);
            }
            while (_active.Count > desejado)
            {
                var c = _active[_active.Count - 1];
                _active.RemoveAt(_active.Count - 1);
                _pool.Release(c);
            }
        }

        private bool Posicionar(TrafficCar c)
        {
            for (int k = 0; k < 20; k++)
            {
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.55f, spawnRadius);
                Vector3 alvo = _player.position + new Vector3(r.x, 0f, r.y);

                int eixo  = Random.value < 0.5f ? 0 : 1;
                int linha = _layout.NearestLine(eixo == 0 ? alvo.x : alvo.z);
                float dir = Random.value < 0.5f ? 1f : -1f;

                // ônibus, caminhão e van só circulam em avenida — rua estreita de bairro não
                // comporta um busão de 12 m, e ver um passando na viela quebra a ilusão na hora
                bool grande = c.dto != null && (c.dto.classe == "Onibus" || c.dto.classe == "Caminhao" || c.dto.classe == "Van");
                if (grande && !_layout.IsAvenue(linha)) continue;
                float along = Mathf.Clamp(eixo == 0 ? alvo.z : alvo.x, -_layout.Extent + 20f, _layout.Extent - 20f);

                Vector3 pos = PosicaoNaFaixa(eixo, linha, dir, along);
                if (!_layout.IsDrivable(pos)) continue;
                if ((pos - _player.position).sqrMagnitude < 40f * 40f) continue;   // não nasce na cara do jogador

                c.eixo = eixo; c.linha = linha; c.dir = dir;
                c.ultimaEsquina = -1;
                c.velocidade = c.cruzeiro * 0.7f;
                c.transform.position = pos;
                c.transform.rotation = Quaternion.LookRotation(c.Forward, Vector3.up);
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ semáforos
        private void PintarSemaforos()
        {
            var gen = CityRuntime.Generator;
            if (gen == null) return;
            var verde   = CityPalette.Mat(new Color(0.15f, 0.85f, 0.25f));
            var vermelho= CityPalette.Mat(new Color(0.90f, 0.15f, 0.15f));

            for (int i = 0; i < gen.LuzesNorteSul.Count; i++)
                if (gen.LuzesNorteSul[i] != null) gen.LuzesNorteSul[i].sharedMaterial = VerdeNorteSul ? verde : vermelho;
            for (int i = 0; i < gen.LuzesLesteOeste.Count; i++)
                if (gen.LuzesLesteOeste[i] != null) gen.LuzesLesteOeste[i].sharedMaterial = VerdeNorteSul ? vermelho : verde;
        }
    }

    /// <summary>Estado de um carro do tráfego: em que via está, para que lado e a que velocidade.</summary>
    public class TrafficCar : MonoBehaviour
    {
        [HideInInspector] public VehicleDto dto;
        [HideInInspector] public int   eixo;          // 0 = anda em Z (via norte-sul) · 1 = anda em X
        [HideInInspector] public int   linha;         // índice da via no CityLayout
        [HideInInspector] public float dir = 1f;      // +1 / −1
        [HideInInspector] public float velocidade;
        [HideInInspector] public float cruzeiro = 14f;
        [HideInInspector] public int   ultimaEsquina = -1;
        [HideInInspector] public float paciencia;    // quanto tempo preso atrás de alguém
        [HideInInspector] public bool  buzinou;
        [HideInInspector] public bool  ehMoto;       // muda o gesto do roubo: puxar o piloto, não abrir porta

        public Vector3 Forward => eixo == 0 ? new Vector3(0f, 0f, dir) : new Vector3(dir, 0f, 0f);
    }
}
