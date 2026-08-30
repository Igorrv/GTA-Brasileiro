using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.World;

namespace Caos.Gameplay
{
    /// <summary>
    /// Spawn de eventos aleatórios segundo docs/06 §H:
    /// P = P_base(bairro) × f_Caos × f_horário × (filtros de clima/evento).
    /// No scaffold, resolve automaticamente uma opção (a UI de escolha virá depois).
    /// </summary>
    public sealed class EventSystem : ITickable
    {
        private readonly WorldStateService _world;
        private readonly TimeOfDayService _time;
        private readonly GameCatalogs _catalogs;
        private readonly ImpactResolver _impact;
        private readonly IRandomSource _rng;

        private float _spawnAccum;
        private const float kSpawnInterval = 30f; // a cada ~30s de jogo
        private const int kMaxActive = 1;         // low-end (docs/06 §H)
        private int _active;

        private readonly Dictionary<string, float> _cooldown = new Dictionary<string, float>();

        // Reaproveitados entre ticks: este serviço roda todo frame e não pode alocar por frame.
        private readonly List<string> _cooldownKeys = new List<string>();
        private readonly List<EventDto> _eligible = new List<EventDto>();

        public int Order => 30;

        /// <param name="rng">
        /// Fluxo de sorteio do sistema de eventos. Omitido, cria um fluxo próprio — o importante é não
        /// ser o mesmo fluxo global que a geração da cidade semeia (ver <see cref="IRandomSource"/>).
        /// </param>
        public EventSystem(WorldStateService world, TimeOfDayService time, GameCatalogs catalogs, ImpactResolver impact,
                           IRandomSource rng = null)
        {
            _world = world; _time = time; _catalogs = catalogs; _impact = impact;
            _rng = rng ?? new CaosRandom(unchecked(System.Environment.TickCount * 31 + 7));
        }

        public void Tick(float dt)
        {
            // tick cooldowns
            _cooldownKeys.Clear();
            foreach (var k in _cooldown.Keys) _cooldownKeys.Add(k);
            for (int i = 0; i < _cooldownKeys.Count; i++)
            {
                string k = _cooldownKeys[i];
                float restante = _cooldown[k] - dt;
                if (restante <= 0f) _cooldown.Remove(k);
                else _cooldown[k] = restante;
            }

            _spawnAccum += dt;
            if (_spawnAccum < kSpawnInterval) return;
            _spawnAccum = 0f;
            if (_active >= kMaxActive) return;

            TrySpawn();
        }

        private void TrySpawn()
        {
            float probBase = DistrictProb(_world.CurrentDistrict);
            float p = probBase * _world.ChaosFactor * HorarioFactor(_time.Fase);
            if (_rng.Valor01() > p) return;

            // filtra eventos elegíveis
            _eligible.Clear();
            foreach (var e in _catalogs.Events)
            {
                if (_cooldown.ContainsKey(e.id)) continue;
                if (!MatchesFilter(e.bairros, _world.CurrentDistrict.ToString())) continue;
                if (!MatchesFilter(e.horarios, _time.Fase)) continue;
                if (!MatchesFilter(e.climas, _world.Weather.ToString())) continue;
                _eligible.Add(e);
            }
            if (_eligible.Count == 0) return;

            var ev = _eligible[_rng.Intervalo(0, _eligible.Count)];
            Resolve(ev);
        }

        private void Resolve(EventDto ev)
        {
            var opcoes = ev.opcoes;
            if (opcoes == null || opcoes.Count == 0) return;

            int idx = _rng.Intervalo(0, opcoes.Count);
            var opt = opcoes[idx];
            _impact.Apply(opt.impacto);

            _active++;
            _cooldown[ev.id] = 60f; // cooldown 60s por tipo

            EventBus<EventoDisparado>.Publish(new EventoDisparado
            { id = ev.id, nome = ev.nome, opcao = opt.rotulo, impacto = opt.impacto });

            CaosLog.Info($"[Evento] {ev.id} '{ev.nome}' — escolha: \"{opt.rotulo}\".");
            // libera "slot" após resolução (scaffold: imediato)
            _active = System.Math.Max(0, _active - 1);
        }

        private float DistrictProb(DistrictId d)
        {
            foreach (var dist in _catalogs.Districts)
                if (dist.id == d.ToString()) return dist.probEventoBase;
            return 0.25f;
        }

        private static float HorarioFactor(string fase) =>
            fase == "manha" ? 1.3f :
            fase == "tarde" ? 1.3f :
            fase == "noite" ? 1.3f :
            fase == "madrugada" ? 1.2f : 0.8f;

        private static bool MatchesFilter(List<string> filter, string value) =>
            filter == null || filter.Count == 0 || filter.Contains(value);
    }
}
