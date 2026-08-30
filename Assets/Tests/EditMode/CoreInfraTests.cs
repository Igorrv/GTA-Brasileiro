using System;
using Caos.Core;
using NUnit.Framework;

namespace Caos.Tests
{
    /// <summary>
    /// Infraestrutura compartilhada: barramento de eventos, registro de serviços e reinício do runtime.
    /// É código que todo mundo usa e ninguém testava — os casos abaixo travam o comportamento em que
    /// o resto do jogo confia.
    /// </summary>
    public class EventBusTests
    {
        private struct Ping : IGameEvent { public int valor; }
        private struct Pong : IGameEvent { }

        [SetUp]
        public void Limpar()
        {
            EventBus.LimparTudo();
            CaosLog.Destino = null;
        }

        [Test]
        public void Publish_entrega_para_todos_os_assinantes()
        {
            int a = 0, b = 0;
            EventBus<Ping>.Subscribe(e => a += e.valor);
            EventBus<Ping>.Subscribe(e => b += e.valor);

            EventBus<Ping>.Publish(new Ping { valor = 3 });

            Assert.That(a, Is.EqualTo(3));
            Assert.That(b, Is.EqualTo(3));
        }

        [Test]
        public void Subscribe_ignora_o_mesmo_handler_duas_vezes()
        {
            int chamadas = 0;
            Action<Ping> handler = _ => chamadas++;

            EventBus<Ping>.Subscribe(handler);
            EventBus<Ping>.Subscribe(handler);
            EventBus<Ping>.Publish(new Ping());

            Assert.That(EventBus<Ping>.Assinantes, Is.EqualTo(1));
            Assert.That(chamadas, Is.EqualTo(1));
        }

        [Test]
        public void Unsubscribe_durante_a_entrega_nao_pula_os_demais()
        {
            // Regressão: a entrega iterava a lista viva. Quem cancelasse a própria assinatura dentro do
            // handler deslocava os índices e o assinante seguinte não recebia o evento.
            bool segundoRecebeu = false, terceiroRecebeu = false;
            Action<Ping> primeiro = null;
            primeiro = _ => EventBus<Ping>.Unsubscribe(primeiro);

            EventBus<Ping>.Subscribe(primeiro);
            EventBus<Ping>.Subscribe(_ => segundoRecebeu = true);
            EventBus<Ping>.Subscribe(_ => terceiroRecebeu = true);

            EventBus<Ping>.Publish(new Ping());

            Assert.That(segundoRecebeu, Is.True);
            Assert.That(terceiroRecebeu, Is.True);
            Assert.That(EventBus<Ping>.Assinantes, Is.EqualTo(2));
        }

        [Test]
        public void Subscribe_durante_a_entrega_so_vale_na_proxima_publicacao()
        {
            int novos = 0;
            EventBus<Ping>.Subscribe(_ => EventBus<Ping>.Subscribe(__ => novos++));

            EventBus<Ping>.Publish(new Ping());
            Assert.That(novos, Is.EqualTo(0), "quem assinou no meio da rodada não deve receber o evento em curso");
        }

        [Test]
        public void Publicacao_aninhada_do_mesmo_tipo_e_entregue()
        {
            // Regressão: uma trava de reentrância descartava, em silêncio, o evento publicado de dentro
            // de um handler. Silêncio é o pior modo de falhar num barramento.
            int recebidos = 0;
            EventBus<Ping>.Subscribe(e =>
            {
                recebidos++;
                if (e.valor > 0) EventBus<Ping>.Publish(new Ping { valor = e.valor - 1 });
            });

            EventBus<Ping>.Publish(new Ping { valor = 2 });

            Assert.That(recebidos, Is.EqualTo(3));
        }

        [Test]
        public void Ciclo_infinito_para_no_limite_de_profundidade()
        {
            int recebidos = 0;
            EventBus<Ping>.Subscribe(_ =>
            {
                recebidos++;
                EventBus<Ping>.Publish(new Ping());
            });

            Assert.DoesNotThrow(() => EventBus<Ping>.Publish(new Ping()));
            Assert.That(recebidos, Is.LessThanOrEqualTo(8), "o limite de profundidade precisa cortar o ciclo");
        }

        [Test]
        public void Handler_que_estoura_nao_impede_os_seguintes()
        {
            bool seguinteRecebeu = false;
            EventBus<Ping>.Subscribe(_ => throw new InvalidOperationException("assinante quebrado"));
            EventBus<Ping>.Subscribe(_ => seguinteRecebeu = true);

            Assert.DoesNotThrow(() => EventBus<Ping>.Publish(new Ping()));
            Assert.That(seguinteRecebeu, Is.True);
        }

