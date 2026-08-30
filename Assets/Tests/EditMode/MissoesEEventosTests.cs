using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using Caos.World;
using NUnit.Framework;

namespace Caos.Tests
{
    /// <summary>Catálogos mínimos montados na mão, para o teste não depender dos JSON de StreamingAssets.</summary>
    internal static class CatalogoDeTeste
    {
        public static GameCatalogs ComMissoesEncadeadas()
        {
            var c = new GameCatalogs();
            c.Missions.Add(new MissionDto
            {
                id = "M01", titulo = "Chegada de Van", recompensaRs = 100f, recompensaXp = 50f,
                preRequisitos = new List<string>()
            });
            c.Missions.Add(new MissionDto
            {
                id = "M02", titulo = "Primeira entrega", recompensaRs = 250f, recompensaXp = 80f,
                preRequisitos = new List<string> { "M01" }
            });
            c.IndexAll();
            return c;
        }

        public static GameCatalogs ComEventoCerto()
        {
            var c = new GameCatalogs();
            c.Districts.Add(new DistrictDto { id = "Centro", nome = "Centro Histórico", probEventoBase = 1f });
            c.Events.Add(new EventDto
            {
                id = "E01", nome = "Blitz na esquina",
                bairros = new List<string>(), horarios = new List<string>(), climas = new List<string>(),
                opcoes = new List<EventOptionDto>
                {
                    new EventOptionDto { rotulo = "Encarar", impacto = new AttributeImpact { caos = 5f, rs = -50f } },
                    new EventOptionDto { rotulo = "Dar meia-volta", impacto = new AttributeImpact { sanidade = -3f } }
                }
            });
            c.IndexAll();
            return c;
        }
    }

    public class MissionServiceTests : TesteDeRegra
    {
        private GameCatalogs _catalogos;
        private EconomyService _econ;
        private MissionService _missoes;

        [SetUp]
        public void Montar()
        {
            _catalogos = CatalogoDeTeste.ComMissoesEncadeadas();
            _econ = new EconomyService(new TimeOfDayService());
            _missoes = new MissionService(_catalogos, _econ);
        }

        [Test]
        public void Missao_com_pre_requisito_pendente_nao_aparece()
        {
            Assert.That(_missoes.IsAvailable("M01"), Is.True);
            Assert.That(_missoes.IsAvailable("M02"), Is.False);
        }

        [Test]
        public void Concluir_a_anterior_libera_a_seguinte()
        {
            _missoes.Accept("M01");
            _missoes.Complete("M01");

            Assert.That(_missoes.IsCompleted("M01"), Is.True);
            Assert.That(_missoes.IsAvailable("M02"), Is.True);
        }

        [Test]
        public void Missao_inexistente_nao_pode_ser_aceita()
        {
            Assert.That(_missoes.IsAvailable("M99"), Is.False);
            Assert.That(_missoes.Accept("M99"), Is.False);
        }

        [Test]
        public void Nao_da_para_aceitar_a_mesma_missao_duas_vezes()
        {
            Assert.That(_missoes.Accept("M01"), Is.True);
            Assert.That(_missoes.Accept("M01"), Is.False);
            Assert.That(_missoes.ActiveSnapshot().Count, Is.EqualTo(1));
        }

        [Test]
        public void Concluir_missao_que_nao_esta_ativa_nao_paga()
        {
            float antes = _econ.Rs;

            Assert.That(_missoes.Complete("M01"), Is.False);
            Assert.That(_econ.Rs, Is.EqualTo(antes));
        }

        [Test]
        public void Concluir_paga_em_reais_e_publica_a_recompensa()
        {
            MissaoConcluida concluida = default;
            EventBus<MissaoConcluida>.Subscribe(e => concluida = e);
            float antes = _econ.Rs;

            _missoes.Accept("M01");
            _missoes.Complete("M01");

            Assert.That(_econ.Rs, Is.EqualTo(antes + 100f));
            Assert.That(concluida.id, Is.EqualTo("M01"));
            Assert.That(concluida.xp, Is.EqualTo(50f), "o XP viaja no evento — é assim que a progressão é creditada");
        }

        [Test]
        public void Missao_concluida_nao_volta_a_ficar_disponivel()
        {
            _missoes.Accept("M01");
            _missoes.Complete("M01");

            Assert.That(_missoes.IsAvailable("M01"), Is.False);
            Assert.That(_missoes.IsActive("M01"), Is.False);
        }

        [Test]
        public void Hydrate_restaura_o_progresso_do_save()
        {
            _missoes.Hydrate(new List<string> { "M01" }, new List<string> { "M02" });

            Assert.That(_missoes.IsCompleted("M01"), Is.True);
            Assert.That(_missoes.IsActive("M02"), Is.True);
            Assert.That(_missoes.IsAvailable("M01"), Is.False);
        }

        [Test]
        public void Hydrate_substitui_o_progresso_anterior()
        {
            _missoes.Accept("M01");
            _missoes.Hydrate(new List<string>(), new List<string>());

            Assert.That(_missoes.IsActive("M01"), Is.False);
            Assert.That(_missoes.IsAvailable("M01"), Is.True);
        }
    }

