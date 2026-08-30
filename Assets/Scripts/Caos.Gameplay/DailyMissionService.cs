using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.World;

namespace Caos.Gameplay
{
    /// <summary>
    /// Missões diárias (docs/07 §7.5): um lote de <see cref="PorDia"/> tarefas sorteado do catálogo
    /// <c>dailies.json</c> a cada virada de dia de jogo, aceito pelo app Diárias do celular e
    /// resolvido no mundo pelo <c>MissionTracker</c> (que empresta beacon, GPS e painel).
    ///
    /// O sorteio é <b>determinístico por dia e por mundo</b> (<c>dia + semente da sessão</b>): todo
    /// jogador do mesmo mundo vê as mesmas cinco diárias no mesmo dia — como as diárias de um
    /// live-service, mas sem servidor. Quem começa uma diária e vira o dia no meio <b>termina</b>:
    /// a recompensa é paga normalmente; só o lote disponível é que renova.
    ///
    /// Completar o lote inteiro paga o bônus de XP da casa (<see cref="kBonusXpCompleto"/>).
    /// Estado persistido no save (versão 3): dia, lote sorteado, concluídas e a em andamento.
    /// </summary>
    public sealed class DailyMissionService : ITickable
    {
        public const int PorDia = 5;
        private const float kBonusXpCompleto = 150f;

        private readonly GameCatalogs      _catalogs;
        private readonly EconomyService    _econ;
        private readonly ExperienceService _xp;
        private readonly ReputationService _rep;
        private readonly ImpactResolver    _impact;
        private readonly TimeOfDayService  _time;

        private int _dia = -1;                          // -1 = ainda não sorteou (força no 1º tick)
        private readonly List<string>    _sorteadas  = new List<string>();
        private readonly HashSet<string> _concluidas = new HashSet<string>();
        private string _ativaId;
        private int    _passoAtiva;

        public DailyMissionService(GameCatalogs catalogs, EconomyService econ, ExperienceService xp,
                                   ReputationService rep, ImpactResolver impact, TimeOfDayService time)
        {
            _catalogs = catalogs; _econ = econ; _xp = xp; _rep = rep; _impact = impact; _time = time;
        }

        /// <summary>Depois do relógio (5) e da economia (15): a virada de dia renova o lote.</summary>
        public int Order => 18;

        public string AtivaId    => _ativaId;
        public int    PassoAtiva => _passoAtiva;
        public int    Dia        => _dia;

        /// <summary>Ids sorteados hoje (auto-sorteia se o dia virou e ninguém olhou ainda).</summary>
        public List<string> Sorteadas
        {
            get { GarantirDia(); return _sorteadas; }
        }

        public int ConcluidasHoje { get { GarantirDia(); return _concluidas.Count; } }

        public bool EstaConcluida(string id)  => _concluidas.Contains(id);
        public bool EstaAtiva(string id)      => _ativaId == id;

        /// <summary>Sorteada hoje, ainda não feita e sem outra diária em andamento.</summary>
        public bool EstaDisponivel(string id)
        {
            GarantirDia();
            return _sorteadas.Contains(id) && !_concluidas.Contains(id) && _ativaId == null;
        }

        public void Tick(float dt) => GarantirDia();

        /// <summary>Se o dia virou (ou nunca sorteou), renova o lote. Idempotente.</summary>
        private void GarantirDia()
        {
            if (_time != null && _dia == _time.Day) return;   // já sorteou hoje (mesmo que o lote seja vazio)
            Renovar(_time != null ? _time.Day : 1);
        }

        /// <summary>Sorteia o lote do dia. Fisher-Yates com semente fixa = mesmo lote em todo cliente.</summary>
        private void Renovar(int dia)
        {
            _dia = dia;
            _sorteadas.Clear();
            _concluidas.Clear();
            // quem estava no meio de uma diária não perde o progresso: termina e recebe normal

            var pool = new List<DailyDto>();
            if (_catalogs != null)
                for (int i = 0; i < _catalogs.Dailies.Count; i++)
                    if (_catalogs.Dailies[i] != null) pool.Add(_catalogs.Dailies[i]);

            var rng = new System.Random(unchecked(dia * 7919 + GameSession.Semente));
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;   // troca explícita (sem tupla)
            }
            for (int i = 0; i < pool.Count && _sorteadas.Count < PorDia; i++)
                _sorteadas.Add(pool[i].id);