        [Test]
        public void LimparTudo_zera_todos_os_tipos_de_evento()
        {
            EventBus<Ping>.Subscribe(_ => { });
            EventBus<Pong>.Subscribe(_ => { });

            EventBus.LimparTudo();

            Assert.That(EventBus<Ping>.Assinantes, Is.Zero);
            Assert.That(EventBus<Pong>.Assinantes, Is.Zero);
        }
    }

    public class ServiceLocatorTests
    {
        private sealed class ServicoA { public int Valor; }
        private sealed class ServicoB { }

        [SetUp]
        public void Limpar() => ServiceLocator.Reset();

        [Test]
        public void Get_devolve_o_servico_registrado()
        {
            var a = new ServicoA { Valor = 7 };
            ServiceLocator.Register(a);

            Assert.That(ServiceLocator.Get<ServicoA>(), Is.SameAs(a));
            Assert.That(ServiceLocator.Get<ServicoA>().Valor, Is.EqualTo(7));
        }

        [Test]
        public void Get_de_servico_ausente_falha_rapido()
        {
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<ServicoA>());
        }

        [Test]
        public void TryGet_de_servico_ausente_devolve_false_sem_estourar()
        {
            Assert.That(ServiceLocator.TryGet<ServicoA>(out var servico), Is.False);
            Assert.That(servico, Is.Null);
        }

        [Test]
        public void Register_nulo_e_rejeitado()
        {
            Assert.Throws<ArgumentNullException>(() => ServiceLocator.Register<ServicoA>(null));
        }

        [Test]
        public void Register_sobrescreve_sem_apagar_os_outros()
        {
            ServiceLocator.Register(new ServicoA { Valor = 1 });
            ServiceLocator.Register(new ServicoB());
            ServiceLocator.Register(new ServicoA { Valor = 2 });

            Assert.That(ServiceLocator.Get<ServicoA>().Valor, Is.EqualTo(2));
            Assert.That(ServiceLocator.IsRegistered<ServicoB>(), Is.True);
            Assert.That(ServiceLocator.Registrados, Is.EqualTo(2));
        }

        [Test]
        public void Unregister_tira_apenas_o_servico_pedido()
        {
            ServiceLocator.Register(new ServicoA());
            ServiceLocator.Register(new ServicoB());

            Assert.That(ServiceLocator.Unregister<ServicoA>(), Is.True);
            Assert.That(ServiceLocator.Unregister<ServicoA>(), Is.False);
            Assert.That(ServiceLocator.IsRegistered<ServicoB>(), Is.True);
        }
    }

    public class CaosRuntimeTests
    {
        private struct Ping : IGameEvent { }
        private sealed class Servico { }

        [Test]
        public void Reiniciar_zera_barramento_servicos_e_sessao()
        {
            EventBus<Ping>.Subscribe(_ => { });
            ServiceLocator.Register(new Servico());
            GameSession.Iniciar(slot: 3, novoJogo: true);

            CaosRuntime.Reiniciar();

            Assert.That(EventBus<Ping>.Assinantes, Is.Zero);
            Assert.That(ServiceLocator.Registrados, Is.Zero);
            Assert.That(GameSession.Iniciado, Is.False);
            Assert.That(GameSession.Slot, Is.EqualTo(1));
        }
    }

    public class CaosLogTests
    {
        [TearDown]
        public void Restaurar()
        {
            CaosLog.Destino = null;
            CaosLog.Nivel = NivelDeLog.Info;
        }

        [Test]
        public void Sem_destino_instalado_nada_estoura()
        {
            CaosLog.Destino = null;
            Assert.DoesNotThrow(() => CaosLog.Info("ninguém escutando"));
            Assert.That(CaosLog.Ativo(NivelDeLog.Erro), Is.False);
        }

        [Test]
        public void Nivel_corta_as_mensagens_abaixo_do_piso()
        {
            int recebidas = 0;
            CaosLog.Destino = (_, __) => recebidas++;
            CaosLog.Nivel = NivelDeLog.Aviso;

            CaosLog.Detalhe("x");
            CaosLog.Info("x");
            CaosLog.Aviso("x");
            CaosLog.Erro("x");

            Assert.That(recebidas, Is.EqualTo(2));
            Assert.That(CaosLog.Ativo(NivelDeLog.Info), Is.False, "caminho quente precisa saber que pode pular a interpolação");
            Assert.That(CaosLog.Ativo(NivelDeLog.Erro), Is.True);
        }
    }
}
