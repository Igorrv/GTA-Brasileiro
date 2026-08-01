using Caos.Core;
using Caos.World;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Diretor de "procurado" (wanted) em nível de cena. Aciona os sistemas <see cref="WorldStateService.Stars"/>
    /// e <see cref="WorldStateService.Caos"/> que já existem no backend mas estavam inertes. Crimes
    /// (colisões no tráfego/polícia) sobem as estrelas; sem crime por um tempo, as estrelas decaem
    /// (você "despista" a polícia). A <see cref="PoliceSystem"/> reage ao número de estrelas em tempo real.
    /// </summary>
    public class CrimeSystem : MonoBehaviour
    {
        public static CrimeSystem Instance { get; private set; }

        private const float kGraceSeconds = 8f;   // sem crime por 8s → começa a decair
        private const float kDecayStep    = 6f;   // −1 estrela a cada 6s após a graça

        private WorldStateService _world;
        private float _lastCrime;
        private float _decayAccum;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start() => ServiceLocator.TryGet(out _world);

        /// <summary>Registra um crime. <paramref name="severity"/> ~ magnitude do impacto (m/s).</summary>
        public void ReportCrime(int severity)
        {
            if (_world == null) return;
            _lastCrime   = Time.time;
            _decayAccum  = 0f;

            // severidade acumulada → estrelas (escala simples, máx +2 por crime)
            int bump = Mathf.Clamp(Mathf.RoundToInt(severity / 8f), 1, 2);
            _world.SetStars(Mathf.Min(5, _world.Stars + bump));
            _world.ApplyCaos(bump * 8f);

            Debug.Log($"[Crime] Procurado {_world.Stars}/5 (severidade {severity}).");
        }

        private void Update()
        {
            if (_world == null || _world.Stars <= 0) return;
            if (Time.time - _lastCrime < kGraceSeconds) return;   // ainda "quente"

            _decayAccum += Time.deltaTime;
            if (_decayAccum >= kDecayStep)
            {
                _decayAccum = 0f;
                _world.SetStars(_world.Stars - 1);
                if (_world.Stars <= 0) Debug.Log("[Crime] Você despistou a polícia.");
            }
        }
    }
}