    public class ImpactResolverTests : TesteDeRegra
    {
        [Test]
        public void Um_impacto_alcanca_atributos_economia_reputacao_e_mundo()
        {
            var attrs = new PlayerAttributes();
            var econ  = new EconomyService(new TimeOfDayService());
            var rep   = new ReputationService();
            var world = new WorldStateService();
            var resolver = new ImpactResolver(attrs, econ, rep, world);

            resolver.Apply(new AttributeImpact
            {
                fome = -10f, rs = -40f, caos = 15f, stars = 2,
                rep = new List<RepDelta> { new RepDelta { alvo = "Milicia", delta = -20 } }
            });

            Assert.That(attrs.Fome, Is.EqualTo(60f).Within(0.01f));
            Assert.That(econ.Rs, Is.EqualTo(110f).Within(0.01f));
            Assert.That(rep.Get("Milicia"), Is.EqualTo(-20));
            Assert.That(world.Caos, Is.EqualTo(35f).Within(0.01f));
            Assert.That(world.Stars, Is.EqualTo(2));
        }

        [Test]
        public void Impacto_vazio_nao_mexe_em_nada()
        {
            var attrs = new PlayerAttributes();
            var econ  = new EconomyService(new TimeOfDayService());
            var world = new WorldStateService();
            var resolver = new ImpactResolver(attrs, econ, new ReputationService(), world);

            resolver.Apply(AttributeImpact.Zero);

            Assert.That(econ.Rs, Is.EqualTo(150f));
            Assert.That(world.Stars, Is.Zero);
            Assert.That(attrs.Saude, Is.EqualTo(100f));
        }
    }

    public class EventSystemTests : TesteDeRegra
    {
        private EventSystem Montar(IRandomSource rng, out WorldStateService world)
        {
            world = new WorldStateService();
            world.ApplyCaos(80f);                       // f_Caos alto: o evento sai sempre
            var time = new TimeOfDayService();          // 8h → "manha"
            var catalogos = CatalogoDeTeste.ComEventoCerto();
            var impacto = new ImpactResolver(new PlayerAttributes(), new EconomyService(time), new ReputationService(), world);
            return new EventSystem(world, time, catalogos, impacto, rng);
        }

        [Test]
        public void Nada_acontece_antes_do_intervalo_de_spawn()
        {
            var sistema = Montar(new CaosRandom(1234), out _);
            int disparos = 0;
            EventBus<EventoDisparado>.Subscribe(_ => disparos++);

            sistema.Tick(10f);

            Assert.That(disparos, Is.Zero);
        }

        [Test]
        public void Passado_o_intervalo_o_evento_dispara_e_aplica_o_impacto()
        {
            var sistema = Montar(new CaosRandom(1234), out var world);
            EventoDisparado disparado = default;
            int disparos = 0;
            EventBus<EventoDisparado>.Subscribe(e => { disparado = e; disparos++; });

            sistema.Tick(31f);

            Assert.That(disparos, Is.EqualTo(1));
            Assert.That(disparado.id, Is.EqualTo("E01"));
            Assert.That(world.Caos, Is.GreaterThan(0f));
        }

        [Test]
        public void Cooldown_impede_o_mesmo_evento_de_repetir_em_seguida()
        {
            var sistema = Montar(new CaosRandom(1234), out _);
            int disparos = 0;
            EventBus<EventoDisparado>.Subscribe(_ => disparos++);

            sistema.Tick(31f);
            sistema.Tick(31f);   // ainda dentro do cooldown de 60 s do E01

            Assert.That(disparos, Is.EqualTo(1));
        }

        [Test]
        public void Mesma_semente_produz_a_mesma_escolha()
        {
            // É o contrato do IRandomSource: com fluxo próprio e semente conhecida, dá para reproduzir
            // um bug de evento sem depender do estado global de Random que a cidade também usa.
            var a = Montar(new CaosRandom(99), out _);
            string escolhaA = null;
            EventBus<EventoDisparado>.Subscribe(e => escolhaA = e.opcao);
            a.Tick(31f);

            CaosRuntime.Reiniciar();

            var b = Montar(new CaosRandom(99), out _);
            string escolhaB = null;
            EventBus<EventoDisparado>.Subscribe(e => escolhaB = e.opcao);
            b.Tick(31f);

            Assert.That(escolhaA, Is.Not.Null);
            Assert.That(escolhaB, Is.EqualTo(escolhaA));
        }
    }

    public class GameCatalogsTests
    {
        [Test]
        public void Fallback_garante_veiculo_missao_e_bairro_para_o_mundo_abrir()
        {
            var c = GameCatalogs.CreateFallback();

            Assert.That(c.Vehicles, Is.Not.Empty);
            Assert.That(c.Missions, Is.Not.Empty);
            Assert.That(c.Districts, Is.Not.Empty);
        }

        [Test]
        public void IndexAll_permite_lookup_por_id()
        {
            var c = GameCatalogs.CreateFallback();

            Assert.That(c.VehicleById.ContainsKey("uno_escada"), Is.True);
            Assert.That(c.MissionById["M01"].titulo, Is.EqualTo("Chegada de Van"));
            Assert.That(c.DistrictById.ContainsKey("Centro"), Is.True);
        }

        [Test]
        public void IndexAll_pode_rodar_de_novo_sem_duplicar()
        {
            var c = GameCatalogs.CreateFallback();
            int antes = c.VehicleById.Count;

            c.IndexAll();

            Assert.That(c.VehicleById.Count, Is.EqualTo(antes));
        }
    }
}
