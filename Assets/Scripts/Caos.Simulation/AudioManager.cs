using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Áudio 100% procedural (docs/12 §12.6): todos os clipes são gerados em runtime via
    /// <see cref="AudioClip.Create"/> (PCM), sem importar nenhum asset.
    ///
    /// O ponto central é o <b>motor guiado por RPM</b>, e não por velocidade. É essa diferença que faz
    /// a <b>troca de marcha ser audível</b>: o câmbio sobe a marcha, o giro cai, o tom despenca e volta
    /// a subir — exatamente como num carro. Velocidade constante com marcha trocando não muda nada se
    /// o som seguir a velocidade; seguindo o giro, muda tudo.
    ///
    /// Vozes:
    ///  • <b>Motor</b> — drone harmônico em loop, pitch pelo giro, volume pelo acelerador;
    ///  • <b>Troca</b> — "chunk" curto a cada mudança de marcha;
    ///  • <b>Pneu</b> — chiado enquanto derrapa (freio de mão / curva no limite);
    ///  • <b>Buzina</b> — duas notas, tecla <b>H</b> (é o Brasil, a buzina é infraestrutura);
    ///  • <b>Chime</b> — missão concluída;  • <b>Ambiente</b> — cama sutil sempre ligada.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int kSampleRate = 22050;

        private VehicleController _vehicle;
        private PlayerVehicleLink _link;

        private AudioSource _motor, _sfx, _pneu, _ambiente;
        private AudioClip _clipMotor, _clipChime, _clipAmbiente, _clipTroca, _clipPneu, _clipBuzina;

        private int   _marchaAnterior = 1;
        private float _giroSuave;

        public void Init(VehicleController vehicle, PlayerVehicleLink link)
        {
            _vehicle = vehicle;
            _link    = link;
        }

        private void Awake()
        {
            _clipMotor    = ClipeMotor();
            _clipChime    = ClipeChime();
            _clipAmbiente = ClipePad();
            _clipTroca    = ClipeTroca();
            _clipPneu     = ClipePneu();
            _clipBuzina   = ClipeBuzina();

            _motor = gameObject.AddComponent<AudioSource>();
            _motor.clip = _clipMotor; _motor.loop = true; _motor.volume = 0f;
            _motor.spatialBlend = 0f; _motor.playOnAwake = false;
            _motor.Play();

            _pneu = gameObject.AddComponent<AudioSource>();
            _pneu.clip = _clipPneu; _pneu.loop = true; _pneu.volume = 0f;
            _pneu.spatialBlend = 0f; _pneu.playOnAwake = false;
            _pneu.Play();

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.spatialBlend = 0f; _sfx.playOnAwake = false;

            _ambiente = gameObject.AddComponent<AudioSource>();
            _ambiente.clip = _clipAmbiente; _ambiente.loop = true; _ambiente.volume = 0.14f;
            _ambiente.spatialBlend = 0f; _ambiente.playOnAwake = true;
        }

        private void Update()
        {
            if (_motor == null) return;
            float dt = Time.deltaTime;

            bool dirigindo = _link != null && !_link.OnFoot && _vehicle != null;

            if (GameInput.Horn && dirigindo && _sfx != null && _clipBuzina != null)
                _sfx.PlayOneShot(_clipBuzina, 0.7f);

            if (!dirigindo)
            {
                _motor.volume = Mathf.Lerp(_motor.volume, 0f, dt * 4f);
                _pneu.volume  = Mathf.Lerp(_pneu.volume,  0f, dt * 6f);
                _giroSuave = 0f;
                return;
            }

            // ---- motor: pitch pelo GIRO (é isso que faz a marcha soar) ----
            float giro = _vehicle.Rpm01;
            _giroSuave = Mathf.Lerp(_giroSuave, giro, dt * 12f);

            // 0,62 na marcha lenta até 2,45 no corte: quase 2 oitavas de excursão
            // o timbre do modelo desloca a faixa inteira: moto fica aguda, caminhão fica grave
            _motor.pitch  = Mathf.Lerp(0.62f, 2.45f, _giroSuave) * _vehicle.Timbre;
            float carga   = Mathf.Clamp01(Mathf.Abs(GameInput.Move.y));
            _motor.volume = Mathf.Lerp(_motor.volume, Mathf.Lerp(0.16f, 0.42f, carga * 0.6f + _giroSuave * 0.4f), dt * 6f);

            // ---- troca de marcha: corta o som um instante e dá o "chunk" ----
            if (_vehicle.Marcha != _marchaAnterior)
            {
                _marchaAnterior = _vehicle.Marcha;
                if (_clipTroca != null) _sfx.PlayOneShot(_clipTroca, 0.55f);
                _motor.volume *= 0.45f;         // alívio do acelerador na troca
                _giroSuave    *= 0.72f;         // e o giro cai junto
            }

            // no corte de giro o motor engasga em vez de subir sem parar
            if (_vehicle.NoCorte) _motor.pitch *= 0.94f + Mathf.Sin(Time.time * 60f) * 0.045f;

            // ---- pneu cantando ----
            bool cantando = _vehicle.Derrapando;
            _pneu.volume = Mathf.Lerp(_pneu.volume, cantando ? 0.30f : 0f, dt * 8f);
            _pneu.pitch  = Mathf.Lerp(0.85f, 1.35f, Mathf.Clamp01(_vehicle.SpeedKmh / 90f));
        }

        /// <summary>Toca o chime de sucesso (chamado por <see cref="MissionTracker"/>).</summary>
        public void Chime()
        {
            if (_sfx != null && _clipChime != null) _sfx.PlayOneShot(_clipChime, 0.9f);
        }

        // ================================================================== síntese PCM
        /// <summary>
        /// Motor: fundamental + harmônicas ímpares (dá o ronco áspero de motor a combustão) + sopro.
        /// O clipe é curto e o pitch faz o resto — sintetizar por giro em tempo real custaria caro.
        /// </summary>
        private AudioClip ClipeMotor()
        {
            int len = kSampleRate / 2;
            var buf = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kSampleRate;
                float s = Mathf.Sin(2f * Mathf.PI * 55f  * t) * 0.50f
                        + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.26f
                        + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.18f   // 3ª harmônica: aspereza
                        + Mathf.Sin(2f * Mathf.PI * 275f * t) * 0.08f   // 5ª
                        + Ruido(t) * 0.07f;                             // admissão
                buf[i] = s * 0.55f;
            }
            return Montar("motor", buf);
        }

        /// <summary>Troca de marcha: baque curto e grave, com um clique metálico por cima.</summary>
        private AudioClip ClipeTroca()
        {
            int len = (int)(kSampleRate * 0.16f);
            var buf = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kSampleRate;
                float env = Mathf.Exp(-t * 28f);
                float baque  = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.6f;
                float clique = Ruido(t * 3f) * Mathf.Exp(-t * 70f) * 0.5f;
                buf[i] = (baque + clique) * env;
            }
            return Montar("troca", buf);
        }

        /// <summary>Pneu cantando: ruído filtrado com formante agudo.</summary>
        private AudioClip ClipePneu()
        {
            int len = kSampleRate;
            var buf = new float[len];
            float anterior = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kSampleRate;
                float bruto = Ruido(t * 7f);
                anterior = Mathf.Lerp(anterior, bruto, 0.35f);            // passa-baixa simples
                float formante = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.25f;
                buf[i] = (anterior * 0.8f + anterior * formante) * 0.5f;
            }
            return Montar("pneu", buf);
        }

        /// <summary>Buzina: duas notas juntas, meio desafinadas — buzina de carro popular.</summary>
        private AudioClip ClipeBuzina()
        {
            int len = (int)(kSampleRate * 0.55f);
            var buf = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kSampleRate;
                float env = Mathf.Clamp01(t * 40f) * Mathf.Clamp01((0.55f - t) * 12f);
                float s = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * 530f * t) * 0.45f
                        + Mathf.Sin(2f * Mathf.PI * 840f * t) * 0.15f;
                buf[i] = s * env * 0.55f;
            }
            return Montar("buzina", buf);
        }

        private AudioClip ClipeChime()
        {
            int len = (int)(kSampleRate * 0.5f);
            var buf = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t   = (float)i / kSampleRate;
                float env = Mathf.Exp(-t * 4f);
                float f   = (t < 0.22f) ? 660f : 990f;
                buf[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.6f;
            }
            return Montar("chime", buf);
        }

        private AudioClip ClipePad()
        {
            int len = (int)(kSampleRate * 2f);
            var buf = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t   = (float)i / kSampleRate;
                float s   = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.5f
                          + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.35f;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.2f * t);
                buf[i] = s * lfo * 0.18f;
            }
            return Montar("ambiente", buf);
        }

        /// <summary>Ruído determinístico — o clipe soa igual em toda máquina.</summary>
        private static float Ruido(float t) => (Mathf.Abs(Mathf.Sin(t * 99991f) * 43758.5453f) % 1f) * 2f - 1f;

        private static AudioClip Montar(string nome, float[] buf)
        {
            var clip = AudioClip.Create(nome, buf.Length, 1, kSampleRate, stream: false);
            clip.SetData(buf, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (_clipMotor    != null) Destroy(_clipMotor);
            if (_clipChime    != null) Destroy(_clipChime);
            if (_clipAmbiente != null) Destroy(_clipAmbiente);
            if (_clipTroca    != null) Destroy(_clipTroca);
            if (_clipPneu     != null) Destroy(_clipPneu);
            if (_clipBuzina   != null) Destroy(_clipBuzina);
        }
    }
}
