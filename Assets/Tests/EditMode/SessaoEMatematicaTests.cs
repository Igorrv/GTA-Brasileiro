using Caos.Core;
using NUnit.Framework;

namespace Caos.Tests
{
    public class GameSessionTests
    {
        [SetUp]
        public void Preparar() => GameSession.Reset();

        [TearDown]
        public void Limpar() => GameSession.Reset();

        [Test]
        public void Antes_de_escolher_o_slot_a_partida_nao_comecou()
        {
            Assert.That(GameSession.Iniciado, Is.False);
            Assert.That(GameSession.Slot, Is.EqualTo(1));
        }

        [Test]
        public void Iniciar_libera_a_partida_no_slot_escolhido()
        {
            GameSession.Iniciar(slot: 2, novoJogo: true);

            Assert.That(GameSession.Iniciado, Is.True);
            Assert.That(GameSession.Slot, Is.EqualTo(2));
            Assert.That(GameSession.NovoJogo, Is.True);
        }

        [Test]
        public void Slot_invalido_cai_no_primeiro()
        {
            GameSession.Iniciar(slot: 0, novoJogo: false);
            Assert.That(GameSession.Slot, Is.EqualTo(1));
        }

        [Test]
        public void O_mesmo_slot_reabre_sempre_a_mesma_cidade()
        {
            // A cidade nasce da semente: se ela variasse, o save 1 abriria noutra São Genésio.
            GameSession.Iniciar(slot: 1, novoJogo: false);
            int primeiraVez = GameSession.Semente;

            GameSession.Reset();
            GameSession.Iniciar(slot: 1, novoJogo: false);

            Assert.That(GameSession.Semente, Is.EqualTo(primeiraVez));
            Assert.That(GameSession.Semente, Is.Not.Zero);
        }

        [Test]
        public void Slots_diferentes_geram_cidades_diferentes()
        {
            GameSession.Iniciar(slot: 1, novoJogo: false);
            int semente1 = GameSession.Semente;

            GameSession.Reset();
            GameSession.Iniciar(slot: 2, novoJogo: false);

            Assert.That(GameSession.Semente, Is.Not.EqualTo(semente1));
        }

        [Test]
        public void Semente_vinda_da_rede_tem_prioridade_sobre_a_do_slot()
        {
            // Contrato do multiplayer: o anfitrião manda a semente no handshake e o cliente entra
            // naquele mundo, não no mundo do próprio slot.
            GameSession.DefinirSemente(4242);
            GameSession.Iniciar(slot: 3, novoJogo: true);

            Assert.That(GameSession.Semente, Is.EqualTo(4242));
            Assert.That(GameSession.SementeExterna, Is.True);
        }

        [Test]
        public void Reset_devolve_a_sessao_ao_menu()
        {
            GameSession.DefinirSemente(7);
            GameSession.Iniciar(slot: 3, novoJogo: true);

            GameSession.Reset();

            Assert.That(GameSession.Iniciado, Is.False);
            Assert.That(GameSession.NovoJogo, Is.False);
            Assert.That(GameSession.SementeExterna, Is.False);
            Assert.That(GameSession.Semente, Is.Zero);
        }
    }

    public class CaosMathTests
    {
        [Test]
        public void Limitar_prende_nas_bordas()
        {
            Assert.That(CaosMath.Limitar(-5f, 0f, 100f), Is.EqualTo(0f));
            Assert.That(CaosMath.Limitar(500f, 0f, 100f), Is.EqualTo(100f));
            Assert.That(CaosMath.Limitar(42f, 0f, 100f), Is.EqualTo(42f));
            Assert.That(CaosMath.Limitar(9, 0, 5), Is.EqualTo(5));
        }

        [Test]
        public void Limitar01_e_o_intervalo_da_barra_de_UI()
        {
            Assert.That(CaosMath.Limitar01(-0.3f), Is.EqualTo(0f));
            Assert.That(CaosMath.Limitar01(1.7f), Is.EqualTo(1f));
            Assert.That(CaosMath.Limitar01(0.25f), Is.EqualTo(0.25f));
        }

        [Test]
        public void Potencia_reproduz_a_curva_de_XP()
        {
            Assert.That(CaosMath.Potencia(4f, 1.45f), Is.EqualTo(7.4642f).Within(0.001f));
            Assert.That(CaosMath.Potencia(1f, 1.45f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Aproximadamente_usa_a_mesma_tolerancia_relativa_do_Mathf()
        {
            Assert.That(CaosMath.Aproximadamente(1f, 1f), Is.True);
            Assert.That(CaosMath.Aproximadamente(0f, 0f), Is.True);
            Assert.That(CaosMath.Aproximadamente(100f, 100.00001f), Is.True);
            Assert.That(CaosMath.Aproximadamente(1f, 1.01f), Is.False);
        }
    }

    public class CaosRandomTests
    {
        [Test]
        public void Mesma_semente_produz_a_mesma_sequencia()
        {
            var a = new CaosRandom(2026);
            var b = new CaosRandom(2026);

            for (int i = 0; i < 32; i++)
                Assert.That(b.Valor01(), Is.EqualTo(a.Valor01()));
        }

        [Test]
        public void Sementes_diferentes_divergem()
        {
            var a = new CaosRandom(1);
            var b = new CaosRandom(2);

            bool algumDiferente = false;
            for (int i = 0; i < 32 && !algumDiferente; i++)
                algumDiferente = a.Valor01() != b.Valor01();

            Assert.That(algumDiferente, Is.True);
        }

        [Test]
        public void Valor01_fica_no_intervalo_de_probabilidade()
        {
            var rng = new CaosRandom(7);
            for (int i = 0; i < 500; i++)
            {
                float v = rng.Valor01();
                Assert.That(v, Is.GreaterThanOrEqualTo(0f));
                Assert.That(v, Is.LessThan(1f));
            }
        }

        [Test]
        public void Intervalo_inteiro_respeita_o_maximo_exclusivo()
        {
            var rng = new CaosRandom(7);
            for (int i = 0; i < 500; i++)
            {
                int v = rng.Intervalo(0, 3);
                Assert.That(v, Is.InRange(0, 2));
            }
        }

        [Test]
        public void Intervalo_degenerado_devolve_o_minimo_em_vez_de_estourar()
        {
            var rng = new CaosRandom(7);
            Assert.That(rng.Intervalo(5, 5), Is.EqualTo(5));
            Assert.That(rng.Intervalo(5, 1), Is.EqualTo(5));
        }
    }
}
