using Caos.Core;
using Caos.Simulation.Audio;
using Caos.World;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Áudio 100% procedural (docs/12 §12.6): nenhum asset importado, tudo gerado em runtime.
    ///
    /// O ponto central continua sendo o <b>motor guiado por RPM</b>, e não por velocidade — é isso que
    /// faz a <b>troca de marcha ser audível</b>: o câmbio sobe, o giro cai, o tom despenca e volta a
    /// subir. A diferença é como isso é produzido. Antes era um laço de meio segundo esticado por
    /// <c>pitch</c> de 0,62× a 2,45×, o que trazia dois defeitos embutidos: o clipe não fechava no
    /// ponto de laço (55 Hz em 0,5 s dá 27,5 ciclos, ou seja, um estalo duas vezes por segundo) e o
    /// <c>pitch</c> arrastava o chiado da admissão junto, virando assobio em alta rotação. Agora o
    /// motor é sintetizado a partir do giro real, no <see cref="MotorSynth"/>.
    ///
    /// Este componente é a <b>ponte</b>: lê o veículo e o mundo na thread principal e alimenta três
    /// fluxos — veículo (motor, pneu, vento), cidade (<see cref="CidadeAmbiente"/>) e os disparos
    /// curtos. Toda mixagem passa pela <see cref="AudioDirector"/>, então buzina e batida abrem espaço
    /// no rádio, a cabine abafa a rua e a pausa silencia tudo com fade em vez de corte seco.
    ///
    /// Vozes:
    ///  • <b>Motor</b> — harmônicas da ordem de ignição, admissão com carga, retorno no freio-motor,
    ///    pipoco no corte de giro;
    ///  • <b>Troca</b> — "chunk" curto a cada mudança de marcha, com solavanco na lataria;
    ///  • <b>Pneu</b> e <b>vento</b> — dentro do mesmo fluxo do motor;
    ///  • <b>Buzina</b> — duas notas, tecla <b>H</b> (é o Brasil, a buzina é infraestrutura);
    ///  • <b>Batida</b> — proporcional ao estrago que a colisão causou;
    ///  • <b>Chime</b> — missão concluída;  • <b>Cidade</b> — cama viva, muda com a hora e com a polícia.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int kTaxa = 22050;

        private VehicleController _vehicle;
        private PlayerVehicleLink _link;
        private VehicleHealth     _health;
        private PoliceSystem      _policia;
        private WorldStateService _mundo;
        private TimeOfDayService  _hora;
        private Transform         _ouvinte;

        private AudioSource _fonteVeiculo, _fonteCidade, _sfx;
        private FluxoDeAudio _fluxoVeiculo, _fluxoCidade;
        private MotorSynth     _motor;
        private CidadeAmbiente _cidade;

        private AudioClip _clipChime, _clipTroca, _clipBuzina, _clipBatida;

        private int   _marchaAnterior = 1;
        private float _hpAnterior = -1f;
        private float _buracoAnterior = -99f;
        private float _proximaBusca;

        public void Init(VehicleController vehicle, PlayerVehicleLink link)
        {
            _vehicle = vehicle;
            _link    = link;
            if (vehicle != null) _health = vehicle.GetComponent<VehicleHealth>();
        }

        private void Awake()
        {
            _clipChime  = ClipeChime();
            _clipTroca  = ClipeTroca();
            _clipBuzina = ClipeBuzina();
            _clipBatida = ClipeBatida();

            _motor  = new MotorSynth(kTaxa);
            _cidade = new CidadeAmbiente(kTaxa);

            _fluxoVeiculo = FluxoDeAudio.Criar("veiculo", _motor, kTaxa);
            _fluxoCidade  = FluxoDeAudio.Criar("cidade",  _cidade, kTaxa);

            if (_fluxoVeiculo != null) { _fonteVeiculo = _fluxoVeiculo.Instalar(gameObject); _fonteVeiculo.Play(); }
            if (_fluxoCidade  != null) { _fonteCidade  = _fluxoCidade.Instalar(gameObject);  _fonteCidade.Play(); }

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.spatialBlend = 0f;
            _sfx.playOnAwake = false;
            _sfx.bypassReverbZones = true;
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _mundo);
            ServiceLocator.TryGet(out _hora);
            var ouvinte = FindObjectOfType<AudioListener>();
            if (ouvinte != null) _ouvinte = ouvinte.transform;
        }

        private void Update()
        {
            bool dirigindo = _link != null && !_link.OnFoot && _vehicle != null;
            AudioDirector.NaCabine = dirigindo;

            AtualizarVeiculo(dirigindo);
            AtualizarCidade(dirigindo);

            if (_fonteVeiculo != null) _fonteVeiculo.volume = AudioDirector.Ganho(Barramento.Motor);
            if (_fonteCidade  != null) _fonteCidade.volume  = AudioDirector.Ganho(Barramento.Ambiente);
        }

        // ------------------------------------------------------------------ veículo
        private void AtualizarVeiculo(bool dirigindo)
        {
            if (_motor == null) return;

            // a buzina é consultada sempre, senão o toque fica pendurado no buffer virtual do touch e
            // dispara sozinho na próxima vez que o jogador entrar no carro
            bool buzinou = GameInput.Horn;

            if (!dirigindo)
            {
                _motor.Ligado = 0f;
                _motor.Carga = 0f;
                _motor.Derrapagem = 0f;
                _motor.Velocidade01 = 0f;
                _motor.NaCabine = 0f;
                AudioDirector.RuidoDeFundo = 0f;
                _hpAnterior = -1f;
                return;
            }

            _motor.Ligado       = 1f;
            _motor.NaCabine     = 1f;
            _motor.Rpm          = _vehicle.Rpm;
            _motor.Carga        = Mathf.Clamp01(Mathf.Abs(GameInput.Move.y));
            _motor.Timbre       = _vehicle.Timbre;
            _motor.Derrapagem   = _vehicle.Derrapando ? 1f : 0f;
            _motor.Velocidade01 = Mathf.Clamp01(_vehicle.SpeedKmh / 130f);

            float giro01 = Mathf.Clamp01(_vehicle.Rpm01);
            AudioDirector.RuidoDeFundo = Mathf.Clamp01(giro01 * 0.55f + _motor.Velocidade01 * 0.65f);

            if (buzinou && _clipBuzina != null)
            {
                Tocar(_clipBuzina, 0.75f);
                AudioDirector.DestacarSfx(0.55f, 0.55f);
            }

            // ---- troca de marcha ----
            if (_vehicle.Marcha != _marchaAnterior)
            {
                _marchaAnterior = _vehicle.Marcha;
                Tocar(_clipTroca, 0.5f);
                _motor.Sacudir(0.28f);
                AudioDirector.DestacarSfx(0.22f, 0.18f);
            }

            // ---- buraco: o mesmo carimbo de tempo que a câmera usa pro solavanco ----
            if (_vehicle.BuracoSentido > _buracoAnterior)
            {
                _buracoAnterior = _vehicle.BuracoSentido;
                _motor.Sacudir(Mathf.Lerp(0.25f, 0.7f, _motor.Velocidade01));
            }

            // ---- batida: a força vem do estrago que a colisão de fato causou ----
            if (_health != null)
            {
                if (_hpAnterior < 0f) _hpAnterior = _health.Hp;
                float dano = _hpAnterior - _health.Hp;
                _hpAnterior = _health.Hp;
                if (dano > 1.5f)
                {
                    float peso = Mathf.Clamp01(dano / 28f);
                    Tocar(_clipBatida, Mathf.Lerp(0.35f, 1f, peso));
                    _motor.Sacudir(Mathf.Lerp(0.4f, 1f, peso));
                    AudioDirector.DestacarSfx(Mathf.Lerp(0.5f, 1f, peso), 0.6f);
                }
            }
        }

        // ------------------------------------------------------------------ cidade
        private void AtualizarCidade(bool dirigindo)
        {
            if (_cidade == null) return;

            _cidade.NaCabine = dirigindo ? 1f : 0f;
            _cidade.Hora     = _hora != null ? _hora.Hour : 12f;

            int estrelas = _mundo != null ? _mundo.Stars : 0;
            _cidade.Urgencia = Mathf.Clamp01(estrelas / 5f);

            // a viatura mais próxima é consultada duas vezes por segundo: buscar todo quadro é caro e
            // uma sirene não muda de distância em 16 ms
            if (Time.unscaledTime >= _proximaBusca)
            {
                _proximaBusca = Time.unscaledTime + 0.5f;
                if (_policia == null) _policia = FindObjectOfType<PoliceSystem>();

                float perto = 999f;
                if (_policia != null && _ouvinte != null) perto = _policia.NearestDistanceTo(_ouvinte);
                _cidade.Sirene = estrelas > 0 ? Mathf.Clamp01(1f - perto / 120f) : 0f;
            }

            // sirene perto rouba a atenção: é a hora em que o jogador precisa ouvir a polícia, não o funk
            if (_cidade.Sirene > 0.25f)
                AudioDirector.Abafar(Barramento.Musica, Mathf.Lerp(1f, 0.55f, _cidade.Sirene), 0.6f);
        }

        /// <summary>Toca o chime de sucesso (chamado por <see cref="MissionTracker"/>).</summary>
        public void Chime()
        {
            Tocar(_clipChime, 0.9f);
            AudioDirector.DestacarSfx(0.75f, 0.9f);
        }

        private void Tocar(AudioClip clip, float volume)
        {
            if (_sfx == null || clip == null) return;
            _sfx.PlayOneShot(clip, volume * AudioDirector.Ganho(Barramento.Sfx));
        }

        // ================================================================== disparos curtos
        // Só o que é transiente continua sendo clipe pronto: são poucos milissegundos cada um, e um
        // one-shot é mais barato do que manter um fluxo aberto para tocar 150 ms de vez em quando.
        // Todos são gerados com envelope que começa e termina em zero — nada de estalo nas pontas.

        /// <summary>Troca de marcha: baque curto e grave com um clique metálico por cima.</summary>
        private static AudioClip ClipeTroca()
        {
            int len = (int)(kTaxa * 0.16f);
            var buf = new float[len];
            var rnd = new CaosDsp.Ruido(4711);
            float fase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kTaxa;
                float env = CaosDsp.Decaimento(t, 28f) * Mathf.Min(1f, t * 900f);
                fase = CaosDsp.Avancar(fase, 90f / kTaxa);
                float baque  = CaosDsp.Seno(fase) * 0.6f;
                float clique = rnd.Proximo() * CaosDsp.Decaimento(t, 70f) * 0.5f;
                buf[i] = (baque + clique) * env;
            }
            return Montar("troca", buf);
        }

        /// <summary>Buzina de carro popular: duas notas juntas e ligeiramente desafinadas.</summary>
        private static AudioClip ClipeBuzina()
        {
            int len = (int)(kTaxa * 0.55f);
            var buf = new float[len];
            float p1 = 0f, p2 = 0f, p3 = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kTaxa;
                float env = Mathf.Clamp01(t * 40f) * Mathf.Clamp01((0.55f - t) * 12f);
                p1 = CaosDsp.Avancar(p1, 420f / kTaxa);
                p2 = CaosDsp.Avancar(p2, 530f / kTaxa);
                p3 = CaosDsp.Avancar(p3, 840f / kTaxa);
                float s = CaosDsp.Seno(p1) * 0.5f + CaosDsp.Seno(p2) * 0.45f + CaosDsp.Seno(p3) * 0.15f;
                buf[i] = CaosDsp.Saturar(s * 1.4f) * env * 0.55f;
            }
            return Montar("buzina", buf);
        }

        /// <summary>
        /// Batida: chapa amassando (ruído grave em queda), estilhaço de vidro/plástico por cima e um
        /// estouro seco na frente. É curto de propósito — impacto longo vira desenho animado.
        /// </summary>
        private static AudioClip ClipeBatida()
        {
            int len = (int)(kTaxa * 0.7f);
            var buf = new float[len];
            var rnd = new CaosDsp.Ruido(1312);
            CaosDsp.Biquad chapa = default, vidro = default;
            chapa.PassaBanda(220f, 1.1f, kTaxa);
            vidro.PassaBanda(4200f, 2.5f, kTaxa);
            float fase = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kTaxa;
                float x = rnd.Proximo();
                fase = CaosDsp.Avancar(fase, Mathf.Lerp(110f, 45f, Mathf.Min(1f, t * 6f)) / kTaxa);

                float impacto = chapa.Filtrar(x) * CaosDsp.Decaimento(t, 9f) * 2.4f
                              + CaosDsp.Seno(fase) * CaosDsp.Decaimento(t, 14f) * 0.7f;
                float caco = vidro.Filtrar(x) * CaosDsp.Decaimento(t, 5.5f) * (rnd.Sorte() < 0.06f ? 2.2f : 0.35f);
                buf[i] = CaosDsp.Saturar((impacto + caco) * 1.2f) * Mathf.Min(1f, t * 1200f);
            }
            return Montar("batida", buf);
        }

        /// <summary>Chime de missão: duas notas ascendentes com cauda — a recompensa precisa soar limpa.</summary>
        private static AudioClip ClipeChime()
        {
            int len = (int)(kTaxa * 0.85f);
            var buf = new float[len];
            float p1 = 0f, p2 = 0f, p3 = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / kTaxa;
                p1 = CaosDsp.Avancar(p1, 660f / kTaxa);
                p2 = CaosDsp.Avancar(p2, 990f / kTaxa);
                p3 = CaosDsp.Avancar(p3, 1320f / kTaxa);

                float a = CaosDsp.Decaimento(t, 3.2f) * Mathf.Min(1f, t * 500f);
                float b = t > 0.16f ? CaosDsp.Decaimento(t - 0.16f, 2.6f) : 0f;
                buf[i] = (CaosDsp.Seno(p1) * a * 0.55f
                        + CaosDsp.Seno(p2) * b * 0.50f
                        + CaosDsp.Seno(p3) * b * 0.18f) * 0.7f;
            }
            return Montar("chime", buf);
        }

        private static AudioClip Montar(string nome, float[] buf)
        {
            var clip = AudioClip.Create(nome, buf.Length, 1, kTaxa, stream: false);
            clip.SetData(buf, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (_fonteVeiculo != null) _fonteVeiculo.Stop();
            if (_fonteCidade  != null) _fonteCidade.Stop();
            _fluxoVeiculo?.Destruir();
            _fluxoCidade?.Destruir();

            if (_clipChime  != null) Destroy(_clipChime);
            if (_clipTroca  != null) Destroy(_clipTroca);
            if (_clipBuzina != null) Destroy(_clipBuzina);
            if (_clipBatida != null) Destroy(_clipBatida);
        }
    }
}
