using Caos.Core;
using Caos.Data;

namespace Caos.World
{
    /// <summary>
    /// Ciclo dia/noite. 1 dia de jogo = 48 min reais (docs/00, docs/10.4).
    /// Expõe hora (0–24), dia atual e fase (manhã/dia/noite/madrugada) usada pelo spawn de eventos.
    /// </summary>
    public sealed class TimeOfDayService : ITickable
    {
        public float Hour { get; private set; } = 8f;   // começa de manhã
        public int   Day  { get; private set; } = 1;

        // 1 real-sec = 24h / (48min*60s) = 0,008333 game-hours
        private const float kGameHoursPerRealSec = 24f / (48f * 60f);

        public int Order => 5;

        public void Tick(float dt)
        {
            Hour += kGameHoursPerRealSec * dt;
            if (Hour >= 24f) { Hour -= 24f; Day++; }
        }

        /// <summary>Fase do dia para filtros de eventos (docs/06).</summary>
        public string Fase
        {
            get
            {
                if (Hour >= 6f && Hour < 10f)  return "manha";
                if (Hour >= 10f && Hour < 16f) return "dia";
                if (Hour >= 16f && Hour < 19f) return "tarde";
                if (Hour >= 19f && Hour < 22f) return "noite";
                return "madrugada";
            }
        }

        /// <summary>Densidade de tráfego sugerida (0..1) pela hora.</summary>
        public float Trafego =>
            (Fase == "manha" || Fase == "tarde") ? 1.0f :
            (Fase == "noite") ? 0.6f :
            (Fase == "madrugada") ? 0.2f : 0.8f;

        /// <summary>Restaura estado a partir do save.</summary>
        public void Hydrate(float hour, int day) { Hour = hour; Day = day; }

        /// <summary>Avança o relógio (ex.: turno de trabalho VaiJá). 1 dia de jogo = 48 min reais.</summary>
        public void AdvanceHours(float hours)
        {
            Hour += hours;
            while (Hour >= 24f) { Hour -= 24f; Day++; }
            while (Hour < 0f)   { Hour += 24f; }
        }
    }
}
