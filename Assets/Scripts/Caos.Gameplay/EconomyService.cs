using Caos.Core;
using Caos.Data;
using Caos.World;

namespace Caos.Gameplay
{
    /// <summary>
    /// Economia: R$ (soft) + CaosCash (premium) + IPC-Caos (inflação semanal, docs/05 §B).
    /// Preços de itens/veículos são corrigidos pelo IPC-Caos.
    /// </summary>
    public sealed class EconomyService : ITickable
    {
        public float Rs       { get; private set; } = 150f;   // salário inicial (docs/00)
        public float CaosCash { get; private set; } = 0f;
        public float IpcCaos  { get; private set; } = 0.01f;  // 1% inicial

        private readonly TimeOfDayService _time;
        private int _lastDay;

        public EconomyService(TimeOfDayService time) { _time = time; _lastDay = time.Day; }

        public int Order => 15;

        public void Tick(float dt)
        {
            // Detecta virada de dia para aplicar inflação semanal
            if (_time.Day != _lastDay)
            {
                _lastDay = _time.Day;
                if (_time.Day % 7 == 0) AddWeeklyInflation();
            }
        }

        /// <summary>IPC-Caos: base semanal + fator de caos (docs/05 §B, docs/06 §H).</summary>
        public void AddWeeklyInflation(float caos = 40f)
        {
            float bump = 0.01f + (caos / 100f) * 0.02f; // 1%..3% por semana conforme caos
            IpcCaos += bump;
            EventBus<DinheiroMudou>.Publish(new DinheiroMudou { rs = Rs, caosCash = CaosCash });
            CaosLog.Info($"[Economia] IPC-Caos subiu para {IpcCaos:P1} (+{bump:P1}).");
        }

        /// <summary>Preço corrigido pela inflação: preço_base × (1 + IPC-Caos).</summary>
        public float PriceFor(float basePrice) => basePrice * (1f + IpcCaos);

        public void Add(float rs, float caosCash = 0f)
        {
            Rs += rs;
            CaosCash += caosCash;
            EventBus<DinheiroMudou>.Publish(new DinheiroMudou { rs = Rs, caosCash = CaosCash });
        }

        /// <summary>Tenta debitar R$. Retorna false se não houver saldo (sem dívida).</summary>
        public bool TrySpend(float rs)
        {
            if (Rs + 0.001f < rs) return false;
            Rs -= rs;
            EventBus<DinheiroMudou>.Publish(new DinheiroMudou { rs = Rs, caosCash = CaosCash });
            return true;
        }

        /// <summary>Restaura estado a partir do save.</summary>
        public void Hydrate(float rs, float caosCash, float ipcCaos)
        {
            Rs = rs; CaosCash = caosCash; IpcCaos = ipcCaos; _lastDay = _time.Day;
            EventBus<DinheiroMudou>.Publish(new DinheiroMudou { rs = Rs, caosCash = CaosCash });
        }
    }
}
