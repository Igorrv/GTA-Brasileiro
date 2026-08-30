using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using Caos.World;
using NUnit.Framework;

namespace Caos.Tests
{
    /// <summary>
    /// Missões diárias (docs/07 §7.5): sorteio do lote de 5 por dia, aceite exclusivo, recompensas
    /// (R$/XP/reputação/bônus de atributo), bônus do lote completo, virada de dia e retomada via save.
    /// O catálogo é montado na mão — o teste não lê os JSON de StreamingAssets.
    /// </summary>
    public class DiariasTests : TesteDeRegra
    {
        private GameCatalogs _catalogos;
        private TimeOfDayService _time;
        private EconomyService _econ;
        private ExperienceService _xp;
        private ReputationService _rep;
        private PlayerAttributes _attrs;
        private DailyMissionService _diarias;

        [SetUp]
        public void Montar()
        {
            GameSession.DefinirSemente(12345);
            _catalogos = new GameCatalogs();
            for (int i = 1; i <= 10; i++)
                _catalogos.Dailies.Add(new DailyDto
                {
                    id = "D" + i.ToString("00"),
                    titulo = "Diária " + i,
                    dador = "teste",
                    descricao = "desc",
                    recompensaRs = 100f + i,
                    recompensaXp = 50f + i,
                    recompensaRep = i == 1
                        ? new List<RepDelta> { new RepDelta { alvo = "Motoclube", delta = 6 } }
                        : new List<RepDelta>(),
                    bonus = i == 2 ? new AttributeImpact { sanidade = 4f } : new AttributeImpact(),
                    objetivos = new List<MissionObjectiveDto>
                    {
                        new MissionObjectiveDto { tipo = "ir", alvo = "x", quantidade = 1, local = "Centro" }
                    }
                });
            _catalogos.IndexAll();

            _time  = new TimeOfDayService();
            var world = new WorldStateService();
            _econ  = new EconomyService(_time);
            _xp    = new ExperienceService();
            _rep   = new ReputationService();
            _attrs = new PlayerAttributes();
            var impact = new ImpactResolver(_attrs, _econ, _rep, world);
            _diarias = new DailyMissionService(_catalogos, _econ, _xp, _rep, impact, _time);
            _diarias.Tick(0.1f);   // primeiro tick sorteia o lote do dia
        }

        [Test]
        public void Sorteia_cinco_por_dia_sem_repeticao()
        {
            var lote = _diarias.Sorteadas;
            Assert.That(lote.Count, Is.EqualTo(DailyMissionService.PorDia));
            Assert.That(new HashSet<string>(lote).Count, Is.EqualTo(lote.Count), "sem repetição no lote");
            foreach (var id in lote)
                Assert.That(_catalogos.DailyById.ContainsKey(id), Is.True, $"{id} existe no catálogo");
        }

        [Test]
        public void Sorteio_e_deterministico_para_o_mesmo_dia_e_mundo()
        {
            var outra = new DailyMissionService(_catalogos, _econ, _xp, _rep,
                new ImpactResolver(_attrs, _econ, _rep, new WorldStateService()), _time);
            outra.Tick(0.1f);
            CollectionAssert.AreEqual(_diarias.Sorteadas, outra.Sorteadas);
        }

        [Test]
        public void Aceite_e_exclusivo_e_nao_repete()
        {
            string a = _diarias.Sorteadas[0], b = _diarias.Sorteadas[1];
            Assert.That(_diarias.EstaDisponivel(a), Is.True);
            Assert.That(_diarias.Accept(a), Is.True);
            Assert.That(_diarias.Accept(a), Is.False, "a mesma duas vezes");
            Assert.That(_diarias.Accept(b), Is.False, "segunda simultânea");
            Assert.That(_diarias.AtivaId, Is.EqualTo(a));
        }

        [Test]
        public void Concluir_paga_rs_xp_e_reputacao()
        {
            string a = _diarias.Sorteadas[0];
            var dto = _catalogos.DailyById[a];
            float rsAntes = _econ.Rs, xpAntes = _xp.Xp;

            _diarias.Accept(a);
            _diarias.AnotarPasso(1);
            Assert.That(_diarias.PassoAtiva, Is.EqualTo(1), "passo anotado para o save");
            Assert.That(_diarias.Complete(a), Is.True);

            Assert.That(_econ.Rs, Is.EqualTo(rsAntes + dto.recompensaRs).Within(0.01f));
            Assert.That(_xp.Xp, Is.EqualTo(xpAntes + dto.recompensaXp).Within(0.01f));
            Assert.That(_diarias.EstaConcluida(a), Is.True);
            Assert.That(_diarias.EstaDisponivel(a), Is.False, "concluída não volta hoje");
        }

