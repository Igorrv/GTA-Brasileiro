using System;
using System.Collections.Generic;

namespace Caos.Core
{
    /// <summary>
    /// Marca eventos trocados pelo sistema. Eventos são structs (sem alocação de GC no publish).
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// Barramento de eventos tipado e estático. Desacopla sistemas (ex.: GameOverEconômico, MissãoConcluída).
    /// Uso:
    ///   EventBus&lt;DinheiroMudouEvt&gt;.Subscribe(e =&gt; ...);
    ///   EventBus&lt;DinheiroMudouEvt&gt;.Publish(new DinheiroMudouEvt { Valor = 100 });
    /// </summary>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        private static readonly List<Action<T>> _handlers = new List<Action<T>>();
        private static bool _isPublishing;

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || _handlers.Contains(handler)) return;
            _handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler) => _handlers.Remove(handler);

        public static void Publish(T evt)
        {
            if (_isPublishing) return; // evita reentrância simples
            _isPublishing = true;
            try
            {
                // itera sobre cópia para permitir unsubscribe durante o publish
                for (int i = 0; i < _handlers.Count; i++)
                    _handlers[i]?.Invoke(evt);
            }
            finally { _isPublishing = false; }
        }

        public static void Clear() => _handlers.Clear();
    }
}
