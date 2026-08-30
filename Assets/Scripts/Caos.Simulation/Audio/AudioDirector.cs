using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>Barramentos da mesa. Cada um é uma família de sons que sobe e desce junto.</summary>
    public enum Barramento
    {
        /// <summary>Rádio do carro (o que o jogador chama de "música").</summary>
        Musica = 0,
        /// <summary>Motor, pneu e vento — o veículo.</summary>
        Motor = 1,
        /// <summary>Buzina, batida, troca de marcha, chime de missão. Nunca é abafado.</summary>
        Sfx = 2,
        /// <summary>Cama da cidade: tráfego distante, bichos, sirene longe.</summary>
        Ambiente = 3,
    }

    /// <summary>
    /// A mesa de som do jogo: um lugar só onde volume, <b>ducking</b> e mute de pausa acontecem.
    ///
    /// <b>O problema que ela resolve.</b> Antes cada sistema escrevia direto no <c>volume</c> da sua
    /// <c>AudioSource</c> com um número mágico (0,22 no rádio; 0,14 no ambiente; 0,42 no motor). Como
    /// ninguém conhecia ninguém, três coisas quebravam: o slider "Volume da música" dos Ajustes não
    /// mexia no rádio, a locução do DJ competia com o motor a plena carga, e o menu de pausa congelava
    /// o jogo com o motor roncando — porque <c>Time.deltaTime</c> vira zero e as interpolações param
    /// no lugar em que estavam.
    ///
    /// <b>Como funciona.</b> Cada barramento tem um ganho base (mixagem + preferências do jogador) e um
    /// <i>ducker</i>: qualquer sistema pede <see cref="Abafar"/> para derrubar um barramento por alguns
    /// instantes, com ataque rápido e alívio lento — é a mesma sidechain de estúdio, feita à mão. Quem
    /// toca só multiplica o próprio volume por <see cref="Ganho"/>.
    ///
    /// A pausa é tratada aqui porque é o único ponto que enxerga tudo: o mestre desce em ~90 ms (sem o
    /// estalo de um corte seco) e só então <c>AudioListener.pause</c> entra, o que também para os
    /// callbacks de síntese e poupa bateria. O mesmo vale para o app ir pro segundo plano no celular —
    /// nada de rádio tocando na gaveta de notificações.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AudioDirector : MonoBehaviour
    {
        /// <summary>Mixagem base de cada barramento (antes das preferências e do ducking).</summary>
        private static readonly float[] kMixBase = { 0.85f, 0.90f, 1.00f, 0.70f };

        private const float kAtaque = 14f;    // ganho/s ao abafar  — precisa ser rápido pra abrir espaço
        private const float kAlivio =  1.6f;  // ganho/s ao soltar  — lento, senão "bombeia"
        private const float kFadePausa = 11f; // ganho/s do mestre ao pausar/despausar

        private sealed class Canal
        {
            public float Pedido = 1f;   // menor ganho pedido na janela atual
            public float Ate;           // instante (unscaled) em que o pedido expira
            public float Atual = 1f;    // ganho suavizado que os sistemas leem
        }

        private static readonly Canal[] _canais = { new Canal(), new Canal(), new Canal(), new Canal() };

        private static AudioDirector _instancia;
        private static float _mestre = 1f;
        private static bool  _appEmSegundoPlano;
        private static bool  _semFoco;

        /// <summary>O jogador está dentro do veículo — a cabine abafa a cidade e libera o rádio.</summary>
        public static bool NaCabine { get; set; }

        /// <summary>
        /// Quanto barulho de fundo (motor + vento) está mascarando o resto, de 0 a 1. O rádio usa para
        /// subir um pouco em alta velocidade, que é o que qualquer aparelho de carro decente faz.
        /// </summary>
        public static float RuidoDeFundo { get; set; }

        /// <summary>Áudio efetivamente silenciado (pausa, segundo plano ou fade em curso).</summary>
        public static bool Silenciado => _mestre < 0.001f;

        // ------------------------------------------------------------------ instalação
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configurar()
        {
            // Buffer de DSP maior = menos acordadas da thread de áudio = menos bateria e menos risco de
            // estouro no celular. 1024 quadros ≈ 21 ms de latência, imperceptível para som ambiente e
            // rádio. Feito antes de qualquer clipe existir, porque um Reset depois disso os invalidaria.
            try
            {
                var cfg = AudioSettings.GetConfiguration();
                cfg.dspBufferSize   = Application.isMobilePlatform ? 1024 : 512;
                cfg.numRealVoices   = Application.isMobilePlatform ? 16 : 32;
                cfg.numVirtualVoices = 64;
                AudioSettings.Reset(cfg);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Áudio] Não foi possível ajustar o buffer de DSP: " + e.Message);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Instalar()
        {
            if (_instancia != null) return;
            var go = new GameObject("[MesaDeSom]");
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<AudioDirector>();
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }
            _instancia = this;
            for (int i = 0; i < _canais.Length; i++) { _canais[i].Pedido = 1f; _canais[i].Atual = 1f; _canais[i].Ate = 0f; }
        }

        // ------------------------------------------------------------------ API
        /// <summary>
        /// Ganho final do barramento: mixagem × preferência do jogador × ducking × fade de pausa.
        /// É o único número que os sistemas de áudio precisam multiplicar.
        /// </summary>
        public static float Ganho(Barramento b)
        {
            int i = (int)b;
            float g = kMixBase[i] * _canais[i].Atual * _mestre;
            if (b == Barramento.Musica) g *= SettingsMenu.VolumeMusica;
            return g;
        }

        /// <summary>Ducking puro do barramento (1 = livre), para quem precisa do fator sem a mixagem.</summary>
        public static float Ducking(Barramento b) => _canais[(int)b].Atual;

        /// <summary>
        /// Pede espaço: derruba <paramref name="alvo"/> para <paramref name="ganho"/> e segura por
        /// <paramref name="segurar"/> segundos. Chamadas repetidas no mesmo instante ficam com a mais
        /// severa — quem grita mais alto manda, como numa sidechain de verdade.
        /// </summary>
        public static void Abafar(Barramento alvo, float ganho, float segurar = 0.12f)
        {
            var c = _canais[(int)alvo];
            float agora = Time.unscaledTime;
            if (agora > c.Ate) c.Pedido = 1f;             // a janela anterior já passou
            c.Pedido = Mathf.Min(c.Pedido, Mathf.Clamp01(ganho));
            c.Ate    = Mathf.Max(c.Ate, agora + segurar);
        }

        /// <summary>
        /// Um SFX importante acabou de tocar: abre espaço para ele no rádio, no motor e na cidade.
        /// <paramref name="peso"/> 0..1 escala o quanto (buzina leve × batida forte).
        /// </summary>
        public static void DestacarSfx(float peso, float segurar = 0.30f)
        {
            peso = Mathf.Clamp01(peso);
            Abafar(Barramento.Musica,   Mathf.Lerp(1f, 0.42f, peso), segurar);
            Abafar(Barramento.Motor,    Mathf.Lerp(1f, 0.70f, peso), segurar);
            Abafar(Barramento.Ambiente, Mathf.Lerp(1f, 0.55f, peso), segurar);
        }

        /// <summary>
        /// O locutor está falando. A voz sai dentro do próprio fluxo do rádio (a cama da música abaixa
        /// lá dentro), mas o motor e a cidade também precisam sair da frente, senão a fala some no
        /// ronco a 4.000 giros.
        /// </summary>
        public static void LocutorNoAr(float intensidade)
        {
            intensidade = Mathf.Clamp01(intensidade);
            Abafar(Barramento.Motor,    Mathf.Lerp(1f, 0.62f, intensidade), 0.18f);
            Abafar(Barramento.Ambiente, Mathf.Lerp(1f, 0.45f, intensidade), 0.18f);
        }

        // ------------------------------------------------------------------ tick
        private void Update()
        {
            float dt    = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float agora = Time.unscaledTime;

            bool congelado = Time.timeScale <= 0.0001f;
            bool foraDoAr  = _appEmSegundoPlano || (_semFoco && Application.isMobilePlatform);
            bool mudo      = congelado || foraDoAr;

            _mestre = Mathf.MoveTowards(_mestre, mudo ? 0f : 1f, kFadePausa * dt);

            // só corta o listener depois do fade — cortar no primeiro quadro estala
            bool pausar = mudo && _mestre <= 0.001f;
            if (AudioListener.pause != pausar) AudioListener.pause = pausar;

            for (int i = 0; i < _canais.Length; i++)
            {
                var c = _canais[i];
                float alvo = agora > c.Ate ? 1f : c.Pedido;
                if (agora > c.Ate) c.Pedido = 1f;
                c.Atual = Mathf.MoveTowards(c.Atual, alvo, (alvo < c.Atual ? kAtaque : kAlivio) * dt);
            }
        }

        private void OnApplicationPause(bool pausado)  { _appEmSegundoPlano = pausado; }
        private void OnApplicationFocus(bool comFoco)  { _semFoco = !comFoco; }

        private void OnDestroy()
        {
            if (_instancia != this) return;
            _instancia = null;
            AudioListener.pause = false;
        }
    }
}