            EventBus<DiariasRenovadas>.Publish(new DiariasRenovadas { dia = dia, quantidade = _sorteadas.Count });
            CaosLog.Info($"[Diárias] Dia {dia}: sorteadas {string.Join(", ", _sorteadas)}.");
        }

        /// <summary>Aceita uma diária disponível. Só uma por vez — o beacon/GPS é único.</summary>
        public bool Accept(string id)
        {
            if (!EstaDisponivel(id)) return false;
            _ativaId = id;
            _passoAtiva = 0;
            EventBus<DiariaAceita>.Publish(new DiariaAceita { id = id });
            return true;
        }

        /// <summary>O tracker avisa em que passo da diária ativa está, para o save retomar no ponto.</summary>
        public void AnotarPasso(int passo) { if (_ativaId != null) _passoAtiva = passo < 0 ? 0 : passo; }

        /// <summary>Desiste da diária em andamento (ela volta a ficar disponível hoje).</summary>
        public void Abandonar()
        {
            _ativaId = null;
            _passoAtiva = 0;
        }

        /// <summary>
        /// Conclui a diária ativa: paga R$, XP, reputação e o bônus de atributo do catálogo.
        /// Funciona mesmo se o dia virou no meio — quem começou, termina.
        /// </summary>
        public bool Complete(string id)
        {
            if (_ativaId != id) return false;
            if (_catalogs == null || !_catalogs.DailyById.TryGetValue(id, out var d)) return false;

            _ativaId = null;
            _passoAtiva = 0;
            bool contaProLote = _sorteadas.Contains(id);
            if (contaProLote) _concluidas.Add(id);

            _econ.Add(d.recompensaRs);
            _xp?.Adicionar(d.recompensaXp, "diária " + id);
            if (d.recompensaRep != null)
                foreach (var r in d.recompensaRep)
                    _rep.ApplyDelta(r.alvo, r.delta);
            if (TemEfeito(d.bonus)) _impact?.Apply(d.bonus);

            EventBus<DiariaConcluida>.Publish(new DiariaConcluida { id = id, rs = d.recompensaRs, xp = d.recompensaXp });
            CaosLog.Info($"[Diárias] Concluída: {id} '{d.titulo}' (+R$ {d.recompensaRs:F0}, +{d.recompensaXp:F0} XP).");

            if (contaProLote && _sorteadas.Count >= PorDia && _concluidas.Count >= _sorteadas.Count)
            {
                _xp?.Adicionar(kBonusXpCompleto, "todas as diárias do dia");
                EventBus<DiariasCompletas>.Publish(new DiariasCompletas { xpBonus = kBonusXpCompleto });
                CaosLog.Info($"[Diárias] Lote do dia completo! Bônus de {kBonusXpCompleto:F0} XP.");
            }
            return true;
        }

        private static bool TemEfeito(AttributeImpact b)
            => b.fome != 0 || b.sede != 0 || b.energia != 0 || b.sanidade != 0 || b.saude != 0
            || b.rs != 0 || b.caosCash != 0 || b.caos != 0 || b.stars != 0
            || (b.rep != null && b.rep.Count > 0);

        // ------------------------------------------------------------------ save
        public List<string> DrawnSnapshot() { GarantirDia(); return new List<string>(_sorteadas); }
        public List<string> DoneSnapshot()  { return new List<string>(_concluidas); }

        /// <summary>
        /// Restaura do save. Lote de outro dia (ou save antigo sem diárias) é descartado e o dia
        /// corrente sorteia de novo — diária de ontem não vale hoje.
        /// </summary>
        public void Hydrate(int dia, List<string> drawn, List<string> done, string ativa, int passo)
        {
            _ativaId = null; _passoAtiva = 0;
            if (drawn == null || drawn.Count == 0 || (_time != null && dia != _time.Day))
            {
                _dia = -1;   // força sorteio novo no próximo acesso
                _sorteadas.Clear(); _concluidas.Clear();
                return;
            }

            _dia = dia;
            _sorteadas.Clear(); _sorteadas.AddRange(drawn);
            _concluidas.Clear();
            if (done != null) foreach (var d in done) _concluidas.Add(d);
            if (!string.IsNullOrEmpty(ativa) && _sorteadas.Contains(ativa) && !_concluidas.Contains(ativa))
            {
                _ativaId = ativa;
                _passoAtiva = passo < 0 ? 0 : passo;
            }
        }
    }
}
