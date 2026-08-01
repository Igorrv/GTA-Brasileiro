using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.World;
using UnityEngine;

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

        private float _spawnAccum;
        private const float kSpawnInterval = 30f; // a cada ~30s de jogo
        private const int kMaxActive = 1;         // low-end (docs/06 §H)
        private int _active;

        private readonly Dictionary<string, float> _cooldown = new Dictionary<string, float>();

        public int Order => 30;

        public EventSystem(WorldStateService world, TimeOfDayService time, GameCatalogs catalogs, ImpactResolver impact)
        {
            _world = world; _time = time; _catalogs = catalogs; _impact = impact;
        }

        public void Tick(float dt)
        {
            // tick cooldowns
            var keys = new List<string>(_cooldown.Keys);
            foreach (var k in keys)
            {
                _cooldown[k] -= dt;
                if (_cooldown[k] <= 0f) _cooldown.Remove(k);
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
            if (Random.value > p) return;

            // filtra eventos elegíveis
            var eligible = new List<EventDto>();
            foreach (var e in _catalogs.Events)
            {
                if (_cooldown.ContainsKey(e.id)) continue;
                if (!MatchesFilter(e.bairros, _world.CurrentDistrict.ToString())) continue;
                if (!MatchesFilter(e.horarios, _time.Fase)) continue;
                if (!MatchesFilter(e.climas, _world.Weather.ToString())) continue;
                eligible.Add(e);
            }
            if (eligible.Count == 0) return;

            var ev = eligible[Random.Range(0, eligible.Count)];
            Resolve(ev);
        }

        private void Resolve(EventDto ev)
        {
            var opcoes = ev.opcoes;
            if (opcoes == null || opcoes.Count == 0) return;

            int idx = Random.Range(0, opcoes.Count);
            var opt = opcoes[idx];
            _impact.Apply(opt.impacto);

            _active++;
            _cooldown[ev.id] = 60f; // cooldown 60s por tipo

            EventBus<EventoDisparado>.Publish(new EventoDisparado
            { id = ev.id, nome = ev.nome, opcao = opt.rotulo, impacto = opt.impacto });

            Debug.Log($"[Evento] {ev.id} '{ev.nome}' — escolha: \"{opt.rotulo}\".");
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
