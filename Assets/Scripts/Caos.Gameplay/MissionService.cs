using System.Collections.Generic;
using Caos.Core;
using Caos.Data;

namespace Caos.Gameplay
{
    /// <summary>
    /// Missões: aceitar, verificar pré-requisitos e concluir (aplica recompensa R$/XP).
    /// Objetivos (ir/coletar/levar...) são resolvidos pela camada de mundo/UI; aqui fica o estado lógico (docs/07).
    /// </summary>
    public sealed class MissionService
    {
        private readonly GameCatalogs _catalogs;
        private readonly EconomyService _econ;
        private readonly HashSet<string> _completed = new HashSet<string>();
        private readonly HashSet<string> _active = new HashSet<string>();

        public MissionService(GameCatalogs catalogs, EconomyService econ) { _catalogs = catalogs; _econ = econ; }

        public bool IsAvailable(string id)
        {
            if (!_catalogs.MissionById.TryGetValue(id, out var m)) return false;
            if (_completed.Contains(id) || _active.Contains(id)) return false;
            if (m.preRequisitos == null) return true;
            foreach (var pre in m.preRequisitos)
                if (!_completed.Contains(pre)) return false;
            return true;
        }

        public bool Accept(string id)
        {
            if (!IsAvailable(id)) return false;
            _active.Add(id);
            EventBus<MissaoAceita>.Publish(new MissaoAceita { id = id });
            return true;
        }

        public bool Complete(string id)
        {
            if (!_active.Contains(id)) return false;
            if (!_catalogs.MissionById.TryGetValue(id, out var m)) return false;

            _active.Remove(id);
            _completed.Add(id);
            _econ.Add(m.recompensaRs);
            EventBus<MissaoConcluida>.Publish(new MissaoConcluida { id = id, rs = m.recompensaRs, xp = m.recompensaXp });
            UnityEngine.Debug.Log($"[Missão] Concluída: {id} '{m.titulo}' (+R$ {m.recompensaRs}, +{m.recompensaXp} XP).");
            return true;
        }

        public bool IsCompleted(string id) => _completed.Contains(id);
        public bool IsActive(string id) => _active.Contains(id);

        public System.Collections.Generic.List<string> CompletedSnapshot() => new System.Collections.Generic.List<string>(_completed);
        public System.Collections.Generic.List<string> ActiveSnapshot() => new System.Collections.Generic.List<string>(_active);

        public void Hydrate(System.Collections.Generic.List<string> completed, System.Collections.Generic.List<string> active)
        {
            _completed.Clear(); _active.Clear();
            if (completed != null) foreach (var id in completed) _completed.Add(id);
            if (active != null)    foreach (var id in active) _active.Add(id);
        }
    }
}
