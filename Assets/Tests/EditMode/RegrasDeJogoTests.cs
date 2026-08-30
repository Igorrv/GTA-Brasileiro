using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using Caos.World;
using NUnit.Framework;

namespace Caos.Tests
{
    /// <summary>Base dos testes de regra: zera o estado estático compartilhado antes de cada caso.</summary>
    public abstract class TesteDeRegra
    {
        [SetUp]
        public void PrepararRuntime()
        {
            CaosRuntime.Reiniciar();
            CaosLog.Destino = null;
        }
    }

    public class TimeOfDayServiceTests : TesteDeRegra
    {
        // 1 dia de jogo = 48 min reais → 1 hora de jogo = 120 s reais (docs/00, docs/10.4).
        private const float kSegundosPorHoraDeJogo = 120f;

        [Test]
        public void Uma_hora_de_jogo_leva_dois_minutos_reais()
        {
            var time = new TimeOfDayService();
            time.Tick(kSegundosPorHoraDeJogo);

            Assert.That(time.Hour, Is.EqualTo(9f).Within(0.001f));
            Assert.That(time.Day, Is.EqualTo(1));
        }

        [Test]
        public void Passar_da_meia_noite_vira_o_dia()
        {
            var time = new TimeOfDayService();
            time.Tick(kSegundosPorHoraDeJogo * 17f);   // 8h + 17h = 25h

            Assert.That(time.Day, Is.EqualTo(2));
            Assert.That(time.Hour, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void AdvanceHours_atravessa_varios_dias()
        {
            var time = new TimeOfDayService();
            time.AdvanceHours(24f * 6f);              // turno longo / viagem

            Assert.That(time.Day, Is.EqualTo(7));
            Assert.That(time.Hour, Is.EqualTo(8f).Within(0.001f));
        }

        [Test]
        public void AdvanceHours_negativo_nao_deixa_a_hora_negativa()
        {
            var time = new TimeOfDayService();
            time.AdvanceHours(-10f);

            Assert.That(time.Hour, Is.EqualTo(22f).Within(0.001f));
        }

        [TestCase(7f,  "manha")]
        [TestCase(12f, "dia")]
        [TestCase(17f, "tarde")]
        [TestCase(20f, "noite")]
        [TestCase(3f,  "madrugada")]
        public void Fase_do_dia_segue_a_hora(float hora, string esperada)
        {
            var time = new TimeOfDayService();
            time.Hydrate(hora, 1);

            Assert.That(time.Fase, Is.EqualTo(esperada));
        }

        [Test]
        public void Trafego_e_maior_no_pico_e_menor_de_madrugada()
        {
            var pico = new TimeOfDayService();
            pico.Hydrate(8f, 1);
            var madrugada = new TimeOfDayService();
            madrugada.Hydrate(3f, 1);

            Assert.That(pico.Trafego, Is.GreaterThan(madrugada.Trafego));
        }
    }

    public class WorldStateServiceTests : TesteDeRegra
    {
        [Test]
        public void ApplyCaos_publica_e_limita_entre_0_e_100()
        {
            var world = new WorldStateService();
            float ultimo = -1f;
            EventBus<CaosMudou>.Subscribe(e => ultimo = e.valor);

            world.ApplyCaos(500f);

            Assert.That(world.Caos, Is.EqualTo(100f));
            Assert.That(ultimo, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyCaos_sem_mudanca_real_nao_publica()
        {
            var world = new WorldStateService();
            int publicacoes = 0;
            EventBus<CaosMudou>.Subscribe(_ => publicacoes++);

            world.ApplyCaos(0f);

            Assert.That(publicacoes, Is.Zero);
        }

        [Test]
        public void Caos_decai_ate_o_piso_de_20_e_para()
        {
            var world = new WorldStateService();
            world.ApplyCaos(30f);           // 50

            world.Tick(600f);               // −0,01667/s × 600 = −10
            Assert.That(world.Caos, Is.EqualTo(40f).Within(0.05f));

            world.Tick(100000f);
            Assert.That(world.Caos, Is.EqualTo(20f), "o piso de caos da cidade é 20");
        }

        [Test]
        public void SetStars_limita_em_cinco_e_so_publica_quando_muda()
        {
            var world = new WorldStateService();
            int publicacoes = 0;
            EventBus<EstrelasMudou>.Subscribe(_ => publicacoes++);

            world.SetStars(9);
            world.SetStars(5);

            Assert.That(world.Stars, Is.EqualTo(5));
            Assert.That(publicacoes, Is.EqualTo(1));
        }

        [Test]
        public void Hydrate_limita_valores_vindos_de_save_corrompido()
        {
            var world = new WorldStateService();
            world.Hydrate(caos: 999f, stars: 42, district: DistrictId.Itauna, weather: WeatherState.Chuva);

            Assert.That(world.Caos, Is.EqualTo(100f));
            Assert.That(world.Stars, Is.EqualTo(5));
            Assert.That(world.CurrentDistrict, Is.EqualTo(DistrictId.Itauna));
        }
    }

    public class PlayerAttributesTests : TesteDeRegra
    {
        [Test]
        public void Sede_cai_mais_rapido_que_a_fome()
        {
            var attrs = new PlayerAttributes();
            attrs.Tick(60f);

            Assert.That(attrs.Fome, Is.EqualTo(69.5f).Within(0.01f));
            Assert.That(attrs.Sede, Is.EqualTo(69.1f).Within(0.01f));
            Assert.That(attrs.Energia, Is.EqualTo(69.6f).Within(0.01f));
        }

        [Test]
        public void Calor_e_esforco_aceleram_a_sede()
        {
            var parado = new PlayerAttributes();
            var correndoNoSol = new PlayerAttributes { Ativo = true, Calor = true };

            parado.Tick(60f);
            correndoNoSol.Tick(60f);

            Assert.That(correndoNoSol.Sede, Is.LessThan(parado.Sede));
            Assert.That(correndoNoSol.Energia, Is.LessThan(parado.Energia));
        }

        [Test]
        public void Apply_limita_entre_0_e_100_e_publica_snapshot()
        {
            var attrs = new PlayerAttributes();
            AtributosMudou ultimo = default;
            EventBus<AtributosMudou>.Subscribe(e => ultimo = e);

            attrs.Apply("fome", 500f);
            attrs.Apply("energia", -500f);

            Assert.That(attrs.Fome, Is.EqualTo(100f));
            Assert.That(attrs.Energia, Is.EqualTo(0f));
            Assert.That(ultimo.fome, Is.EqualTo(100f));
        }

        [Test]
        public void Morte_e_anunciada_uma_vez_por_queda()
        {
            // Regressão: com a saúde zerada o evento saía a cada frame, e quem escuta (o ciclo de vida)
            // refazia o respawn 60 vezes por segundo durante toda a tela de WASTED.
            var attrs = new PlayerAttributes();
            int mortes = 0;
            EventBus<PlayerMorreu>.Subscribe(_ => mortes++);

            attrs.Apply("saude", -100f);
            for (int i = 0; i < 60; i++) attrs.Tick(1f / 60f);

            Assert.That(mortes, Is.EqualTo(1));
        }

        [Test]
        public void Depois_de_reanimado_a_morte_pode_ser_anunciada_de_novo()
        {
            var attrs = new PlayerAttributes();
            int mortes = 0;
            EventBus<PlayerMorreu>.Subscribe(_ => mortes++);

            attrs.Apply("saude", -100f);
            attrs.Tick(0.016f);

            attrs.Hydrate(70f, 70f, 70f, 60f, 100f);   // respawn
            attrs.Apply("saude", -100f);
            attrs.Tick(0.016f);

            Assert.That(mortes, Is.EqualTo(2));
        }
    }

    public class EconomyServiceTests : TesteDeRegra
    {
        [Test]
        public void Comeca_com_o_salario_inicial_da_biblia()
        {
            var econ = new EconomyService(new TimeOfDayService());
            Assert.That(econ.Rs, Is.EqualTo(150f));
        }

        [Test]
        public void TrySpend_recusa_sem_saldo_e_nao_cria_divida()
        {
            var econ = new EconomyService(new TimeOfDayService());

            Assert.That(econ.TrySpend(1000f), Is.False);
            Assert.That(econ.Rs, Is.EqualTo(150f));
            Assert.That(econ.TrySpend(150f), Is.True);
            Assert.That(econ.Rs, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Add_publica_o_novo_saldo()
        {
            var econ = new EconomyService(new TimeOfDayService());
            DinheiroMudou ultimo = default;
            EventBus<DinheiroMudou>.Subscribe(e => ultimo = e);

            econ.Add(50f, 3f);

            Assert.That(ultimo.rs, Is.EqualTo(200f));
            Assert.That(ultimo.caosCash, Is.EqualTo(3f));
        }

        [Test]
        public void PriceFor_corrige_pelo_IPC_Caos()
        {
            var econ = new EconomyService(new TimeOfDayService());
            Assert.That(econ.PriceFor(100f), Is.EqualTo(101f).Within(0.001f));

            econ.AddWeeklyInflation(caos: 40f);        // +1,8 p.p.
            Assert.That(econ.IpcCaos, Is.EqualTo(0.028f).Within(0.0001f));
            Assert.That(econ.PriceFor(100f), Is.EqualTo(102.8f).Within(0.01f));
        }

        [Test]
        public void Inflacao_semanal_dispara_na_virada_do_setimo_dia()
        {
            var time = new TimeOfDayService();
            var econ = new EconomyService(time);
            float antes = econ.IpcCaos;

            time.AdvanceHours(24f * 6f);               // dia 1 → dia 7
            econ.Tick(0.016f);

            Assert.That(econ.IpcCaos, Is.GreaterThan(antes));
        }

        [Test]
        public void Inflacao_nao_dispara_duas_vezes_no_mesmo_dia()
        {
            var time = new TimeOfDayService();
            var econ = new EconomyService(time);

            time.AdvanceHours(24f * 6f);
            econ.Tick(0.016f);
            float depoisDaPrimeira = econ.IpcCaos;
            econ.Tick(0.016f);

            Assert.That(econ.IpcCaos, Is.EqualTo(depoisDaPrimeira));
        }
    }

    public class ExperienceServiceTests : TesteDeRegra
    {
        [Test]
        public void Comeca_no_nivel_1_como_recem_chegado()
        {
            var xp = new ExperienceService();

            Assert.That(xp.Nivel, Is.EqualTo(1));
            Assert.That(xp.Xp, Is.Zero);
            Assert.That(xp.Titulo, Is.EqualTo("Recém-chegado"));
        }

        [Test]
        public void Xp_suficiente_sobe_de_nivel_e_publica_o_titulo()
        {
            var xp = new ExperienceService();
            SubiuDeNivel subiu = default;
            int publicacoes = 0;
            EventBus<SubiuDeNivel>.Subscribe(e => { subiu = e; publicacoes++; });

            xp.Adicionar(ExperienceService.XpParaNivel(2), "teste");

            Assert.That(xp.Nivel, Is.EqualTo(2));
            Assert.That(publicacoes, Is.EqualTo(1));
            Assert.That(subiu.titulo, Is.EqualTo("Chegante"));
        }

        [Test]
        public void Um_unico_ganho_grande_pode_pular_varios_niveis()
        {
            var xp = new ExperienceService();
            xp.Adicionar(ExperienceService.XpParaNivel(5));

            Assert.That(xp.Nivel, Is.EqualTo(5));
        }

        [Test]
        public void Xp_nao_positivo_e_ignorado()
        {
            var xp = new ExperienceService();
            int publicacoes = 0;
            EventBus<XpMudou>.Subscribe(_ => publicacoes++);

            xp.Adicionar(0f);
            xp.Adicionar(-100f);

            Assert.That(xp.Xp, Is.Zero);
            Assert.That(publicacoes, Is.Zero);
        }

        [Test]
        public void Nivel_maximo_trava_a_progressao_e_a_barra_enche()
        {
            var xp = new ExperienceService();
            xp.Adicionar(1e9f);

            Assert.That(xp.Nivel, Is.EqualTo(ExperienceService.NivelMaximo));
            Assert.That(xp.Progresso01, Is.EqualTo(1f));
            Assert.That(xp.Titulo, Is.EqualTo("Dono da Cidade"));
        }

        [Test]
        public void Progresso_da_barra_fica_entre_0_e_1()
        {
            var xp = new ExperienceService();
            xp.Adicionar(ExperienceService.XpParaNivel(2));
            Assert.That(xp.Progresso01, Is.EqualTo(0f).Within(0.0001f));

            float meio = (ExperienceService.XpParaNivel(3) - ExperienceService.XpParaNivel(2)) * 0.5f;
            xp.Adicionar(meio);
            Assert.That(xp.Progresso01, Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void Hydrate_conserta_nivel_invalido_de_save_antigo()
        {
            var xp = new ExperienceService();
            xp.Hydrate(500f, 0);
            Assert.That(xp.Nivel, Is.EqualTo(1));

            xp.Hydrate(500f, 999);
            Assert.That(xp.Nivel, Is.EqualTo(ExperienceService.NivelMaximo));
        }

        [Test]
        public void Vantagens_crescem_com_o_nivel()
        {
            var xp = new ExperienceService();
            float pagamentoNivel1 = xp.BonusPagamento;

            xp.Adicionar(ExperienceService.XpParaNivel(10));

            Assert.That(xp.BonusPagamento, Is.GreaterThan(pagamentoNivel1));
            Assert.That(xp.DescontoCansaco, Is.LessThan(1f));
            Assert.That(xp.BonusDespiste, Is.GreaterThan(1f));
        }
    }

    public class ReputationServiceTests : TesteDeRegra
    {
        [Test]
        public void ApplyDelta_acumula_e_limita_entre_menos_100_e_100()
        {
            var rep = new ReputationService();
            rep.ApplyDelta("Camelos", 60);
            rep.ApplyDelta("Camelos", 60);

            Assert.That(rep.Get("Camelos"), Is.EqualTo(100));
            Assert.That(rep.Tone("Camelos"), Is.EqualTo("Ídolo"));
        }

        [Test]
        public void Alvo_desconhecido_nao_estoura()
        {
            var rep = new ReputationService();
            Assert.DoesNotThrow(() => rep.ApplyDelta("SindicatoDosPastel", 50));
            Assert.That(rep.Get("SindicatoDosPastel"), Is.Zero);
            Assert.That(rep.TryGet("SindicatoDosPastel", out _), Is.False);
        }

        [Test]
        public void Hydrate_escreve_por_cima_em_vez_de_somar()
        {
            // Regressão: Hydrate somava o valor salvo ao valor corrente. Carregar um save por cima de
            // uma sessão já jogada (voltar ao menu e continuar) dobrava a reputação.
            var rep = new ReputationService();
            rep.ApplyDelta("Centro", 30);

            var salvo = new List<ReputationService.RepEntry>
            {
                new ReputationService.RepEntry { alvo = "Centro", valor = 30 }
            };
            rep.Hydrate(new List<ReputationService.RepEntry>(), salvo);

            Assert.That(rep.Get("Centro"), Is.EqualTo(30));
        }

        [Test]
        public void Hydrate_zera_o_que_nao_esta_no_save()
        {
            var rep = new ReputationService();
            rep.ApplyDelta("Milicia", -40);

            rep.Hydrate(new List<ReputationService.RepEntry>(), new List<ReputationService.RepEntry>());

            Assert.That(rep.Get("Milicia"), Is.Zero);
        }

        [Test]
        public void Snapshot_cobre_todas_as_faccoes_e_bairros()
        {
            var rep = new ReputationService();

            Assert.That(rep.FactionSnapshot().Count, Is.EqualTo(System.Enum.GetValues(typeof(FactionId)).Length));
            Assert.That(rep.DistrictSnapshot().Count, Is.EqualTo(System.Enum.GetValues(typeof(DistrictId)).Length));
        }
    }
}