        [Test]
        public void Reputacao_da_recompensa_e_aplicada()
        {
            // D01 tem Motoclube +6 no catálogo de teste; força ela no lote via Hydrate
            _diarias.Hydrate(_time.Day, new List<string> { "D01" }, new List<string>(), null, 0);
            int antes = _rep.Get("Motoclube");

            _diarias.Accept("D01");
            _diarias.Complete("D01");

            Assert.That(_rep.Get("Motoclube"), Is.EqualTo(antes + 6));
        }

        [Test]
        public void Bonus_de_atributo_passa_pelo_impact_resolver()
        {
            // D02 tem sanidade +4 no catálogo de teste
            _diarias.Hydrate(_time.Day, new List<string> { "D02" }, new List<string>(), null, 0);
            float antes = _attrs.Sanidade;

            _diarias.Accept("D02");
            _diarias.Complete("D02");

            Assert.That(_attrs.Sanidade, Is.EqualTo(System.Math.Min(100f, antes + 4f)).Within(0.01f));
        }

        [Test]
        public void Fechar_o_lote_inteiro_paga_bonus_de_xp()
        {
            float xpAntes = _xp.Xp;
            float esperado = 0f;
            foreach (var id in _diarias.Sorteadas)
                esperado += _catalogos.DailyById[id].recompensaXp;

            foreach (var id in new List<string>(_diarias.Sorteadas))
            {
                _diarias.Accept(id);
                _diarias.Complete(id);
            }

            Assert.That(_diarias.ConcluidasHoje, Is.EqualTo(DailyMissionService.PorDia));
            Assert.That(_xp.Xp, Is.EqualTo(xpAntes + esperado + 150f).Within(0.01f), "lote + bônus da casa");
        }

        [Test]
        public void Virada_de_dia_renova_o_lote_e_zera_as_feitas()
        {
            string a = _diarias.Sorteadas[0];
            _diarias.Accept(a);
            _diarias.Complete(a);
            int diaAntes = _diarias.Dia;

            _time.AdvanceHours(24f);
            _diarias.Tick(0.1f);

            Assert.That(_diarias.Dia, Is.EqualTo(diaAntes + 1));
            Assert.That(_diarias.ConcluidasHoje, Is.EqualTo(0));
            Assert.That(_diarias.Sorteadas.Count, Is.EqualTo(DailyMissionService.PorDia));
        }

        [Test]
        public void Quem_comecou_termina_mesmo_depois_da_virada()
        {
            string a = _diarias.Sorteadas[0];
            float rsAntes = _econ.Rs;
            _diarias.Accept(a);

            _time.AdvanceHours(24f);
            _diarias.Tick(0.1f);

            Assert.That(_diarias.Complete(a), Is.True, "ativa da véspera conclui hoje");
            Assert.That(_econ.Rs, Is.GreaterThan(rsAntes));
        }

        [Test]
        public void Desistir_devolve_a_diaria_ao_lote()
        {
            string a = _diarias.Sorteadas[0];
            _diarias.Accept(a);
            _diarias.Abandonar();

            Assert.That(_diarias.AtivaId, Is.Null);
            Assert.That(_diarias.EstaDisponivel(a), Is.True);
        }

        [Test]
        public void Hydrate_restaura_lote_e_ativa_no_passo_salvo()
        {
            var lote = new List<string>(_diarias.Sorteadas);
            string a = lote[0];
            _diarias.Accept(a);
            _diarias.AnotarPasso(2);

            var outra = new DailyMissionService(_catalogos, _econ, _xp, _rep,
                new ImpactResolver(_attrs, _econ, _rep, new WorldStateService()), _time);
            outra.Hydrate(_diarias.Dia, _diarias.DrawnSnapshot(), _diarias.DoneSnapshot(),
                _diarias.AtivaId, _diarias.PassoAtiva);

            CollectionAssert.AreEqual(lote, outra.Sorteadas);
            Assert.That(outra.AtivaId, Is.EqualTo(a));
            Assert.That(outra.PassoAtiva, Is.EqualTo(2));
        }

        [Test]
        public void Hydrate_de_outro_dia_descarta_e_resorteia()
        {
            var outra = new DailyMissionService(_catalogos, _econ, _xp, _rep,
                new ImpactResolver(_attrs, _econ, _rep, new WorldStateService()), _time);
            outra.Hydrate(_time.Day - 1, _diarias.DrawnSnapshot(), _diarias.DoneSnapshot(), "D05", 1);
            outra.Tick(0.1f);

            Assert.That(outra.Dia, Is.EqualTo(_time.Day));
            Assert.That(outra.AtivaId, Is.Null, "diária de ontem não vale hoje");
        }
    }
}
