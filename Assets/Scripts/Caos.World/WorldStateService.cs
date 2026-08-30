using Caos.Core;
using Caos.Data;

namespace Caos.World
{
    /// <summary>
    /// Estado global do mundo: Nível de Caos (0–100), estrelas de procurado (0–5),
    /// bairro atual e clima. Decai o Caos lentamente quando não há incidentes (docs/10.6).
    /// </summary>
    public sealed class WorldStateService : ITickable
    {
        public float Caos { get; private set; } = 20f;          // padrão inicial tranquilo
        public int   Stars { get; private set; } = 0;
        public DistrictId  CurrentDistrict { get; set; } = DistrictId.Centro;
        public WeatherState Weather { get; set; } = WeatherState.SolLeve;

        public int Order => 10;

        public void Tick(float dt)
        {
            // decaimento do Caos: −1/min = −0,0167/s (apenas se acima do piso)
            if (Caos > 20f) Caos = System.Math.Max(20f, Caos - 0.01667f * dt);
        }

        public void ApplyCaos(float delta)
        {
            var prev = Caos;
            Caos = CaosMath.Limitar(Caos + delta, 0f, 100f);
            if (!CaosMath.Aproximadamente(prev, Caos))
                EventBus<CaosMudou>.Publish(new CaosMudou { valor = Caos });
        }

        public void SetStars(int value)
        {
            value = CaosMath.Limitar(value, 0, 5);
            if (Stars == value) return;
            Stars = value;
            EventBus<EstrelasMudou>.Publish(new EstrelasMudou { valor = Stars });
        }

        public float ChaosFactor => 0.5f + (Caos / 100f); // f_Caos do docs/06 §H

        /// <summary>Restaura estado a partir do save.</summary>
        public void Hydrate(float caos, int stars, DistrictId district, WeatherState weather)
        {
            Caos = CaosMath.Limitar(caos, 0f, 100f);
            Stars = CaosMath.Limitar(stars, 0, 5);
            CurrentDistrict = district;
            Weather = weather;
        }
    }

    // ---- Eventos de mundo (structs) ----
    public struct CaosMudou : IGameEvent { public float valor; }
    public struct EstrelasMudou : IGameEvent { public int valor; }
}
