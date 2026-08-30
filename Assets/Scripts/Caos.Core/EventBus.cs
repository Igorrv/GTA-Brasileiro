using System;
using System.Collections.Generic;

namespace Caos.Core
{
    /// <summary>
    /// Marca eventos trocados pelo sistema. Eventos são structs (sem alocação de GC no publish).
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// Lado não genérico do barramento. Serve para uma coisa só: <see cref="LimparTudo"/>.
    ///
    /// <c>EventBus&lt;T&gt;</c> é uma classe estática genérica — cada <c>T</c> tem a sua própria lista de
    /// assinantes e não existe como enumerar essas listas de fora. Por isso cada <c>T</c> se cadastra
    /// aqui no primeiro uso, e o boot consegue zerar o barramento inteiro de uma vez.
    ///
    /// Isso importa quando o "Enter Play Mode" do Editor está sem recarga de domínio (o modo rápido,
    /// que é o que se quer usar iterando em celular): sem esta limpeza os assinantes da sessão anterior
    /// continuam na lista, apontando para objetos já destruídos.
    /// </summary>
    public static class EventBus
    {
        private static readonly List<Action> _limpadores = new List<Action>();

        internal static void Cadastrar(Action limpar)
        {
            if (limpar == null) return;
            _limpadores.Add(limpar);
        }

        /// <summary>Remove todos os assinantes de todos os tipos de evento.</summary>
        public static void LimparTudo()
        {
            for (int i = 0; i < _limpadores.Count; i++) _limpadores[i]();
        }
    }

    /// <summary>
    /// Barramento de eventos tipado e estático. Desacopla sistemas (ex.: GameOverEconômico, MissãoConcluída).
    /// Uso:
    ///   EventBus&lt;DinheiroMudouEvt&gt;.Subscribe(e =&gt; ...);
    ///   EventBus&lt;DinheiroMudouEvt&gt;.Publish(new DinheiroMudouEvt { Valor = 100 });
    ///
    /// Garantias do <see cref="Publish"/>:
    ///  • quem assina ou cancela <b>durante</b> a entrega não altera a rodada em curso (a lista é
    ///    copiada antes de chamar o primeiro handler);
    ///  • um handler que estoura exceção não impede os seguintes de receber o evento;
    ///  • publicar o mesmo tipo de dentro de um handler funciona, até
    ///    <see cref="kProfundidadeMaxima"/> níveis — o limite existe para que um ciclo A→A não
    ///    trave o jogo em recursão infinita.
    /// </summary>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        private const int kProfundidadeMaxima = 8;

        private static readonly List<Action<T>> _handlers = new List<Action<T>>();

        /// <summary>Cópia reaproveitada da lista de handlers: entrega sem alocar no caso comum.</summary>
        private static Action<T>[] _entrega = new Action<T>[8];
        private static int _profundidade;

        static EventBus() => Caos.Core.EventBus.Cadastrar(Clear);

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || _handlers.Contains(handler)) return;
            _handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler) => _handlers.Remove(handler);

        public static void Publish(T evt)
        {
            int total = _handlers.Count;
            if (total == 0) return;

            if (_profundidade >= kProfundidadeMaxima)
            {
                CaosLog.Erro($"[EventBus] Ciclo de publicação em {typeof(T).Name}: parou em {kProfundidadeMaxima} níveis.");
                return;
            }

            // No nível de fora reaproveita o buffer; só a publicação aninhada (rara) aloca o seu.
            Action<T>[] entrega;
            if (_profundidade == 0)
            {
                if (_entrega.Length < total) _entrega = new Action<T>[Math.Max(total, _entrega.Length * 2)];
                entrega = _entrega;
            }
            else
            {
                entrega = new Action<T>[total];
            }
            _handlers.CopyTo(0, entrega, 0, total);

            _profundidade++;
            try
            {
                for (int i = 0; i < total; i++)
                {
                    var handler = entrega[i];
                    if (handler == null) continue;
                    try
                    {
                        handler(evt);
                    }
                    catch (Exception e)
                    {
                        // Um assinante quebrado (tipicamente UI) não pode derrubar a regra de jogo.
                        CaosLog.Erro($"[EventBus] Handler de {typeof(T).Name} falhou: {e}");
                    }
                }
            }
            finally
            {
                _profundidade--;
                Array.Clear(entrega, 0, total);   // não segura referência a objeto destruído
            }
        }

        /// <summary>Número de assinantes — usado por testes e por diagnóstico de vazamento.</summary>
        public static int Assinantes => _handlers.Count;

        public static void Clear() => _handlers.Clear();
    }
}
