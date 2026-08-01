using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Pool genérico para <see cref="Component"/>. Evita Instantiate/Destroy em runtime (GC + spike
    /// de física), pilar de otimização mobile (docs/12 §12.4). Uso típico: tráfego, props, projéteis.
    ///
    /// Cria via factory (não exige prefab) — compatível com o <see cref="WorldBuilder"/> que monta
    /// primitivas em runtime. Prewarm aquece o pool no boot (sem hitch no meio do jogo).
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly Func<T>    _factory;
        private readonly Action<T>  _onGet;
        private readonly Action<T>  _onRelease;
        private readonly Stack<T>   _stack = new Stack<T>();

        public int InPool => _stack.Count;

        public ObjectPool(Func<T> factory, int prewarm = 0, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory   = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet     = onGet;
            _onRelease = onRelease;
            for (int i = 0; i < prewarm; i++)
            {
                var item = _factory();
                item.gameObject.SetActive(false);
                _stack.Push(item);
            }
        }

        public T Get()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : _factory();
            item.gameObject.SetActive(true);
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            _onRelease?.Invoke(item);
            item.gameObject.SetActive(false);
            _stack.Push(item);
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                var item = _stack.Pop();
                if (item != null) UnityEngine.Object.Destroy(item.gameObject);
            }
        }
    }
}
