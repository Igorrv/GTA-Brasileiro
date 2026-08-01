using System;
using System.Collections.Generic;

namespace Caos.Core
{
    /// <summary>
    /// Registro global de serviços (EconomyService, ReputationService, etc.).
    /// Falha rápido (exception) se um serviço esperado não estiver registrado — ajuda a pegar bugs de wiring no boot.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
            throw new InvalidOperationException($"[ServiceLocator] Serviço não registrado: {typeof(T).Name}");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s)) { service = (T)s; return true; }
            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class => _services.ContainsKey(typeof(T));

        public static void Reset() => _services.Clear();
    }
}
